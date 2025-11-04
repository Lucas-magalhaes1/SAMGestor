using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using SAMGestor.Application.Features.Families.Unassigned;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;
using Xunit;

namespace SAMGestor.UnitTests.Application.Features.Families.Unassigned;

public sealed class GetUnassignedHandlerTests
{
    private static Registration Reg(
        Guid retreatId,
        string name,
        Gender g,
        string city = "SP",
        string? email = null,
        string cpf = "52998224725",
        RegistrationStatus status = RegistrationStatus.Confirmed
    )
        => new Registration(
            new FullName(name),
            new CPF(cpf),
            new EmailAddress(email ?? $"{Guid.NewGuid():N}@mail.com"),
            "11999999999",
            new DateOnly(1990, 1, 1),
            g,
            city,
            status,
            retreatId
        );

    private static FamilyMember Link(Guid retreatId, Guid familyId, Guid regId, int pos)
        => new FamilyMember(retreatId, familyId, regId, pos);

    private static (Mock<IRegistrationRepository> regRepo, Mock<IFamilyMemberRepository> linkRepo) Mocks()
        => (new Mock<IRegistrationRepository>(), new Mock<IFamilyMemberRepository>());

    private static void SetupLists(
        Mock<IRegistrationRepository> regRepo,
        Guid retreatId,
        IEnumerable<Registration> confirmed,
        IEnumerable<Registration> payConf)
    {
        regRepo.Setup(r => r.ListAsync(
                retreatId,
                nameof(RegistrationStatus.Confirmed),
                null, 0, int.MaxValue,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(confirmed.ToList());

        regRepo.Setup(r => r.ListAsync(
                retreatId,
                nameof(RegistrationStatus.PaymentConfirmed),
                null, 0, int.MaxValue,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(payConf.ToList());
    }
    

    [Fact]
    public async Task Returns_unassigned_from_confirmed_plus_paymentconfirmed_distinct_by_id_and_sorted_by_name()
    {
        var (regRepo, linkRepo) = Mocks();
        var retreatId = Guid.NewGuid();

        var a = Reg(retreatId, "Ana Silva", Gender.Female, status: RegistrationStatus.Confirmed);
        var b = Reg(retreatId, "Bruno Garcia", Gender.Male, status: RegistrationStatus.PaymentConfirmed);
        var c = Reg(retreatId, "Carla Tom", Gender.Female, status: RegistrationStatus.Confirmed);
        
        SetupLists(regRepo, retreatId,
            confirmed: new[] { a, c },
            payConf:   new[] { b, c });

        
        linkRepo.Setup(l => l.ListByRetreatAsync(retreatId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<FamilyMember>());

        var handler = new GetUnassignedHandler(regRepo.Object, linkRepo.Object);

        var res = await handler.Handle(new GetUnassignedQuery(retreatId), default);

        res.Items.Select(i => i.Name).Should().ContainInOrder("Ana Silva", "Bruno Garcia", "Carla Tom");
        res.Items.Should().HaveCount(3);
        res.Items.Should().OnlyContain(i => i.Gender == "Female" || i.Gender == "Male");
        res.Items.Should().OnlyContain(i => !string.IsNullOrWhiteSpace(i.Email));
    }

    [Fact]
    public async Task Excludes_already_assigned()
    {
        var (regRepo, linkRepo) = Mocks();
        var retreatId = Guid.NewGuid();

        var a = Reg(retreatId, "Ana Silva", Gender.Female);
        var b = Reg(retreatId, "Bruno Garcia", Gender.Male);

        SetupLists(regRepo, retreatId,
            confirmed: new[] { a, b },
            payConf:   Array.Empty<Registration>());
        
        linkRepo.Setup(l => l.ListByRetreatAsync(retreatId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<FamilyMember> { Link(retreatId, Guid.NewGuid(), b.Id, 0) });

        var handler = new GetUnassignedHandler(regRepo.Object, linkRepo.Object);

        var res = await handler.Handle(new GetUnassignedQuery(retreatId), default);

        res.Items.Should().HaveCount(1);
        res.Items.Single().Name.Should().Be("Ana Silva");
    }

    [Fact]
    public async Task Filters_by_gender_city_and_search_on_name_email_and_cpf()
    {
        var (regRepo, linkRepo) = Mocks();
        var retreatId = Guid.NewGuid();

        var joao   = Reg(retreatId, "Joao da Silva", Gender.Male,   city: "Recife",     email: "joao@exemplo.com",  cpf: "12345678901");
        var maria  = Reg(retreatId, "Maria Souza",   Gender.Female, city: "Recife",     email: "maria@exemplo.com", cpf: "22233344455");
        var pedro  = Reg(retreatId, "Pedro Lima",    Gender.Male,   city: "São Paulo",  email: "pedro@exemplo.com", cpf: "99988877766");

        SetupLists(regRepo, retreatId,
            confirmed: new[] { joao, maria, pedro },
            payConf:   Array.Empty<Registration>());

        linkRepo.Setup(l => l.ListByRetreatAsync(retreatId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<FamilyMember>());

        var handler = new GetUnassignedHandler(regRepo.Object, linkRepo.Object);
        
        var resGender = await handler.Handle(new GetUnassignedQuery(retreatId, Gender: "male"), default);
        resGender.Items.Should().HaveCount(2);
        resGender.Items.Should().OnlyContain(i => i.Gender == "Male");
        
        var resCity = await handler.Handle(new GetUnassignedQuery(retreatId, City: "  recIfE "), default);
        resCity.Items.Should().HaveCount(2);
        resCity.Items.Select(i => i.Name).Should().BeEquivalentTo(new[] { "Joao da Silva", "Maria Souza" });
        
        var resSearchName = await handler.Handle(new GetUnassignedQuery(retreatId, Search: "lima"), default);
        resSearchName.Items.Should().HaveCount(1);
        resSearchName.Items.Single().Name.Should().Be("Pedro Lima");
        
        var resSearchEmail = await handler.Handle(new GetUnassignedQuery(retreatId, Search: "maria@exemplo.com"), default);
        resSearchEmail.Items.Should().HaveCount(1);
        resSearchEmail.Items.Single().Name.Should().Be("Maria Souza");

        
        var resSearchCpf = await handler.Handle(new GetUnassignedQuery(retreatId, Search: "12345678901"), default);
        resSearchCpf.Items.Should().HaveCount(1);
        resSearchCpf.Items.Single().Name.Should().Be("Joao da Silva");
    }

    [Fact]
    public async Task When_all_filtered_out_returns_empty_list()
    {
        var (regRepo, linkRepo) = Mocks();
        var retreatId = Guid.NewGuid();

        var r = Reg(retreatId, "Zezinho Jabuci", Gender.Male, city: "Rio");
        SetupLists(regRepo, retreatId, confirmed: new[] { r }, payConf: Array.Empty<Registration>());
        
        linkRepo.Setup(l => l.ListByRetreatAsync(retreatId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<FamilyMember> { Link(retreatId, Guid.NewGuid(), r.Id, 0) });

        var handler = new GetUnassignedHandler(regRepo.Object, linkRepo.Object);

        var res = await handler.Handle(new GetUnassignedQuery(retreatId, Gender: "Female", City: "Recife", Search: "nada"), default);

        res.Items.Should().BeEmpty();
    }
}
