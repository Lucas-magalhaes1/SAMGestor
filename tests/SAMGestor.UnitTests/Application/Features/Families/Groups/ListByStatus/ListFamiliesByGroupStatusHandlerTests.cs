using FluentAssertions;
using Moq;
using SAMGestor.Application.Features.Families.Groups.ListByStatus;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.UnitTests.Application.Features.Families.Groups.ListByStatus;

public class ListFamiliesByGroupStatusHandlerTests
{
    private static Retreat OpenRetreat()
        => new Retreat(
            new FullName("Retiro Listagem"), "ED1", "Tema",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(12)),
            50, 50,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
            new Money(0,"BRL"), new Money(0,"BRL"),
            new Percentage(50), new Percentage(50));

    private static Family Fam(Guid retreatId, string name, GroupStatus status = GroupStatus.None)
    {
        var f = new Family(new FamilyName(name), retreatId, capacity: 4);
        switch (status)
        {
            case GroupStatus.Creating:
                f.MarkGroupCreating();
                break;
            case GroupStatus.Active:
                f.MarkGroupActive(
                    link: $"https://chat/{Guid.NewGuid()}",
                    externalId: "ext",
                    channel: "whatsapp",
                    createdAt: DateTimeOffset.UtcNow.AddMinutes(-5),
                    notifiedAt: null);
                break;
            case GroupStatus.Failed:
                f.MarkGroupFailed();
                break;
            case GroupStatus.None:
            default:
                break;
        }
        return f;
    }

    private static ListFamiliesByGroupStatusHandler BuildHandler(
        Retreat retreat,
        List<Family> families,
        out Mock<IRetreatRepository> retRepo,
        out Mock<IFamilyRepository> famRepo)
    {
        retRepo = new Mock<IRetreatRepository>();
        famRepo = new Mock<IFamilyRepository>();

        retRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(retreat);

        famRepo.Setup(r => r.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(families);

        return new ListFamiliesByGroupStatusHandler(retRepo.Object, famRepo.Object);
    }
    

    [Fact]
    public async Task Falha_quando_retiro_nao_existe()
    {
        var retreatId = Guid.NewGuid();

        var retRepo = new Mock<IRetreatRepository>();
        var famRepo = new Mock<IFamilyRepository>();

        retRepo.Setup(r => r.GetByIdAsync(retreatId, It.IsAny<CancellationToken>()))
               .ReturnsAsync((Retreat?)null);

        var sut = new ListFamiliesByGroupStatusHandler(retRepo.Object, famRepo.Object);

        await FluentActions.Invoking(() => sut.Handle(new ListFamiliesByGroupStatusQuery(retreatId, Status: null), default))
            .Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Retreat*");
    }

    [Fact]
    public async Task Retorna_lista_vazia_quando_nao_ha_familias()
    {
        var retreat = OpenRetreat();

        var sut = BuildHandler(retreat, new List<Family>(), out var retRepo, out var famRepo);

        var res = await sut.Handle(new ListFamiliesByGroupStatusQuery(retreat.Id, Status: null), default);

        res.Items.Should().BeEmpty();

        retRepo.Verify(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()), Times.Once);
        famRepo.Verify(r => r.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Sem_filtro_Status_null_retorna_todas_ordenadas_por_nome()
    {
        var retreat = OpenRetreat();

        var f3 = Fam(retreat.Id, "Zeta Família", GroupStatus.Active);
        var f1 = Fam(retreat.Id, "Alfa Família", GroupStatus.None);
        var f2 = Fam(retreat.Id, "Beta Família", GroupStatus.Creating);

        var sut = BuildHandler(retreat, new List<Family> { f3, f1, f2 }, out _, out _);

        var res = await sut.Handle(new ListFamiliesByGroupStatusQuery(retreat.Id, Status: null), default);

        res.Items.Select(i => i.Name).Should().ContainInOrder("Alfa Família", "Beta Família", "Zeta Família");
        res.Items.Should().HaveCount(3);
    }

    [Fact]
    public async Task Filtro_vazio_string_vazia_equivale_a_sem_filtro()
    {
        var retreat = OpenRetreat();
        var f1 = Fam(retreat.Id, "B", GroupStatus.Failed);
        var f2 = Fam(retreat.Id, "A", GroupStatus.None);

        var sut = BuildHandler(retreat, new List<Family> { f1, f2 }, out _, out _);

        var res = await sut.Handle(new ListFamiliesByGroupStatusQuery(retreat.Id, Status: ""), default);

        res.Items.Select(i => i.Name).Should().ContainInOrder("A", "B");
        res.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Filtro_desconhecido_retorna_tudo()
    {
        var retreat = OpenRetreat();
        var f1 = Fam(retreat.Id, "A", GroupStatus.Creating);
        var f2 = Fam(retreat.Id, "B", GroupStatus.Failed);

        var sut = BuildHandler(retreat, new List<Family> { f2, f1 }, out _, out _);

        var res = await sut.Handle(new ListFamiliesByGroupStatusQuery(retreat.Id, Status: "weird"), default);

        res.Items.Should().HaveCount(2);
        res.Items.Select(i => i.Name).Should().ContainInOrder("A", "B");
    }

    [Fact]
    public async Task Filtro_none_retorna_apenas_Status_None()
    {
        var retreat = OpenRetreat();
        var none1 = Fam(retreat.Id, "Fam 1", GroupStatus.None);
        var none2 = Fam(retreat.Id, "Fam 2", GroupStatus.None);
        var other = Fam(retreat.Id, "Fam 3", GroupStatus.Active);

        var sut = BuildHandler(retreat, new List<Family> { other, none2, none1 }, out _, out _);

        var res = await sut.Handle(new ListFamiliesByGroupStatusQuery(retreat.Id, Status: "none"), default);

        res.Items.Should().HaveCount(2);
        res.Items.All(i => i.GroupStatus == GroupStatus.None.ToString()).Should().BeTrue();
        res.Items.Select(i => i.Name).Should().ContainInOrder("Fam 1", "Fam 2");
    }

    [Fact]
    public async Task Filtro_creating_retorna_apenas_Status_Creating()
    {
        var retreat = OpenRetreat();
        var c1 = Fam(retreat.Id, "Fam A", GroupStatus.Creating);
        var c2 = Fam(retreat.Id, "Fam B", GroupStatus.Creating);
        var n1 = Fam(retreat.Id, "Fam C", GroupStatus.None);

        var sut = BuildHandler(retreat, new List<Family> { n1, c2, c1 }, out _, out _);

        var res = await sut.Handle(new ListFamiliesByGroupStatusQuery(retreat.Id, Status: "creating"), default);

        res.Items.Should().HaveCount(2);
        res.Items.All(i => i.GroupStatus == GroupStatus.Creating.ToString()).Should().BeTrue();
        res.Items.Select(i => i.Name).Should().ContainInOrder("Fam A", "Fam B");
    }

    [Fact]
    public async Task Filtro_active_case_insensitive()
    {
        var retreat = OpenRetreat();
        var a1 = Fam(retreat.Id, "Fam A", GroupStatus.Active);
        var a2 = Fam(retreat.Id, "Fam B", GroupStatus.Active);
        var f1 = Fam(retreat.Id, "Fam C", GroupStatus.Failed);

        var sut = BuildHandler(retreat, new List<Family> { f1, a2, a1 }, out _, out _);

        var res = await sut.Handle(new ListFamiliesByGroupStatusQuery(retreat.Id, Status: "AcTiVe"), default);

        res.Items.Should().HaveCount(2);
        res.Items.All(i => i.GroupStatus == GroupStatus.Active.ToString()).Should().BeTrue();
        res.Items.Select(i => i.Name).Should().ContainInOrder("Fam A", "Fam B");
        
        res.Items[0].GroupLink.Should().NotBeNull();
        res.Items[0].GroupChannel.Should().Be("whatsapp");
    }

    [Fact]
    public async Task Filtro_failed_retorna_apenas_Status_Failed()
    {
        var retreat = OpenRetreat();
        var f1 = Fam(retreat.Id, "Fam 1", GroupStatus.Failed);
        var f2 = Fam(retreat.Id, "Fam 2", GroupStatus.Failed);
        var a1 = Fam(retreat.Id, "Fam 3", GroupStatus.Active);

        var sut = BuildHandler(retreat, new List<Family> { a1, f2, f1 }, out _, out _);

        var res = await sut.Handle(new ListFamiliesByGroupStatusQuery(retreat.Id, Status: "failed"), default);

        res.Items.Should().HaveCount(2);
        res.Items.All(i => i.GroupStatus == GroupStatus.Failed.ToString()).Should().BeTrue();
        res.Items.Select(i => i.Name).Should().ContainInOrder("Fam 1", "Fam 2");
    }

    [Fact]
    public async Task Mapeia_campos_no_payload_da_resposta_corretamente()
    {
        var retreat = OpenRetreat();
        var f = Fam(retreat.Id, "Fam X", GroupStatus.Active);

        var sut = BuildHandler(retreat, new List<Family> { f }, out _, out _);

        var res = await sut.Handle(new ListFamiliesByGroupStatusQuery(retreat.Id, Status: "active"), default);

        var item = res.Items.Single();
        item.FamilyId.Should().Be(f.Id);
        item.Name.Should().Be((string)f.Name);
        item.GroupStatus.Should().Be(GroupStatus.Active.ToString());
        item.GroupLink.Should().Be(f.GroupLink);
        item.GroupExternalId.Should().Be(f.GroupExternalId);
        item.GroupChannel.Should().Be(f.GroupChannel);
        item.GroupCreatedAt.Should().Be(f.GroupCreatedAt);
        item.GroupLastNotifiedAt.Should().Be(f.GroupLastNotifiedAt);
        item.GroupVersion.Should().Be(f.GroupVersion);
    }
}
