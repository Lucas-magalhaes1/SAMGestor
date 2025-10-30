using System.Runtime.Serialization;
using FluentAssertions;
using Moq;
using SAMGestor.Application.Features.Tents.TentRoster.Unassigned;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.UnitTests.Application.Features.Tents.TentRoster.Unassigned;

public class GetTentUnassignedHandlerTests
{
    private static Registration MakeReg(Guid retreatId, string name, Gender gender, string? city = null)
    {
        var r = (Registration)FormatterServices.GetUninitializedObject(typeof(Registration));
        typeof(Registration).GetProperty("Id")!.SetValue(r, Guid.NewGuid());
        typeof(Registration).GetProperty("RetreatId")!.SetValue(r, retreatId);
        var safe = string.IsNullOrWhiteSpace(name) || !name.Contains(' ') ? $"{name} Silva" : name;
        typeof(Registration).GetProperty("Name")!.SetValue(r, new FullName(safe));
        typeof(Registration).GetProperty("Gender")!.SetValue(r, gender);
        typeof(Registration).GetProperty("City")!.SetValue(r, city);
        return r;
    }

    [Fact]
    public async Task Should_forward_gender_and_search_and_map_items()
    {
        var retId = Guid.NewGuid();
        var a = MakeReg(retId, "Ana Souza", Gender.Female, "SP");
        var b = MakeReg(retId, "Bruno Dias", Gender.Male, "RJ");

        var repo = new Mock<IRegistrationRepository>();
        repo.Setup(r => r.ListPaidUnassignedAsync(retId, It.Is<Gender?>(g => g == Gender.Female), "ana", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Registration> { a });

        var handler = new GetTentUnassignedHandler(repo.Object);

        var res = await handler.Handle(new GetTentUnassignedQuery(retId, Gender: "Female", Search: "ana"), CancellationToken.None);

        res.RetreatId.Should().Be(retId);
        res.Items.Should().HaveCount(1);
        res.Items[0].RegistrationId.Should().Be(a.Id);
        res.Items[0].Name.Should().Be((string)a.Name);
        res.Items[0].Gender.Should().Be("Female");
        res.Items[0].City.Should().Be("SP");

        repo.Verify(r => r.ListPaidUnassignedAsync(retId, It.Is<Gender?>(g => g == Gender.Female), "ana", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Invalid_gender_should_query_with_null_and_sort_by_name_within_gender()
    {
        var retId = Guid.NewGuid();
        var f1 = MakeReg(retId, "Zara Lima", Gender.Female, "SP");
        var f2 = MakeReg(retId, "Ana Souza", Gender.Female, "SP");
        var m1 = MakeReg(retId, "Carlos Braga", Gender.Male, "RJ");

        var repo = new Mock<IRegistrationRepository>();
        repo.Setup(r => r.ListPaidUnassignedAsync(retId, It.Is<Gender?>(g => g == null), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Registration> { f1, f2, m1 });

        var handler = new GetTentUnassignedHandler(repo.Object);

        var res = await handler.Handle(new GetTentUnassignedQuery(retId, Gender: "???", Search: null), CancellationToken.None);

        res.Items.Should().HaveCount(3);
        var females = res.Items.Where(i => i.Gender == "Female").Select(i => i.Name).ToList();
        females.Should().BeInAscendingOrder();
        var males = res.Items.Where(i => i.Gender == "Male").Select(i => i.Name).ToList();
        males.Should().BeInAscendingOrder();

        repo.Verify(r => r.ListPaidUnassignedAsync(retId, It.Is<Gender?>(g => g == null), null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Male_filter_should_return_only_males()
    {
        var retId = Guid.NewGuid();
        var m1 = MakeReg(retId, "Joao Silva", Gender.Male, "SP");
        var m2 = MakeReg(retId, "Pedro Lima", Gender.Male, "RJ");

        var repo = new Mock<IRegistrationRepository>();
        repo.Setup(r => r.ListPaidUnassignedAsync(retId, It.Is<Gender?>(g => g == Gender.Male), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Registration> { m1, m2 });

        var handler = new GetTentUnassignedHandler(repo.Object);

        var res = await handler.Handle(new GetTentUnassignedQuery(retId, Gender: "male"), CancellationToken.None);

        res.Items.Should().HaveCount(2);
        res.Items.All(i => i.Gender == "Male").Should().BeTrue();

        repo.Verify(r => r.ListPaidUnassignedAsync(retId, It.Is<Gender?>(g => g == Gender.Male), null, It.IsAny<CancellationToken>()), Times.Once);
    }
}
