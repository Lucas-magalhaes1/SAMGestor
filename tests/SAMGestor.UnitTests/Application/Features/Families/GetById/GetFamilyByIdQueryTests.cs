using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using SAMGestor.Application.Features.Families.GetById;
using SAMGestor.Application.Common.Families;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;
using Xunit;

namespace SAMGestor.UnitTests.Application.Features.Families.GetById;

public class GetFamilyByIdQueryTests
{
    private static Retreat NewRetreat(bool locked = false, int familiesVersion = 1)
    {
        var r = new Retreat(
            new FullName("Retiro Teste"),
            "ED1",
            "Tema",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(12)),
            10, 10,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
            new Money(0, "BRL"),
            new Money(0, "BRL"),
            new Percentage(50),
            new Percentage(50));

        for (int i = 1; i < familiesVersion; i++) r.BumpFamiliesVersion();
        if (locked) r.LockFamilies();
        return r;
    }

    private static Family NewFamily(Guid retreatId, string name = "Família 1", int capacity = 4, bool isLocked = false)
    {
        var f = new Family(new FamilyName(name), retreatId, capacity);
        if (isLocked) f.Lock();
        return f;
    }

    private static Registration NewReg(Guid retreatId, string name, Gender g, string city)
        => new Registration(
            new FullName(name),
            new CPF(RandomCpf()),
            new EmailAddress($"{Guid.NewGuid():N}@mail.com"),
            "11999999999",
            new DateOnly(1990, 1, 1),
            g,
            city,
            RegistrationStatus.Confirmed,
            ParticipationCategory.Guest,
            "Reg",
            retreatId);

    private static FamilyMember Link(Guid retreatId, Guid familyId, Guid regId, int pos)
        => new FamilyMember(retreatId, familyId, regId, pos);

    private static string RandomCpf()
    {
        var rnd = new Random();
        return string.Concat(Enumerable.Range(0, 11).Select(_ => rnd.Next(0, 9)));
    }
    

    [Fact]
    public async Task Return_null_when_retreat_not_found()
    {
        var retreatRepo = new Mock<IRetreatRepository>();
        retreatRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync((Retreat?)null);

        var familyRepo = new Mock<IFamilyRepository>();
        var fmRepo     = new Mock<IFamilyMemberRepository>();
        var regRepo    = new Mock<IRegistrationRepository>();

        var handler = new GetFamilyByIdHandler(retreatRepo.Object, familyRepo.Object, fmRepo.Object, regRepo.Object);
        
        var res = await handler.Handle(new GetFamilyByIdQuery(Guid.NewGuid(), Guid.NewGuid(), IncludeAlerts: true), CancellationToken.None);
        
        res.Version.Should().Be(0);
        res.Family.Should().BeNull();
    }

    [Fact]
    public async Task Return_null_when_family_not_found_or_belongs_to_other_retreat()
    {
        
        var retreat = NewRetreat(familiesVersion: 4);
        var otherRetreat = NewRetreat();

        var familyRepo = new Mock<IFamilyRepository>();
        familyRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync((Family?)null);

        var retreatRepo = new Mock<IRetreatRepository>();
        retreatRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(retreat);

        var handler = new GetFamilyByIdHandler(
            retreatRepo.Object,
            familyRepo.Object,
            new Mock<IFamilyMemberRepository>().Object,
            new Mock<IRegistrationRepository>().Object);
        
        var res1 = await handler.Handle(new GetFamilyByIdQuery(retreat.Id, Guid.NewGuid(), IncludeAlerts: true), CancellationToken.None);
        
        res1.Version.Should().Be(retreat.FamiliesVersion);
        res1.Family.Should().BeNull();
        
        var famOther = NewFamily(otherRetreat.Id);
        familyRepo.Setup(r => r.GetByIdAsync(famOther.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(famOther);

        var res2 = await handler.Handle(new GetFamilyByIdQuery(retreat.Id, famOther.Id, IncludeAlerts: true), CancellationToken.None);
        
        res2.Version.Should().Be(retreat.FamiliesVersion);
        res2.Family.Should().BeNull();
    }

    [Fact]
    public async Task Return_family_with_members_and_alerts_when_includeAlerts_true()
    {
        var retreat = NewRetreat(familiesVersion: 2);
        var fam     = NewFamily(retreat.Id, "Família XPTO", capacity: 4, isLocked: true);

        var r1 = NewReg(retreat.Id, "João Silva",  Gender.Male,   "São Paulo");
        var r2 = NewReg(retreat.Id, "Maria Souza", Gender.Female, "São Paulo"); 
        var r3 = NewReg(retreat.Id, "Pedro Lima",  Gender.Male,   "Recife");
        var r4 = NewReg(retreat.Id, "Ana Lima",    Gender.Female, "Recife");

        var l1 = Link(retreat.Id, fam.Id, r1.Id, 0);
        var l2 = Link(retreat.Id, fam.Id, r2.Id, 1);
        var l3 = Link(retreat.Id, fam.Id, r3.Id, 2);
        var l4 = Link(retreat.Id, fam.Id, r4.Id, 3);
        var links = new List<FamilyMember> { l1, l2, l3, l4 };

        var retreatRepo = new Mock<IRetreatRepository>();
        retreatRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(retreat);

        var familyRepo = new Mock<IFamilyRepository>();
        familyRepo.Setup(r => r.GetByIdAsync(fam.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(fam);

        var fmRepo = new Mock<IFamilyMemberRepository>();
        fmRepo.Setup(r => r.ListByFamilyAsync(fam.Id, It.IsAny<CancellationToken>()))
              .ReturnsAsync(links);

        var regRepo = new Mock<IRegistrationRepository>();
        regRepo.Setup(r => r.GetMapByIdsAsync(
                It.Is<IEnumerable<Guid>>(ids => ids.OrderBy(x=>x).SequenceEqual(new[]{r1.Id,r2.Id,r3.Id,r4.Id}.OrderBy(x=>x))),
                It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Dictionary<Guid, Registration>
               {
                   [r1.Id] = r1, [r2.Id] = r2, [r3.Id] = r3, [r4.Id] = r4
               });

        var handler = new GetFamilyByIdHandler(retreatRepo.Object, familyRepo.Object, fmRepo.Object, regRepo.Object);
        
        var res = await handler.Handle(new GetFamilyByIdQuery(retreat.Id, fam.Id, IncludeAlerts: true), CancellationToken.None);
        
        res.Version.Should().Be(retreat.FamiliesVersion);
        res.Family.Should().NotBeNull();
        res.Family!.FamilyId.Should().Be(fam.Id);
        res.Family.IsLocked.Should().BeTrue();
        res.Family.Name.Should().Be("Família XPTO");
        res.Family.Capacity.Should().Be(4);
        res.Family.TotalMembers.Should().Be(4);
        res.Family.MaleCount.Should().Be(2);
        res.Family.FemaleCount.Should().Be(2);
        res.Family.Remaining.Should().Be(0);
        
        res.Family.Members.Select(m => m.Position).Should().BeInAscendingOrder().And.OnlyHaveUniqueItems();

        // alerts presentes (SAME_CITY esperado por SP & SP; e por Recife & Recife)
        res.Family.Alerts.Should().NotBeNull();
        res.Family.Alerts.Any(a => a.Code == "SAME_CITY").Should().BeTrue();
    }

    [Fact]
    public async Task Return_family_without_alerts_when_includeAlerts_false()
    {
        
        var retreat = NewRetreat(familiesVersion: 6);
        var fam     = NewFamily(retreat.Id, "Família 7", capacity: 4, isLocked: false);

        var r1 = NewReg(retreat.Id, "João Silva", Gender.Male, "SP");
        var l1 = Link(retreat.Id, fam.Id, r1.Id, 0);

        var retreatRepo = new Mock<IRetreatRepository>();
        retreatRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(retreat);

        var familyRepo = new Mock<IFamilyRepository>();
        familyRepo.Setup(r => r.GetByIdAsync(fam.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(fam);

        var fmRepo = new Mock<IFamilyMemberRepository>();
        fmRepo.Setup(r => r.ListByFamilyAsync(fam.Id, It.IsAny<CancellationToken>()))
              .ReturnsAsync(new List<FamilyMember> { l1 });

        var regRepo = new Mock<IRegistrationRepository>();
        regRepo.Setup(r => r.GetMapByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Dictionary<Guid, Registration> { [r1.Id] = r1 });

        var handler = new GetFamilyByIdHandler(retreatRepo.Object, familyRepo.Object, fmRepo.Object, regRepo.Object);
        
        var res = await handler.Handle(new GetFamilyByIdQuery(retreat.Id, fam.Id, IncludeAlerts: false), CancellationToken.None);
        
        res.Version.Should().Be(retreat.FamiliesVersion);
        res.Family.Should().NotBeNull();
        res.Family!.IsLocked.Should().BeFalse();
        res.Family!.Alerts.Should().NotBeNull().And.BeEmpty(); 
    }
}
