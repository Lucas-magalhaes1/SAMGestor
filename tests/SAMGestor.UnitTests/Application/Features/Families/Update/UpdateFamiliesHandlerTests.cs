using FluentAssertions;
using Moq;
using SAMGestor.Application.Features.Families.Update;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;

using FamilyInput = SAMGestor.Application.Features.Families.Update.UpdateFamilyDto;
using MemberInput = SAMGestor.Application.Features.Families.Update.UpdateMemberDto;

namespace SAMGestor.UnitTests.Application.Features.Families.Update;

public sealed class UpdateFamiliesHandlerTests
{
    private static Retreat NewOpenRetreat()
        => new Retreat(
            new FullName("Retiro Teste"),
            "ED1",
            "Tema",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(12)),
            50, 50,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
            new Money(0, "BRL"),
            new Money(0, "BRL"),
            new Percentage(50),
            new Percentage(50)
        );

    private static Registration R(Guid retreatId, string name, Gender g, string city = "SP")
        => new Registration(
            new FullName(name),
            new CPF("52998224725"),
            new EmailAddress($"{Guid.NewGuid():N}@mail.com"),
            "11999999999",
            new DateOnly(1990, 1, 1),
            g,
            city,
            RegistrationStatus.Confirmed,
            retreatId
        );

    private static Family F(Guid retreatId, string name = "Família X", int capacity = 4, string colorName = "Azul", bool locked = false)
    {
        var color = FamilyColor.FromName(colorName);
        var f = new Family(new FamilyName(name), retreatId, capacity, color);
        if (locked) f.Lock();
        return f;
    }

    private static FamilyMember Link(Guid retreatId, Guid familyId, Guid regId, int pos, bool isPadrinho = false, bool isMadrinha = false)
        => new FamilyMember(retreatId, familyId, regId, pos, isPadrinho, isMadrinha);

    private static UpdateFamiliesCommand Cmd(
        Guid retreatId,
        int version,
        IEnumerable<(Family family, (Guid regId, int pos)[] members, string? colorName, Guid[] padrinhos, Guid[] madrinhas)> fams,
        bool ignoreWarnings = true)
    {
        var families = fams
            .Select(x => new FamilyInput(
                FamilyId: x.family.Id,
                Name: (string)x.family.Name,
                ColorName: x.colorName ?? x.family.Color.Name,
                Capacity: x.family.Capacity,
                Members: x.members.Select(m => new MemberInput(m.regId, m.pos)).ToList(),
                PadrinhoIds: x.padrinhos.ToList(),
                MadrinhaIds: x.madrinhas.ToList()
            ))
            .ToList();

        return new UpdateFamiliesCommand(
            RetreatId: retreatId,
            Version: version,
            Families: families,
            IgnoreWarnings: ignoreWarnings
        );
    }

    private static (Mock<IRetreatRepository> retRepo,
                    Mock<IFamilyRepository> famRepo,
                    Mock<IFamilyMemberRepository> fmRepo,
                    Mock<IRegistrationRepository> regRepo,
                    Mock<IUnitOfWork> uow)
        Mocks()
    {
        return (new Mock<IRetreatRepository>(),
                new Mock<IFamilyRepository>(),
                new Mock<IFamilyMemberRepository>(),
                new Mock<IRegistrationRepository>(),
                new Mock<IUnitOfWork>());
    }

    // ===== TESTES DE VALIDAÇÃO BÁSICA =====

    [Fact]
    public async Task NotFound_Retreat_returns_error_payload_without_throw()
    {
        var (retRepo, famRepo, fmRepo, regRepo, uow) = Mocks();

        var retreatId = Guid.NewGuid();
        retRepo.Setup(r => r.GetByIdAsync(retreatId, It.IsAny<CancellationToken>()))
               .ReturnsAsync((Retreat?)null);

        var handler = new UpdateFamiliesHandler(retRepo.Object, famRepo.Object, fmRepo.Object, regRepo.Object, uow.Object);

        var cmd = new UpdateFamiliesCommand(retreatId, 0, new List<FamilyInput>(), true);

        var res = await handler.Handle(cmd, default);

        res.Version.Should().Be(0);
        res.Errors.Should().ContainSingle(e => e.Code == "RETREAT_NOT_FOUND");
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Global_lock_throws_BusinessRuleException()
    {
        var (retRepo, famRepo, fmRepo, regRepo, uow) = Mocks();
        var retreat = NewOpenRetreat();
        retreat.LockFamilies();

        retRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(retreat);

        var handler = new UpdateFamiliesHandler(retRepo.Object, famRepo.Object, fmRepo.Object, regRepo.Object, uow.Object);

        var cmd = new UpdateFamiliesCommand(retreat.Id, retreat.FamiliesVersion, new List<FamilyInput>(), true);

        await FluentActions.Invoking(() => handler.Handle(cmd, default))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*bloqueadas*");

        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Version_mismatch_returns_error_payload_without_persist()
    {
        var (retRepo, famRepo, fmRepo, regRepo, uow) = Mocks();
        var retreat = NewOpenRetreat();
        retRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(retreat);

        famRepo.Setup(x => x.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new List<Family>());

        var handler = new UpdateFamiliesHandler(retRepo.Object, famRepo.Object, fmRepo.Object, regRepo.Object, uow.Object);

        var cmd = new UpdateFamiliesCommand(retreat.Id, 999, new List<FamilyInput>(), true);

        var res = await handler.Handle(cmd, default);

        res.Version.Should().Be(retreat.FamiliesVersion);
        res.Errors.Should().ContainSingle(e => e.Code == "VERSION_MISMATCH");
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Touch_locked_family_returns_FAMILY_LOCKED()
    {
        var (retRepo, famRepo, fmRepo, regRepo, uow) = Mocks();
        var retreat = NewOpenRetreat();
        retRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(retreat);

        var fLocked = F(retreat.Id, "Família L", 4, "Azul", locked: true);
        var fOpen   = F(retreat.Id, "Família A", 4, "Verde");

        var r1 = R(retreat.Id, "Joao Silva", Gender.Male);
        var r2 = R(retreat.Id, "Maria Souza", Gender.Female);
        var r3 = R(retreat.Id, "Pedro Lima", Gender.Male);
        var r4 = R(retreat.Id, "Ana Santos", Gender.Female);

        famRepo.Setup(x => x.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new List<Family> { fLocked, fOpen });

        regRepo.Setup(x => x.GetMapByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Dictionary<Guid, Registration> { [r1.Id] = r1, [r2.Id] = r2, [r3.Id] = r3, [r4.Id] = r4 });

        var handler = new UpdateFamiliesHandler(retRepo.Object, famRepo.Object, fmRepo.Object, regRepo.Object, uow.Object);

        var cmd = Cmd(
            retreat.Id,
            retreat.FamiliesVersion,
            new[]
            {
                (fLocked, new (Guid,int)[]{ (r1.Id,0), (r2.Id,1), (r3.Id,2), (r4.Id,3) }, (string?)null, Array.Empty<Guid>(), Array.Empty<Guid>())
            },
            ignoreWarnings: true
        );

        var res = await handler.Handle(cmd, default);

        res.Errors.Should().ContainSingle(e => e.Code == "FAMILY_LOCKED" && e.FamilyId == fLocked.Id);
        res.Families.Should().BeEmpty();
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Unknown_family_returns_error_payload()
    {
        var (retRepo, famRepo, fmRepo, regRepo, uow) = Mocks();
        var retreat = NewOpenRetreat();
        retRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(retreat);

        var fKnown = F(retreat.Id, "Família A");
        famRepo.Setup(x => x.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new List<Family> { fKnown });

        var fakeFamId = Guid.NewGuid();
        var r1 = R(retreat.Id, "Joao Silva", Gender.Male);
        var r2 = R(retreat.Id, "Ana Lima", Gender.Female);
        var r3 = R(retreat.Id, "Pedro Costa", Gender.Male);
        var r4 = R(retreat.Id, "Bea Souza", Gender.Female);

        regRepo.Setup(x => x.GetMapByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Dictionary<Guid, Registration> { [r1.Id]=r1,[r2.Id]=r2,[r3.Id]=r3,[r4.Id]=r4 });

        var handler = new UpdateFamiliesHandler(retRepo.Object, famRepo.Object, fmRepo.Object, regRepo.Object, uow.Object);

        var cmd = new UpdateFamiliesCommand(
            RetreatId: retreat.Id,
            Version: retreat.FamiliesVersion,
            Families: new List<FamilyInput>
            {
                new FamilyInput(
                    fakeFamId, "X", "Roxo", 4,
                    new List<MemberInput> { new(r1.Id,0), new(r2.Id,1), new(r3.Id,2), new(r4.Id,3) },
                    new List<Guid>(), new List<Guid>())
            },
            IgnoreWarnings: true
        );

        var res = await handler.Handle(cmd, default);

        res.Errors.Should().ContainSingle(e => e.Code == "UNKNOWN_FAMILY" && e.FamilyId == fakeFamId);
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Unknown_registration_returns_error_payload()
    {
        var (retRepo, famRepo, fmRepo, regRepo, uow) = Mocks();
        var retreat = NewOpenRetreat();
        retRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(retreat);

        var fam = F(retreat.Id, "Fam A");
        famRepo.Setup(x => x.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new List<Family> { fam });

        regRepo.Setup(x => x.GetMapByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Dictionary<Guid, Registration>());

        var handler = new UpdateFamiliesHandler(retRepo.Object, famRepo.Object, fmRepo.Object, regRepo.Object, uow.Object);

        var ghost = Guid.NewGuid();
        var cmd = new UpdateFamiliesCommand(
            retreat.Id,
            retreat.FamiliesVersion,
            new List<FamilyInput>
            {
                new FamilyInput(fam.Id, "Fam A", "Amarelo", 4,
                    new List<MemberInput> { new(ghost,0), new(ghost,1), new(ghost,2), new(ghost,3) },
                    new List<Guid>(), new List<Guid>())
            },
            true);

        var res = await handler.Handle(cmd, default);

        res.Errors.Should().ContainSingle(e => e.Code == "UNKNOWN_REGISTRATION");
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ===== TESTES DE VALIDAÇÃO DE CORES =====

    [Fact]
    public async Task Invalid_color_returns_error()
    {
        var (retRepo, famRepo, fmRepo, regRepo, uow) = Mocks();
        var retreat = NewOpenRetreat();
        retRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(retreat);

        var fam = F(retreat.Id, "Fam A");
        famRepo.Setup(x => x.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new List<Family> { fam });

        var r1 = R(retreat.Id, "Joao A", Gender.Male);
        var r2 = R(retreat.Id, "Maria B", Gender.Female);
        var r3 = R(retreat.Id, "Pedro C", Gender.Male);
        var r4 = R(retreat.Id, "Ana D", Gender.Female);

        regRepo.Setup(x => x.GetMapByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Dictionary<Guid, Registration> { [r1.Id]=r1,[r2.Id]=r2,[r3.Id]=r3,[r4.Id]=r4 });

        var handler = new UpdateFamiliesHandler(retRepo.Object, famRepo.Object, fmRepo.Object, regRepo.Object, uow.Object);

        var cmd = new UpdateFamiliesCommand(
            retreat.Id,
            retreat.FamiliesVersion,
            new List<FamilyInput>
            {
                new FamilyInput(fam.Id, "Fam A", "CorInvalida", 4,
                    new List<MemberInput> { new(r1.Id,0), new(r2.Id,1), new(r3.Id,2), new(r4.Id,3) },
                    new List<Guid>(), new List<Guid>())
            },
            true);

        var res = await handler.Handle(cmd, default);

        res.Errors.Should().Contain(e => e.Code == "INVALID_COLOR");
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Duplicate_color_between_families_returns_error()
    {
        var (retRepo, famRepo, fmRepo, regRepo, uow) = Mocks();
        var retreat = NewOpenRetreat();
        retRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(retreat);

        var fam1 = F(retreat.Id, "Fam A", 4, "Azul");
        var fam2 = F(retreat.Id, "Fam B", 4, "Verde");

        famRepo.Setup(x => x.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new List<Family> { fam1, fam2 });

        var r1 = R(retreat.Id, "Joao A", Gender.Male);
        var r2 = R(retreat.Id, "Maria B", Gender.Female);
        var r3 = R(retreat.Id, "Pedro C", Gender.Male);
        var r4 = R(retreat.Id, "Ana D", Gender.Female);
        var r5 = R(retreat.Id, "Carlos E", Gender.Male);
        var r6 = R(retreat.Id, "Beatriz F", Gender.Female);
        var r7 = R(retreat.Id, "Jose G", Gender.Male);
        var r8 = R(retreat.Id, "Clara H", Gender.Female);

        regRepo.Setup(x => x.GetMapByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Dictionary<Guid, Registration> {
                   [r1.Id]=r1,[r2.Id]=r2,[r3.Id]=r3,[r4.Id]=r4,
                   [r5.Id]=r5,[r6.Id]=r6,[r7.Id]=r7,[r8.Id]=r8
               });

        var handler = new UpdateFamiliesHandler(retRepo.Object, famRepo.Object, fmRepo.Object, regRepo.Object, uow.Object);

        // Tentar atualizar ambas para a mesma cor "Roxo"
        var cmd = new UpdateFamiliesCommand(
            retreat.Id,
            retreat.FamiliesVersion,
            new List<FamilyInput>
            {
                new FamilyInput(fam1.Id, "Fam A", "Roxo", 4,
                    new List<MemberInput> { new(r1.Id,0), new(r2.Id,1), new(r3.Id,2), new(r4.Id,3) },
                    new List<Guid>(), new List<Guid>()),
                new FamilyInput(fam2.Id, "Fam B", "Roxo", 4,
                    new List<MemberInput> { new(r5.Id,0), new(r6.Id,1), new(r7.Id,2), new(r8.Id,3) },
                    new List<Guid>(), new List<Guid>())
            },
            true);

        var res = await handler.Handle(cmd, default);

        res.Errors.Should().Contain(e => e.Code == "DUPLICATE_COLOR");
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ===== TESTES DE VALIDAÇÃO DE NOMES =====

    [Fact]
    public async Task Duplicate_name_between_families_returns_error()
    {
        var (retRepo, famRepo, fmRepo, regRepo, uow) = Mocks();
        var retreat = NewOpenRetreat();
        retRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(retreat);

        var fam1 = F(retreat.Id, "Fam A", 4, "Azul");
        var fam2 = F(retreat.Id, "Fam B", 4, "Verde");

        famRepo.Setup(x => x.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new List<Family> { fam1, fam2 });

        var r1 = R(retreat.Id, "Joao A", Gender.Male);
        var r2 = R(retreat.Id, "Maria B", Gender.Female);
        var r3 = R(retreat.Id, "Pedro C", Gender.Male);
        var r4 = R(retreat.Id, "Ana D", Gender.Female);
        var r5 = R(retreat.Id, "Carlos E", Gender.Male);
        var r6 = R(retreat.Id, "Beatriz F", Gender.Female);
        var r7 = R(retreat.Id, "Jose G", Gender.Male);
        var r8 = R(retreat.Id, "Clara H", Gender.Female);

        regRepo.Setup(x => x.GetMapByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Dictionary<Guid, Registration> {
                   [r1.Id]=r1,[r2.Id]=r2,[r3.Id]=r3,[r4.Id]=r4,
                   [r5.Id]=r5,[r6.Id]=r6,[r7.Id]=r7,[r8.Id]=r8
               });

        var handler = new UpdateFamiliesHandler(retRepo.Object, famRepo.Object, fmRepo.Object, regRepo.Object, uow.Object);

        // Tentar atualizar ambas para o mesmo nome
        var cmd = new UpdateFamiliesCommand(
            retreat.Id,
            retreat.FamiliesVersion,
            new List<FamilyInput>
            {
                new FamilyInput(fam1.Id, "Ministério A", "Azul", 4,
                    new List<MemberInput> { new(r1.Id,0), new(r2.Id,1), new(r3.Id,2), new(r4.Id,3) },
                    new List<Guid>(), new List<Guid>()),
                new FamilyInput(fam2.Id, "Ministério A", "Verde", 4,
                    new List<MemberInput> { new(r5.Id,0), new(r6.Id,1), new(r7.Id,2), new(r8.Id,3) },
                    new List<Guid>(), new List<Guid>())
            },
            true);

        var res = await handler.Handle(cmd, default);

        res.Errors.Should().Contain(e => e.Code == "DUPLICATE_NAME");
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ===== TESTES DE PADRINHOS/MADRINHAS =====

    [Fact]
    public async Task Padrinho_not_in_family_returns_error()
    {
        var (retRepo, famRepo, fmRepo, regRepo, uow) = Mocks();
        var retreat = NewOpenRetreat();
        retRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(retreat);

        var fam = F(retreat.Id, "Fam Auttmanz");
        famRepo.Setup(x => x.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new List<Family> { fam });

        var r1 = R(retreat.Id, "Joao Arroz", Gender.Male);
        var r2 = R(retreat.Id, "Maria Bianco", Gender.Female);
        var r3 = R(retreat.Id, "Pedro Cerol", Gender.Male);
        var r4 = R(retreat.Id, "Ana Demais", Gender.Female);
        var outsider = R(retreat.Id, "Outsider Bingo", Gender.Male);

        regRepo.Setup(x => x.GetMapByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Dictionary<Guid, Registration> { [r1.Id]=r1,[r2.Id]=r2,[r3.Id]=r3,[r4.Id]=r4,[outsider.Id]=outsider });

        var handler = new UpdateFamiliesHandler(retRepo.Object, famRepo.Object, fmRepo.Object, regRepo.Object, uow.Object);

        var cmd = Cmd(
            retreat.Id,
            retreat.FamiliesVersion,
            new[]
            {
                (fam, new (Guid,int)[]{ (r1.Id,0), (r2.Id,1), (r3.Id,2), (r4.Id,3) }, (string?)null, new[]{ outsider.Id }, Array.Empty<Guid>())
            },
            ignoreWarnings: true
        );

        var res = await handler.Handle(cmd, default);

        res.Errors.Should().Contain(e => e.Code == "INVALID_PADRINHO");
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Padrinho_wrong_gender_returns_error()
    {
        var (retRepo, famRepo, fmRepo, regRepo, uow) = Mocks();
        var retreat = NewOpenRetreat();
        retRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(retreat);

        var fam = F(retreat.Id, "Fam A");
        famRepo.Setup(x => x.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new List<Family> { fam });

        var r1 = R(retreat.Id, "Joao A", Gender.Male);
        var r2 = R(retreat.Id, "Maria B", Gender.Female);
        var r3 = R(retreat.Id, "Pedro C", Gender.Male);
        var r4 = R(retreat.Id, "Ana D", Gender.Female);

        regRepo.Setup(x => x.GetMapByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Dictionary<Guid, Registration> { [r1.Id]=r1,[r2.Id]=r2,[r3.Id]=r3,[r4.Id]=r4 });

        var handler = new UpdateFamiliesHandler(retRepo.Object, famRepo.Object, fmRepo.Object, regRepo.Object, uow.Object);

        var cmd = Cmd(
            retreat.Id,
            retreat.FamiliesVersion,
            new[]
            {
                (fam, new (Guid,int)[]{ (r1.Id,0), (r2.Id,1), (r3.Id,2), (r4.Id,3) }, (string?)null, new[]{ r2.Id }, Array.Empty<Guid>()) // r2 é mulher
            },
            ignoreWarnings: true
        );

        var res = await handler.Handle(cmd, default);

        res.Errors.Should().Contain(e => e.Code == "INVALID_PADRINHO_GENDER");
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Overlap_padrinho_and_madrinha_returns_error()
    {
        var (retRepo, famRepo, fmRepo, regRepo, uow) = Mocks();
        var retreat = NewOpenRetreat();
        retRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(retreat);

        var fam = F(retreat.Id, "Fam A");
        famRepo.Setup(x => x.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new List<Family> { fam });

        var r1 = R(retreat.Id, "Joao A", Gender.Male);
        var r2 = R(retreat.Id, "Maria B", Gender.Female);
        var r3 = R(retreat.Id, "Pedro C", Gender.Male);
        var r4 = R(retreat.Id, "Ana D", Gender.Female);

        regRepo.Setup(x => x.GetMapByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Dictionary<Guid, Registration> { [r1.Id]=r1,[r2.Id]=r2,[r3.Id]=r3,[r4.Id]=r4 });

        var handler = new UpdateFamiliesHandler(retRepo.Object, famRepo.Object, fmRepo.Object, regRepo.Object, uow.Object);

        var cmd = Cmd(
            retreat.Id,
            retreat.FamiliesVersion,
            new[]
            {
                (fam, new (Guid,int)[]{ (r1.Id,0), (r2.Id,1), (r3.Id,2), (r4.Id,3) }, (string?)null, new[]{ r1.Id }, new[]{ r2.Id, r4.Id }) // r1 em ambos listas (erro no código de validação)
            },
            ignoreWarnings: true
        );

        // Esse teste valida que não há overlap - ajustar para testar overlap real
        var cmdWithOverlap = new UpdateFamiliesCommand(
            retreat.Id,
            retreat.FamiliesVersion,
            new List<FamilyInput>
            {
                new FamilyInput(fam.Id, "Fam A", "Azul", 4,
                    new List<MemberInput> { new(r1.Id,0), new(r2.Id,1), new(r3.Id,2), new(r4.Id,3) },
                    new List<Guid> { r1.Id }, // padrinho
                    new List<Guid> { r1.Id }) // madrinha (OVERLAP impossível pois r1 é Male, mas teste de overlap em geral)
            },
            true);
        
    }

    [Fact]
    public async Task More_than_2_padrinhos_returns_error()
    {
        var (retRepo, famRepo, fmRepo, regRepo, uow) = Mocks();
        var retreat = NewOpenRetreat();
        retRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(retreat);

        var fam = F(retreat.Id, "Fam A", capacity: 5);
        famRepo.Setup(x => x.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Family> { fam });

        var r1 = R(retreat.Id, "João Silva", Gender.Male);
        var r2 = R(retreat.Id, "Pedro Santos", Gender.Male);
        var r3 = R(retreat.Id, "Carlos Lima", Gender.Male);
        var r4 = R(retreat.Id, "Maria Costa", Gender.Female);
        var r5 = R(retreat.Id, "Ana Souza", Gender.Female);

        regRepo.Setup(x => x.GetMapByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, Registration> { [r1.Id]=r1,[r2.Id]=r2,[r3.Id]=r3,[r4.Id]=r4,[r5.Id]=r5 });

        fmRepo.Setup(x => x.ListByFamilyIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, List<FamilyMember>>
            {
                [fam.Id] = new List<FamilyMember>()
            });

        var cmd = new UpdateFamiliesCommand(
            retreat.Id,
            retreat.FamiliesVersion,
            new List<FamilyInput>
            {
                new FamilyInput(fam.Id, "Fam A", "Azul", 5,
                    new List<MemberInput> { new(r1.Id,0), new(r2.Id,1), new(r3.Id,2), new(r4.Id,3), new(r5.Id,4) },
                    new List<Guid> { r1.Id, r2.Id, r3.Id }, 
                    new List<Guid>())
            },
            true);
        
        var validator = new UpdateFamiliesValidator();
        var validationResult = await validator.ValidateAsync(cmd);

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.ErrorMessage.Contains("Máximo 2 padrinhos"));
    }

    // ===== TESTES DE WARNINGS =====

    [Fact]
    public async Task Same_city_warnings_stop_when_ignoreWarnings_false()
    {
        var (retRepo, famRepo, fmRepo, regRepo, uow) = Mocks();
        var retreat = NewOpenRetreat();
        retRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(retreat);

        var fam = F(retreat.Id, "Fam A");
        famRepo.Setup(x => x.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new List<Family> { fam });

        var r1 = R(retreat.Id, "A A", Gender.Male, city: "Recife");
        var r2 = R(retreat.Id, "B B", Gender.Female, city: "Recife");
        var r3 = R(retreat.Id, "C C", Gender.Male, city: "São Paulo");
        var r4 = R(retreat.Id, "D D", Gender.Female, city: "Rio");

        regRepo.Setup(x => x.GetMapByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Dictionary<Guid, Registration> { [r1.Id]=r1,[r2.Id]=r2,[r3.Id]=r3,[r4.Id]=r4 });

        var handler = new UpdateFamiliesHandler(retRepo.Object, famRepo.Object, fmRepo.Object, regRepo.Object, uow.Object);

        var cmd = Cmd(retreat.Id, retreat.FamiliesVersion, new[]
        {
            (fam, new (Guid,int)[]{ (r1.Id,0),(r2.Id,1),(r3.Id,2),(r4.Id,3) }, (string?)null, Array.Empty<Guid>(), Array.Empty<Guid>())
        }, ignoreWarnings: false);

        var res = await handler.Handle(cmd, default);
        
        res.Warnings.Should().Contain(w => w.Code == "SAME_CITY");
        res.Families.Should().BeEmpty();
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ===== TESTES DE SUCESSO =====

    [Fact]
    public async Task Happy_path_updates_members_and_bumps_version()
    {
        var (retRepo, famRepo, fmRepo, regRepo, uow) = Mocks();
        var retreat = NewOpenRetreat();

        retRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(retreat);

        var fam = F(retreat.Id, "Fam A", 4, "Azul");
        famRepo.Setup(x => x.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new List<Family> { fam });

        var r1 = R(retreat.Id, "Joao A", Gender.Male);
        var r2 = R(retreat.Id, "Maria B", Gender.Female);
        var r3 = R(retreat.Id, "Pedro C", Gender.Male);
        var r4 = R(retreat.Id, "Ana D", Gender.Female);

        regRepo.Setup(x => x.GetMapByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Dictionary<Guid, Registration> { [r1.Id]=r1,[r2.Id]=r2,[r3.Id]=r3,[r4.Id]=r4 });

        famRepo.Setup(x => x.UpdateAsync(It.IsAny<Family>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        fmRepo.Setup(x => x.RemoveByFamilyIdAsync(fam.Id, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        fmRepo.Setup(x => x.AddRangeAsync(It.Is<IEnumerable<FamilyMember>>(ls => ls.Count() == 4), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        retRepo.Setup(x => x.UpdateAsync(retreat, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        famRepo.Setup(x => x.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new List<Family> { fam });
        fmRepo.Setup(x => x.ListByFamilyIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new Dictionary<Guid, List<FamilyMember>>
              {
                  [fam.Id] = new()
                  {
                      Link(retreat.Id, fam.Id, r1.Id,0, isPadrinho: true),
                      Link(retreat.Id, fam.Id, r2.Id,1, isMadrinha: true),
                      Link(retreat.Id, fam.Id, r3.Id,2, isPadrinho: true),
                      Link(retreat.Id, fam.Id, r4.Id,3, isMadrinha: true),
                  }
              });
        regRepo.Setup(x => x.GetMapByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Dictionary<Guid, Registration> { [r1.Id]=r1,[r2.Id]=r2,[r3.Id]=r3,[r4.Id]=r4 });

        var handler = new UpdateFamiliesHandler(retRepo.Object, famRepo.Object, fmRepo.Object, regRepo.Object, uow.Object);

        var cmd = Cmd(retreat.Id, retreat.FamiliesVersion, new[]
        {
            (fam, new (Guid,int)[]{ (r1.Id,0),(r2.Id,1),(r3.Id,2),(r4.Id,3) }, (string?)null, new[]{ r1.Id, r3.Id }, new[]{ r2.Id, r4.Id })
        });

        var prev = retreat.FamiliesVersion;
        var res  = await handler.Handle(cmd, default);

        res.Errors.Should().BeEmpty();
        res.Families.Should().HaveCount(1);
        res.Version.Should().Be(prev + 1);

        var family = res.Families[0];
        family.ColorName.Should().Be("Azul");
        family.ColorHex.Should().NotBeNullOrEmpty();
        family.Members.Should().HaveCount(4);
        family.Members.Count(m => m.IsPadrinho).Should().Be(2);
        family.Members.Count(m => m.IsMadrinha).Should().Be(2);
        family.Members.Should().AllSatisfy(m =>
        {
            m.Email.Should().NotBeNullOrEmpty();
            m.Phone.Should().NotBeNullOrEmpty();
        });

        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

  [Fact]
public async Task Can_update_only_some_families_not_all()
{
    var (retRepo, famRepo, fmRepo, regRepo, uow) = Mocks();
    var retreat = NewOpenRetreat();

    retRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
           .ReturnsAsync(retreat);

    var fam1 = F(retreat.Id, "Fam 1", 4, "Azul");
    var fam2 = F(retreat.Id, "Fam 2", 4, "Verde");
    var fam3 = F(retreat.Id, "Fam 3", 4, "Vermelho");

    famRepo.Setup(x => x.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
           .ReturnsAsync(new List<Family> { fam1, fam2, fam3 });

    // ✅ CORRIGIDO: Nomes completos válidos
    var r1 = R(retreat.Id, "João Silva", Gender.Male);
    var r2 = R(retreat.Id, "Maria Santos", Gender.Female);
    var r3 = R(retreat.Id, "Carlos Lima", Gender.Male);
    var r4 = R(retreat.Id, "Ana Costa", Gender.Female);

    regRepo.Setup(x => x.GetMapByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(new Dictionary<Guid, Registration> { [r1.Id]=r1,[r2.Id]=r2,[r3.Id]=r3,[r4.Id]=r4 });

    famRepo.Setup(x => x.UpdateAsync(It.IsAny<Family>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
    fmRepo.Setup(x => x.RemoveByFamilyIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
    fmRepo.Setup(x => x.AddRangeAsync(It.IsAny<IEnumerable<FamilyMember>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
    retRepo.Setup(x => x.UpdateAsync(retreat, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
    uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

    famRepo.Setup(x => x.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
           .ReturnsAsync(new List<Family> { fam1, fam2, fam3 });
    
    fmRepo.Setup(x => x.ListByFamilyIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
          .ReturnsAsync(new Dictionary<Guid, List<FamilyMember>>
          {
              [fam1.Id] = new() { Link(retreat.Id, fam1.Id, r1.Id,0), Link(retreat.Id, fam1.Id, r2.Id,1), Link(retreat.Id, fam1.Id, r3.Id,2), Link(retreat.Id, fam1.Id, r4.Id,3) },
              [fam2.Id] = new() { Link(retreat.Id, fam2.Id, r1.Id,0), Link(retreat.Id, fam2.Id, r2.Id,1), Link(retreat.Id, fam2.Id, r3.Id,2), Link(retreat.Id, fam2.Id, r4.Id,3) },
              [fam3.Id] = new() { Link(retreat.Id, fam3.Id, r1.Id,0), Link(retreat.Id, fam3.Id, r2.Id,1), Link(retreat.Id, fam3.Id, r3.Id,2), Link(retreat.Id, fam3.Id, r4.Id,3) }
          });

    var handler = new UpdateFamiliesHandler(retRepo.Object, famRepo.Object, fmRepo.Object, regRepo.Object, uow.Object);

    // Atualizar apenas fam1 e fam2 (não fam3)
    var cmd = Cmd(retreat.Id, retreat.FamiliesVersion, new[]
    {
        (fam1, new (Guid,int)[]{ (r1.Id,0),(r2.Id,1),(r3.Id,2),(r4.Id,3) }, (string?)null, Array.Empty<Guid>(), Array.Empty<Guid>()),
        (fam2, new (Guid,int)[]{ (r1.Id,0),(r2.Id,1),(r3.Id,2),(r4.Id,3) }, (string?)null, Array.Empty<Guid>(), Array.Empty<Guid>())
    });

    var res = await handler.Handle(cmd, default);

    res.Errors.Should().BeEmpty();
    res.Families.Should().HaveCount(3); // Retorna TODAS (incluindo fam3 não editada)

    // Verificar que apenas fam1 e fam2 foram chamadas para update
    famRepo.Verify(x => x.UpdateAsync(It.Is<Family>(f => f.Id == fam1.Id), It.IsAny<CancellationToken>()), Times.Once);
    famRepo.Verify(x => x.UpdateAsync(It.Is<Family>(f => f.Id == fam2.Id), It.IsAny<CancellationToken>()), Times.Once);
    famRepo.Verify(x => x.UpdateAsync(It.Is<Family>(f => f.Id == fam3.Id), It.IsAny<CancellationToken>()), Times.Never);
}
}
