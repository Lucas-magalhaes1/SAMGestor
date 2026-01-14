using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using SAMGestor.Application.Features.Families.GetAll;
using SAMGestor.Application.Common.Families;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;
using Xunit;

namespace SAMGestor.UnitTests.Application.Features.Families.GetAll;

public class GetAllFamiliesQueryTests
{
    private static Retreat NewRetreat(bool locked = false, int familiesVersion = 3)
    {
        var r = new Retreat(
            new FullName("Retiro Unidade Teste"),
            "ED1",
            "Tema",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(33)),
            50, 50,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
            new Money(0, "BRL"),
            new Money(0, "BRL"),
            new Percentage(50),
            new Percentage(50));
        
        for (int i = 0; i < familiesVersion; i++) r.BumpFamiliesVersion();
        if (locked) r.LockFamilies();
        return r;
    }

    private static Family NewFamily(Guid retreatId, string name = "Família 1", int capacity = 4, string colorName = "Azul")
    {
        var color = FamilyColor.FromName(colorName);
        return new Family(new FamilyName(name), retreatId, capacity, color);
    }

    private static Registration NewReg(Guid retreatId, string name, Gender g, string city)
        => new Registration(
            new FullName(name),
            new CPF(RandomCpf()),
            new EmailAddress($"{Guid.NewGuid():N}@ex.com"),
            "11999999999",
            new DateOnly(1990, 1, 1),
            g,
            city,
            RegistrationStatus.Confirmed,
            retreatId);

    private static FamilyMember Link(Guid retreatId, Guid familyId, Guid regId, int pos, bool isPadrinho = false, bool isMadrinha = false)
        => new FamilyMember(retreatId, familyId, regId, pos, isPadrinho, isMadrinha);

    private static string RandomCpf()
    {
        var rnd = new Random();
        return string.Concat(Enumerable.Range(0, 11).Select(_ => rnd.Next(0, 9))).PadRight(11, '0');
    }

    [Fact]
    public async Task Return_empty_when_retreat_not_found()
    {
        var retreatRepo = new Mock<IRetreatRepository>();
        retreatRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync((Retreat?)null);

        var familyRepo = new Mock<IFamilyRepository>();
        var fmRepo     = new Mock<IFamilyMemberRepository>();
        var regRepo    = new Mock<IRegistrationRepository>();

        var handler = new GetAllFamiliesHandler(retreatRepo.Object, familyRepo.Object, fmRepo.Object, regRepo.Object);

        var res = await handler.Handle(new GetAllFamiliesQuery(Guid.NewGuid(), IncludeAlerts: true), CancellationToken.None);
        
        res.Version.Should().Be(0);
        res.FamiliesLocked.Should().BeFalse();
        res.Families.Should().BeEmpty();
    }

    [Fact]
    public async Task Return_empty_when_no_families_but_retreat_exists()
    {
        var retreat = NewRetreat(locked: false, familiesVersion: 5);

        var retreatRepo = new Mock<IRetreatRepository>();
        retreatRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(retreat);

        var familyRepo = new Mock<IFamilyRepository>();
        familyRepo.Setup(r => r.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(Array.Empty<Family>());

        var fmRepo  = new Mock<IFamilyMemberRepository>();
        var regRepo = new Mock<IRegistrationRepository>();

        var handler = new GetAllFamiliesHandler(retreatRepo.Object, familyRepo.Object, fmRepo.Object, regRepo.Object);
        
        var res = await handler.Handle(new GetAllFamiliesQuery(retreat.Id, IncludeAlerts: true), CancellationToken.None);
        
        res.Version.Should().Be(retreat.FamiliesVersion);
        res.FamiliesLocked.Should().Be(retreat.FamiliesLocked);
        res.Families.Should().BeEmpty();
    }

    [Fact]
    public async Task Returns_families_with_members_and_alerts_when_includeAlerts_true()
    {
        var retreat = NewRetreat(locked: true, familiesVersion: 2); 

        var f1 = NewFamily(retreat.Id, "Família 1", colorName: "Azul");
        var f2 = NewFamily(retreat.Id, "Família 2", colorName: "Verde");
        
        var r1 = NewReg(retreat.Id, "João Silva", Gender.Male,   "São Paulo");
        var r2 = NewReg(retreat.Id, "Maria Souza", Gender.Female, "São Paulo");
        var r3 = NewReg(retreat.Id, "Pedro Lima",  Gender.Male,   "Recife");
        var r4 = NewReg(retreat.Id, "Ana Lima",    Gender.Female, "Recife");

        var l1 = Link(retreat.Id, f1.Id, r1.Id, 0, isPadrinho: true);
        var l2 = Link(retreat.Id, f1.Id, r2.Id, 1, isMadrinha: true);
        var l3 = Link(retreat.Id, f1.Id, r3.Id, 2);
        var l4 = Link(retreat.Id, f1.Id, r4.Id, 3);
        var linksByFamily = new Dictionary<Guid, List<FamilyMember>>
        {
            [f1.Id] = new() { l1, l2, l3, l4 },
            [f2.Id] = new() 
        };
        
        var retreatRepo = new Mock<IRetreatRepository>();
        retreatRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(retreat);

        var familyRepo = new Mock<IFamilyRepository>();
        familyRepo.Setup(r => r.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new List<Family> { f1, f2 });

        var fmRepo = new Mock<IFamilyMemberRepository>();
        fmRepo.Setup(r => r.ListByFamilyIdsAsync(It.Is<IEnumerable<Guid>>(ids =>
                            ids.Contains(f1.Id) && ids.Contains(f2.Id)), It.IsAny<CancellationToken>()))
              .ReturnsAsync(linksByFamily);

        var regRepo = new Mock<IRegistrationRepository>();
        regRepo.Setup(r => r.GetMapByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Dictionary<Guid, Registration>
               {
                   [r1.Id] = r1, [r2.Id] = r2, [r3.Id] = r3, [r4.Id] = r4
               });

        var handler = new GetAllFamiliesHandler(retreatRepo.Object, familyRepo.Object, fmRepo.Object, regRepo.Object);
        
        var res = await handler.Handle(new GetAllFamiliesQuery(retreat.Id, IncludeAlerts: true), CancellationToken.None);
        
        res.Version.Should().Be(retreat.FamiliesVersion);
        res.FamiliesLocked.Should().BeTrue();
        
        var fam1 = res.Families.Single(f => f.FamilyId == f1.Id);
        fam1.Name.Should().Be("Família 1");
        fam1.ColorName.Should().Be("Azul");
        fam1.ColorHex.Should().NotBeNullOrEmpty();
        fam1.Capacity.Should().Be(4);
        fam1.TotalMembers.Should().Be(4);
        fam1.MaleCount.Should().Be(2);
        fam1.FemaleCount.Should().Be(2);
        fam1.MalePercentage.Should().Be(50m);
        fam1.FemalePercentage.Should().Be(50m);
        fam1.Remaining.Should().Be(0);
        fam1.Members.Select(m => m.Position).Should().BeInAscendingOrder().And.OnlyHaveUniqueItems();
        
        // Validar email/phone
        fam1.Members.Should().AllSatisfy(m =>
        {
            m.Email.Should().NotBeNullOrEmpty();
            m.Phone.Should().NotBeNullOrEmpty();
        });
        
        // Validar padrinhos/madrinhas
        fam1.Members.Count(m => m.IsPadrinho).Should().Be(1);
        fam1.Members.Count(m => m.IsMadrinha).Should().Be(1);
        
        fam1.Alerts.Should().NotBeNull();
        fam1.Alerts.Any(a => a.Code == "SAME_CITY").Should().BeTrue();
        
        var fam2 = res.Families.Single(f => f.FamilyId == f2.Id);
        fam2.ColorName.Should().Be("Verde");
        fam2.TotalMembers.Should().Be(0);
        fam2.Remaining.Should().Be(4);
        fam2.Alerts.Should().NotBeNull(); 
    }

    [Fact]
    public async Task Returns_without_alerts_when_includeAlerts_false()
    {
        var retreat = NewRetreat(locked: false, familiesVersion: 7);

        var f1 = NewFamily(retreat.Id, "Família 1", colorName: "Roxo");
        var r1 = NewReg(retreat.Id, "João Silva", Gender.Male, "SP");
        var l1 = Link(retreat.Id, f1.Id, r1.Id, 0);

        var retreatRepo = new Mock<IRetreatRepository>();
        retreatRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(retreat);

        var familyRepo = new Mock<IFamilyRepository>();
        familyRepo.Setup(r => r.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new List<Family> { f1 });

        var fmRepo = new Mock<IFamilyMemberRepository>();
        fmRepo.Setup(r => r.ListByFamilyIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new Dictionary<Guid, List<FamilyMember>> { [f1.Id] = new() { l1 } });

        var regRepo = new Mock<IRegistrationRepository>();
        regRepo.Setup(r => r.GetMapByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Dictionary<Guid, Registration> { [r1.Id] = r1 });

        var handler = new GetAllFamiliesHandler(retreatRepo.Object, familyRepo.Object, fmRepo.Object, regRepo.Object);
        
        var res = await handler.Handle(new GetAllFamiliesQuery(retreat.Id, IncludeAlerts: false), CancellationToken.None);
        
        res.Version.Should().Be(retreat.FamiliesVersion);
        res.FamiliesLocked.Should().BeFalse();

        var fam = res.Families.Single();
        fam.ColorName.Should().Be("Roxo");
        fam.Alerts.Should().NotBeNull();
        fam.Alerts.Should().BeEmpty(); 
    }
}
