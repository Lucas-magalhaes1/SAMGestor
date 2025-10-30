using System.Reflection;
using FluentAssertions;
using Moq;
using SAMGestor.Application.Features.Tents.TentRoster.Assign;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.UnitTests.Application.Features.Tents.TentRoster.Assing
{
    public class UpdateTentRosterHandlerTests
    {
        private static T Set<T>(T obj, string prop, object? value)
        {
            obj!.GetType().GetProperty(prop, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.SetValue(obj, value);
            return obj;
        }

        private static Retreat MakeRetreat(Guid? id = null, int version = 1, bool locked = false)
        {
            var r = (Retreat)Activator.CreateInstance(typeof(Retreat), nonPublic: true)!;
            Set(r, "Id", id ?? Guid.NewGuid());
            Set(r, "TentsVersion", version);
            Set(r, "TentsLocked", locked);
            return r;
        }

        private static Tent MakeTent(Guid retreatId, TentCategory cat, int number, int capacity, bool locked = false, bool active = true)
        {
            var t = (Tent)Activator.CreateInstance(typeof(Tent), nonPublic: true)!;
            Set(t, "Id", Guid.NewGuid());
            Set(t, "RetreatId", retreatId);
            Set(t, "Number", new TentNumber(number));
            Set(t, "Category", cat);
            Set(t, "Capacity", capacity);
            Set(t, "IsLocked", locked);
            Set(t, "IsActive", active);
            return t;
        }

        private static object MakeNameValue(object onEntity, string value)
        {
            var s = value?.Trim();
            if (string.IsNullOrWhiteSpace(s)) s = "Test Silva";
            if (!s!.Contains(' ')) s = s + " Silva";
            if (s.Length < 3) s = "Test Silva";
            var p = onEntity.GetType().GetProperty("Name", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;
            var t = p.PropertyType;
            var ctor = t.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                        .FirstOrDefault(c => c.GetParameters() is { Length: 1 } ps && ps[0].ParameterType == typeof(string));
            return ctor is null ? s : ctor.Invoke(new object[] { s });
        }

        private static Registration MakeReg(Guid retreatId, string name, Gender g, bool enabled, RegistrationStatus st, string? city = "SP")
        {
            var r = (Registration)Activator.CreateInstance(typeof(Registration), nonPublic: true)!;
            Set(r, "Id", Guid.NewGuid());
            Set(r, "RetreatId", retreatId);
            Set(r, "Name", MakeNameValue(r, name));
            Set(r, "Gender", g);
            Set(r, "Enabled", enabled);
            Set(r, "Status", st);
            Set(r, "City", city);
            return r;
        }

        [Fact]
        public async Task Happy_path_should_apply_snapshot_bump_version_and_respect_positions()
        {
            var retId = Guid.NewGuid();
            var retreat = MakeRetreat(retId, 2, false);
            var t1 = MakeTent(retId, TentCategory.Male, 1, 3, false);
            var t2 = MakeTent(retId, TentCategory.Female, 1, 2, false);
            var r1 = MakeReg(retId, "Alex Silva", Gender.Male, true, RegistrationStatus.Confirmed);
            var r2 = MakeReg(retId, "Bruno Souza", Gender.Male, true, RegistrationStatus.PaymentConfirmed);
            var r3 = MakeReg(retId, "Carla Dias", Gender.Female, true, RegistrationStatus.Confirmed);

            var retreatRepo = new Mock<IRetreatRepository>();
            retreatRepo.Setup(x => x.GetByIdAsync(retId, It.IsAny<CancellationToken>())).ReturnsAsync(retreat);
            retreatRepo.Setup(x => x.UpdateAsync(retreat, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var tentRepo = new Mock<ITentRepository>();
            tentRepo.Setup(x => x.ListByRetreatAsync(retId, It.IsAny<TentCategory?>(), It.IsAny<bool?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<Tent> { t1, t2 });

            var assignRepo = new Mock<ITentAssignmentRepository>();
            assignRepo.SetupSequence(x => x.ListByTentIdsAsync(It.IsAny<Guid[]>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(new List<TentAssignment>())
                      .ReturnsAsync(new List<TentAssignment>
                      {
                          new TentAssignment(t1.Id, r1.Id, 0),
                          new TentAssignment(t1.Id, r2.Id, 1),
                          new TentAssignment(t2.Id, r3.Id, 0)
                      });
            assignRepo.Setup(x => x.RemoveRangeAsync(It.IsAny<List<TentAssignment>>(), It.IsAny<CancellationToken>()))
                      .Returns(Task.CompletedTask);
            assignRepo.Setup(x => x.AddRangeAsync(It.IsAny<List<TentAssignment>>(), It.IsAny<CancellationToken>()))
                      .Returns(Task.CompletedTask);

            var regRepo = new Mock<IRegistrationRepository>();
            regRepo.Setup(x => x.GetMapByIdsAsync(It.IsAny<Guid[]>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync((Guid[] ids, CancellationToken _) =>
                  {
                      var map = new Dictionary<Guid, Registration>();
                      foreach (var r in new[] { r1, r2, r3 }) map[r.Id] = r;
                      return map;
                  });

            var uow = new Mock<IUnitOfWork>();
            uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var handler = new UpdateTentRosterHandler(retreatRepo.Object, tentRepo.Object, assignRepo.Object, regRepo.Object, uow.Object);

            var cmd = new UpdateTentRosterCommand(retId, retreat.TentsVersion, new[]
            {
                new TentRosterSnapshot(t1.Id, new []
                {
                    new TentRosterMemberItem(r1.Id, 0),
                    new TentRosterMemberItem(r2.Id, 1)
                }),
                new TentRosterSnapshot(t2.Id, new []
                {
                    new TentRosterMemberItem(r3.Id, 0)
                })
            });

            var res = await handler.Handle(cmd, CancellationToken.None);
            res.Errors.Should().BeEmpty();
            res.Version.Should().BeGreaterThan(2);
            var t1View = res.Tents.Single(t => t.TentId == t1.Id);
            t1View.Members.Select(m => (m.RegistrationId, m.Position)).Should().BeEquivalentTo(new[]
            {
                (r1.Id, (int?)0), (r2.Id, (int?)1)
            }, o => o.WithStrictOrdering());
            var t2View = res.Tents.Single(t => t.TentId == t2.Id);
            t2View.Members.Select(m => (m.RegistrationId, m.Position)).Should().BeEquivalentTo(new[]
            {
                (r3.Id, (int?)0)
            }, o => o.WithStrictOrdering());
        }

        [Fact]
        public async Task Version_mismatch_should_return_error_and_not_apply()
        {
            var retId = Guid.NewGuid();
            var retreat = MakeRetreat(retId, 5, false);
            var t1 = MakeTent(retId, TentCategory.Male, 1, 2);

            var retreatRepo = new Mock<IRetreatRepository>();
            retreatRepo.Setup(x => x.GetByIdAsync(retId, It.IsAny<CancellationToken>())).ReturnsAsync(retreat);

            var tentRepo = new Mock<ITentRepository>();
            tentRepo.Setup(x => x.ListByRetreatAsync(retId, It.IsAny<TentCategory?>(), It.IsAny<bool?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<Tent> { t1 });

            var assignRepo = new Mock<ITentAssignmentRepository>();
            var regRepo = new Mock<IRegistrationRepository>();
            var uow = new Mock<IUnitOfWork>();

            var handler = new UpdateTentRosterHandler(retreatRepo.Object, tentRepo.Object, assignRepo.Object, regRepo.Object, uow.Object);
            var res = await handler.Handle(new UpdateTentRosterCommand(retId, 1, new[] { new TentRosterSnapshot(t1.Id, Array.Empty<TentRosterMemberItem>()) }), CancellationToken.None);

            res.Errors.Should().Contain(e => e.Code == "VERSION_MISMATCH");
            assignRepo.Verify(x => x.RemoveRangeAsync(It.IsAny<List<TentAssignment>>(), It.IsAny<CancellationToken>()), Times.Never);
            assignRepo.Verify(x => x.AddRangeAsync(It.IsAny<List<TentAssignment>>(), It.IsAny<CancellationToken>()), Times.Never);
            retreatRepo.Verify(x => x.UpdateAsync(It.IsAny<Retreat>(), It.IsAny<CancellationToken>()), Times.Never);
            uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Retreat_locked_should_throw()
        {
            var retId = Guid.NewGuid();
            var retreat = MakeRetreat(retId, 1, true);

            var retreatRepo = new Mock<IRetreatRepository>();
            retreatRepo.Setup(x => x.GetByIdAsync(retId, It.IsAny<CancellationToken>())).ReturnsAsync(retreat);

            var tentRepo = new Mock<ITentRepository>();
            var assignRepo = new Mock<ITentAssignmentRepository>();
            var regRepo = new Mock<IRegistrationRepository>();
            var uow = new Mock<IUnitOfWork>();

            var handler = new UpdateTentRosterHandler(retreatRepo.Object, tentRepo.Object, assignRepo.Object, regRepo.Object, uow.Object);
            await Assert.ThrowsAsync<BusinessRuleException>(() =>
                handler.Handle(new UpdateTentRosterCommand(retId, 1, Array.Empty<TentRosterSnapshot>()), CancellationToken.None));
        }

        [Fact]
        public async Task Unknown_tent_should_return_error()
        {
            var retId = Guid.NewGuid();
            var retreat = MakeRetreat(retId, 1, false);
            var t1 = MakeTent(retId, TentCategory.Male, 1, 2);
            var unknownTentId = Guid.NewGuid();

            var retreatRepo = new Mock<IRetreatRepository>();
            retreatRepo.Setup(x => x.GetByIdAsync(retId, It.IsAny<CancellationToken>())).ReturnsAsync(retreat);

            var tentRepo = new Mock<ITentRepository>();
            tentRepo.Setup(x => x.ListByRetreatAsync(retId, It.IsAny<TentCategory?>(), It.IsAny<bool?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<Tent> { t1 });

            var assignRepo = new Mock<ITentAssignmentRepository>();
            var regRepo = new Mock<IRegistrationRepository>();
            var uow = new Mock<IUnitOfWork>();

            var handler = new UpdateTentRosterHandler(retreatRepo.Object, tentRepo.Object, assignRepo.Object, regRepo.Object, uow.Object);
            var res = await handler.Handle(new UpdateTentRosterCommand(retId, 1, new[] { new TentRosterSnapshot(unknownTentId, Array.Empty<TentRosterMemberItem>()) }), CancellationToken.None);

            res.Errors.Should().Contain(e => e.Code == "UNKNOWN_TENT");
        }

        [Fact]
        public async Task Unknown_registration_should_return_error()
        {
            var retId = Guid.NewGuid();
            var retreat = MakeRetreat(retId, 1, false);
            var t1 = MakeTent(retId, TentCategory.Male, 1, 2);
            var fakeReg = Guid.NewGuid();

            var retreatRepo = new Mock<IRetreatRepository>();
            retreatRepo.Setup(x => x.GetByIdAsync(retId, It.IsAny<CancellationToken>())).ReturnsAsync(retreat);

            var tentRepo = new Mock<ITentRepository>();
            tentRepo.Setup(x => x.ListByRetreatAsync(retId, It.IsAny<TentCategory?>(), It.IsAny<bool?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<Tent> { t1 });

            var regRepo = new Mock<IRegistrationRepository>();
            regRepo.Setup(x => x.GetMapByIdsAsync(It.IsAny<Guid[]>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new Dictionary<Guid, Registration>());

            var assignRepo = new Mock<ITentAssignmentRepository>();
            var uow = new Mock<IUnitOfWork>();

            var handler = new UpdateTentRosterHandler(retreatRepo.Object, tentRepo.Object, assignRepo.Object, regRepo.Object, uow.Object);
            var res = await handler.Handle(new UpdateTentRosterCommand(retId, 1, new[] { new TentRosterSnapshot(t1.Id, new[] { new TentRosterMemberItem(fakeReg, 0) }) }), CancellationToken.None);

            res.Errors.Should().Contain(e => e.Code == "UNKNOWN_REGISTRATION");
        }

        [Fact]
        public async Task Wrong_retreat_registration_should_return_error()
        {
            var retId = Guid.NewGuid();
            var retreat = MakeRetreat(retId, 1, false);
            var t1 = MakeTent(retId, TentCategory.Male, 1, 2);
            var rForeign = MakeReg(Guid.NewGuid(), "Mario Silva", Gender.Male, true, RegistrationStatus.Confirmed);

            var retreatRepo = new Mock<IRetreatRepository>();
            retreatRepo.Setup(x => x.GetByIdAsync(retId, It.IsAny<CancellationToken>())).ReturnsAsync(retreat);

            var tentRepo = new Mock<ITentRepository>();
            tentRepo.Setup(x => x.ListByRetreatAsync(retId, It.IsAny<TentCategory?>(), It.IsAny<bool?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<Tent> { t1 });

            var regRepo = new Mock<IRegistrationRepository>();
            regRepo.Setup(x => x.GetMapByIdsAsync(It.IsAny<Guid[]>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new Dictionary<Guid, Registration> { [rForeign.Id] = rForeign });

            var assignRepo = new Mock<ITentAssignmentRepository>();
            var uow = new Mock<IUnitOfWork>();

            var handler = new UpdateTentRosterHandler(retreatRepo.Object, tentRepo.Object, assignRepo.Object, regRepo.Object, uow.Object);
            var res = await handler.Handle(new UpdateTentRosterCommand(retId, 1, new[] { new TentRosterSnapshot(t1.Id, new[] { new TentRosterMemberItem(rForeign.Id, 0) }) }), CancellationToken.None);

            res.Errors.Should().Contain(e => e.Code == "WRONG_RETREAT");
        }

        [Fact]
        public async Task Tent_locked_should_return_error()
        {
            var retId = Guid.NewGuid();
            var retreat = MakeRetreat(retId, 1, false);
            var t1 = MakeTent(retId, TentCategory.Male, 1, 2, locked: true);
            var r1 = MakeReg(retId, "Alan Silva", Gender.Male, true, RegistrationStatus.Confirmed);

            var retreatRepo = new Mock<IRetreatRepository>();
            retreatRepo.Setup(x => x.GetByIdAsync(retId, It.IsAny<CancellationToken>())).ReturnsAsync(retreat);

            var tentRepo = new Mock<ITentRepository>();
            tentRepo.Setup(x => x.ListByRetreatAsync(retId, It.IsAny<TentCategory?>(), It.IsAny<bool?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<Tent> { t1 });

            var regRepo = new Mock<IRegistrationRepository>();
            regRepo.Setup(x => x.GetMapByIdsAsync(It.IsAny<Guid[]>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new Dictionary<Guid, Registration> { [r1.Id] = r1 });

            var assignRepo = new Mock<ITentAssignmentRepository>();
            var uow = new Mock<IUnitOfWork>();

            var handler = new UpdateTentRosterHandler(retreatRepo.Object, tentRepo.Object, assignRepo.Object, regRepo.Object, uow.Object);
            var res = await handler.Handle(new UpdateTentRosterCommand(retId, 1, new[] { new TentRosterSnapshot(t1.Id, new[] { new TentRosterMemberItem(r1.Id, 0) }) }), CancellationToken.None);

            res.Errors.Should().Contain(e => e.Code == "TENT_LOCKED");
        }

        [Fact]
        public async Task Over_capacity_should_return_error()
        {
            var retId = Guid.NewGuid();
            var retreat = MakeRetreat(retId, 1, false);
            var t1 = MakeTent(retId, TentCategory.Male, 1, 1, false);
            var r1 = MakeReg(retId, "Alex Silva", Gender.Male, true, RegistrationStatus.Confirmed);
            var r2 = MakeReg(retId, "Bruno Souza", Gender.Male, true, RegistrationStatus.PaymentConfirmed);

            var retreatRepo = new Mock<IRetreatRepository>();
            retreatRepo.Setup(x => x.GetByIdAsync(retId, It.IsAny<CancellationToken>())).ReturnsAsync(retreat);

            var tentRepo = new Mock<ITentRepository>();
            tentRepo.Setup(x => x.ListByRetreatAsync(retId, It.IsAny<TentCategory?>(), It.IsAny<bool?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<Tent> { t1 });

            var regRepo = new Mock<IRegistrationRepository>();
            regRepo.Setup(x => x.GetMapByIdsAsync(It.IsAny<Guid[]>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new Dictionary<Guid, Registration> { [r1.Id] = r1, [r2.Id] = r2 });

            var assignRepo = new Mock<ITentAssignmentRepository>();
            var uow = new Mock<IUnitOfWork>();

            var handler = new UpdateTentRosterHandler(retreatRepo.Object, tentRepo.Object, assignRepo.Object, regRepo.Object, uow.Object);
            var res = await handler.Handle(new UpdateTentRosterCommand(retId, 1, new[]
            {
                new TentRosterSnapshot(t1.Id, new []
                {
                    new TentRosterMemberItem(r1.Id, 0),
                    new TentRosterMemberItem(r2.Id, 1)
                })
            }), CancellationToken.None);

            res.Errors.Should().Contain(e => e.Code == "OVER_CAPACITY");
        }

        [Fact]
        public async Task Duplicated_member_in_multiple_tents_should_return_error()
        {
            var retId = Guid.NewGuid();
            var retreat = MakeRetreat(retId, 1, false);
            var tA = MakeTent(retId, TentCategory.Male, 1, 2);
            var tB = MakeTent(retId, TentCategory.Male, 2, 2);
            var r = MakeReg(retId, "Xavier Souza", Gender.Male, true, RegistrationStatus.Confirmed);

            var retreatRepo = new Mock<IRetreatRepository>();
            retreatRepo.Setup(x => x.GetByIdAsync(retId, It.IsAny<CancellationToken>())).ReturnsAsync(retreat);

            var tentRepo = new Mock<ITentRepository>();
            tentRepo.Setup(x => x.ListByRetreatAsync(retId, It.IsAny<TentCategory?>(), It.IsAny<bool?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<Tent> { tA, tB });

            var regRepo = new Mock<IRegistrationRepository>();
            regRepo.Setup(x => x.GetMapByIdsAsync(It.IsAny<Guid[]>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new Dictionary<Guid, Registration> { [r.Id] = r });

            var assignRepo = new Mock<ITentAssignmentRepository>();
            var uow = new Mock<IUnitOfWork>();

            var handler = new UpdateTentRosterHandler(retreatRepo.Object, tentRepo.Object, assignRepo.Object, regRepo.Object, uow.Object);
            var res = await handler.Handle(new UpdateTentRosterCommand(retId, 1, new[]
            {
                new TentRosterSnapshot(tA.Id, new [] { new TentRosterMemberItem(r.Id, 0) }),
                new TentRosterSnapshot(tB.Id, new [] { new TentRosterMemberItem(r.Id, 0) })
            }), CancellationToken.None);

            res.Errors.Should().Contain(e => e.Code == "DUPLICATED_MEMBER");
        }

        [Fact]
        public async Task Wrong_category_and_invalid_member_should_return_both_errors()
        {
            var retId = Guid.NewGuid();
            var retreat = MakeRetreat(retId, 1, false);
            var tF = MakeTent(retId, TentCategory.Female, 1, 2);
            var rWrongGender = MakeReg(retId, "Mario Silva", Gender.Male, true, RegistrationStatus.Confirmed);
            var rInvalid = MakeReg(retId, "Bianca Souza", Gender.Female, true, RegistrationStatus.PendingPayment);

            var retreatRepo = new Mock<IRetreatRepository>();
            retreatRepo.Setup(x => x.GetByIdAsync(retId, It.IsAny<CancellationToken>())).ReturnsAsync(retreat);

            var tentRepo = new Mock<ITentRepository>();
            tentRepo.Setup(x => x.ListByRetreatAsync(retId, It.IsAny<TentCategory?>(), It.IsAny<bool?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Tent> { tF });

            var regRepo = new Mock<IRegistrationRepository>();
            regRepo.Setup(x => x.GetMapByIdsAsync(It.IsAny<Guid[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Dictionary<Guid, Registration> { [rWrongGender.Id] = rWrongGender, [rInvalid.Id] = rInvalid });

            var assignRepo = new Mock<ITentAssignmentRepository>();
            var uow = new Mock<IUnitOfWork>();

            var handler = new UpdateTentRosterHandler(retreatRepo.Object, tentRepo.Object, assignRepo.Object, regRepo.Object, uow.Object);
            var res = await handler.Handle(new UpdateTentRosterCommand(retId, 1, new[]
            {
                new TentRosterSnapshot(tF.Id, new []
                {
                    new TentRosterMemberItem(rWrongGender.Id, 0),
                    new TentRosterMemberItem(rInvalid.Id, 1)
                })
            }), CancellationToken.None);

            res.Errors.Should().Contain(e => e.Code == "WRONG_CATEGORY");
            res.Errors.Should().Contain(e => e.Code == "INVALID_MEMBER");
        }


        [Fact]
        public async Task Should_replace_only_touched_tents_and_keep_others_intact()
        {
            var retId = Guid.NewGuid();
            var retreat = MakeRetreat(retId, 7, false);
            var t1 = MakeTent(retId, TentCategory.Male, 1, 3);
            var t2 = MakeTent(retId, TentCategory.Male, 2, 3);
            var rKeep = MakeReg(retId, "Kleber Souza", Gender.Male, true, RegistrationStatus.Confirmed);
            var rA = MakeReg(retId, "Arthur Melo", Gender.Male, true, RegistrationStatus.Confirmed);
            var rB = MakeReg(retId, "Bruno Neri", Gender.Male, true, RegistrationStatus.PaymentConfirmed);

            var retreatRepo = new Mock<IRetreatRepository>();
            retreatRepo.Setup(x => x.GetByIdAsync(retId, It.IsAny<CancellationToken>())).ReturnsAsync(retreat);
            retreatRepo.Setup(x => x.UpdateAsync(retreat, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var tentRepo = new Mock<ITentRepository>();
            tentRepo.Setup(x => x.ListByRetreatAsync(retId, It.IsAny<TentCategory?>(), It.IsAny<bool?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<Tent> { t1, t2 });

            var existingTouched = new List<TentAssignment>
            {
                new TentAssignment(t1.Id, rKeep.Id, 0)
            };
            var finalAll = new List<TentAssignment>
            {
                new TentAssignment(t1.Id, rA.Id, 0),
                new TentAssignment(t1.Id, rB.Id, 1),
                new TentAssignment(t2.Id, rKeep.Id, 0)
            };

            var assignRepo = new Mock<ITentAssignmentRepository>();
            assignRepo.SetupSequence(x => x.ListByTentIdsAsync(It.IsAny<Guid[]>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(existingTouched)
                      .ReturnsAsync(finalAll);
            assignRepo.Setup(x => x.RemoveRangeAsync(It.IsAny<List<TentAssignment>>(), It.IsAny<CancellationToken>()))
                      .Returns(Task.CompletedTask);
            assignRepo.Setup(x => x.AddRangeAsync(It.IsAny<List<TentAssignment>>(), It.IsAny<CancellationToken>()))
                      .Returns(Task.CompletedTask);

            var regRepo = new Mock<IRegistrationRepository>();
            regRepo.Setup(x => x.GetMapByIdsAsync(It.IsAny<Guid[]>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync((Guid[] ids, CancellationToken _) =>
                  {
                      var map = new Dictionary<Guid, Registration>();
                      foreach (var r in new[] { rKeep, rA, rB }) map[r.Id] = r;
                      return map;
                  });

            var uow = new Mock<IUnitOfWork>();
            uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var handler = new UpdateTentRosterHandler(retreatRepo.Object, tentRepo.Object, assignRepo.Object, regRepo.Object, uow.Object);
            var cmd = new UpdateTentRosterCommand(retId, retreat.TentsVersion, new[]
            {
                new TentRosterSnapshot(t1.Id, new[]
                {
                    new TentRosterMemberItem(rA.Id, 0),
                    new TentRosterMemberItem(rB.Id, 1)
                })
            });

            var res = await handler.Handle(cmd, CancellationToken.None);
            res.Errors.Should().BeEmpty();
            res.Version.Should().BeGreaterThan(7);

            assignRepo.Verify(x => x.RemoveRangeAsync(It.Is<List<TentAssignment>>(l => l.Count == existingTouched.Count && l.All(a => a.TentId == t1.Id)), It.IsAny<CancellationToken>()), Times.Once);
            assignRepo.Verify(x => x.AddRangeAsync(It.Is<List<TentAssignment>>(l => l.Count == 2 && l.All(a => a.TentId == t1.Id)), It.IsAny<CancellationToken>()), Times.Once);

            var t1View = res.Tents.Single(t => t.TentId == t1.Id);
            t1View.Members.Select(m => m.RegistrationId).Should().BeEquivalentTo(new[] { rA.Id, rB.Id });
        }
    }
}
