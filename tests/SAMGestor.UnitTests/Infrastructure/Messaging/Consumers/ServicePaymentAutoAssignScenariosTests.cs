using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.Serialization;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using SAMGestor.Contracts;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.ValueObjects;
using SAMGestor.Infrastructure.Messaging.Consumers;
using SAMGestor.Infrastructure.Messaging.Options;
using SAMGestor.Infrastructure.Messaging.RabbitMq;
using SAMGestor.Infrastructure.Persistence;
using SAMGestor.UnitTests.Dependencies;
using Xunit;

namespace SAMGestor.UnitTests.Infrastructure.Messaging.Consumers
{
    public class ServicePaymentAutoAssignScenariosTests
    {
        private static readonly ConcurrentDictionary<string, InMemoryDatabaseRoot> Roots = new();

        private static InMemoryDatabaseRoot RootFor(string name)
            => Roots.GetOrAdd(name, _ => new InMemoryDatabaseRoot());

        private static SAMContext NewDb(string dbName)
        {
            var opts = new DbContextOptionsBuilder<SAMContext>()
                .UseInMemoryDatabase(dbName, RootFor(dbName))
                .EnableSensitiveDataLogging()
                .Options;
            return new SAMContext(opts);
        }

        private static ServiceProvider NewProvider(string dbName)
        {
            var sc = new ServiceCollection();
            sc.AddDbContext<SAMContext>(o =>
            {
                o.UseInMemoryDatabase(dbName, RootFor(dbName));
                o.EnableSensitiveDataLogging();
            }, contextLifetime: ServiceLifetime.Scoped, optionsLifetime: ServiceLifetime.Singleton);
            return sc.BuildServiceProvider();
        }

        private static ServicePaymentConfirmedConsumer NewConsumer(IServiceProvider sp, ServiceAutoAssignOptions autoOpt)
        {
            var opt = new RabbitMqOptions();
            var conn = TestObjectFactory.Uninitialized<RabbitMqConnection>();
            var logger = new Mock<ILogger<ServicePaymentConfirmedConsumer>>().Object;
            return new ServicePaymentConfirmedConsumer(opt, conn, logger, sp, autoOpt);
        }

        private static MethodInfo HandleMethod()
            => typeof(ServicePaymentConfirmedConsumer)
                .GetMethod("HandleAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;

        private static Retreat Retreat()
            => new Retreat(new FullName("Retiro Teste"), "ED1", "Tema",
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(12)),
                10, 10,
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
                new Money(0, "BRL"), new Money(0, "BRL"),
                new Percentage(50), new Percentage(50));

        private static ServiceSpace Space(Guid retreatId, string name, int min = 0, int max = 10, bool locked = false, bool active = true)
        {
            var s = new ServiceSpace(retreatId, name, description: null, maxPeople: max, minPeople: min);
            if (!active) s.Deactivate();
            if (locked) s.Lock();
            return s;
        }

        private static ServiceRegistration Reg(Guid retreatId, string name, string email, string cpf, Guid? pref = null)
            => new ServiceRegistration(
                retreatId,
                new FullName(name),
                new CPF(cpf),
                new EmailAddress(email),
                "11999999999",
                new DateOnly(1990, 1, 1),
                Gender.Male,
                "SP",
                "Oeste",
                preferredSpaceId: pref
            );

        private static object NewEvt(Guid regId, Guid payId, decimal amount, DateTimeOffset paidAt, string method = "pix")
        {
            var t = typeof(PaymentConfirmedV1);
            var ctor = t.GetConstructors().Single();
            var pars = ctor.GetParameters();
            var args = new object?[pars.Length];

            for (int i = 0; i < pars.Length; i++)
            {
                var pt = pars[i].ParameterType;
                var name = (pars[i].Name ?? string.Empty).ToLowerInvariant();

                if (pt == typeof(Guid))
                {
                    if (name.Contains("registration")) args[i] = regId;
                    else if (name.Contains("payment")) args[i] = payId;
                    else args[i] = payId;
                }
                else if (pt == typeof(decimal))
                {
                    args[i] = amount;
                }
                else if (pt == typeof(DateTimeOffset))
                {
                    args[i] = paidAt;
                }
                else if (pt == typeof(string))
                {
                    if (name.Contains("method")) args[i] = method;
                    else args[i] = string.Empty;
                }
                else if (pt == typeof(int))
                {
                    args[i] = 0;
                }
                else if (pt == typeof(bool))
                {
                    args[i] = false;
                }
                else
                {
                    args[i] = null;
                }
            }
            return ctor.Invoke(args);
        }

        [Fact]
        public async Task No_assignment_when_no_preference()
        {
            var dbName = nameof(No_assignment_when_no_preference) + Guid.NewGuid();

            Guid regId;
            using (var seed = NewDb(dbName))
            {
                var retreat = Retreat();
                var reg = Reg(retreat.Id, "Sem Preferencia", "a@mail.com", "52998224725", pref: null);
                await seed.Retreats.AddAsync(retreat);
                await seed.ServiceRegistrations.AddAsync(reg);
                regId = reg.Id;
                await seed.SaveChangesAsync();
            }

            var sp = NewProvider(dbName);
            var consumer = NewConsumer(sp, new ServiceAutoAssignOptions { Enabled = true, EnforceMax = true });
            var method = HandleMethod();

            await (Task)method.Invoke(consumer, new object[] { NewEvt(regId, Guid.NewGuid(), 10m, DateTimeOffset.UtcNow), default(CancellationToken) })!;

            using (var verify = NewDb(dbName))
                (await verify.ServiceAssignments.CountAsync()).Should().Be(0);
        }

        [Fact]
        public async Task No_assignment_when_space_locked()
        {
            var dbName = nameof(No_assignment_when_space_locked) + Guid.NewGuid();

            Guid regId;
            using (var seed = NewDb(dbName))
            {
                var retreat = Retreat();
                var space = Space(retreat.Id, "Locked", max: 5, locked: true);
                var reg = Reg(retreat.Id, "João Silva", "x@mail.com", "52998224726", pref: space.Id);
                await seed.Retreats.AddAsync(retreat);
                await seed.ServiceSpaces.AddAsync(space);
                await seed.ServiceRegistrations.AddAsync(reg);
                regId = reg.Id;
                await seed.SaveChangesAsync();
            }

            var sp = NewProvider(dbName);
            var consumer = NewConsumer(sp, new ServiceAutoAssignOptions { Enabled = true, EnforceMax = true });
            var method = HandleMethod();

            await (Task)method.Invoke(consumer, new object[] { NewEvt(regId, Guid.NewGuid(), 10m, DateTimeOffset.UtcNow), default(CancellationToken) })!;

            using (var verify = NewDb(dbName))
                (await verify.ServiceAssignments.CountAsync()).Should().Be(0);
        }

        [Fact]
        public async Task No_assignment_when_enforceMax_and_full()
        {
            var dbName = nameof(No_assignment_when_enforceMax_and_full) + Guid.NewGuid();

            Guid regId;
            using (var seed = NewDb(dbName))
            {
                var retreat = Retreat();
                var space = Space(retreat.Id, "Cheio", max: 1);
                var occupant = Reg(retreat.Id, "Carlos Dias", "carlos@mail.com", "52998224730", pref: space.Id);
                var reg = Reg(retreat.Id, "Ana Lima", "ana@mail.com", "52998224727", pref: space.Id);
                await seed.Retreats.AddAsync(retreat);
                await seed.ServiceSpaces.AddAsync(space);
                await seed.ServiceRegistrations.AddAsync(occupant);
                await seed.ServiceRegistrations.AddAsync(reg);
                await seed.ServiceAssignments.AddAsync(new ServiceAssignment(space.Id, occupant.Id, ServiceRole.Member));
                regId = reg.Id;
                await seed.SaveChangesAsync();
            }

            var sp = NewProvider(dbName);
            var consumer = NewConsumer(sp, new ServiceAutoAssignOptions { Enabled = true, EnforceMax = true });
            var method = HandleMethod();

            await (Task)method.Invoke(consumer, new object[] { NewEvt(regId, Guid.NewGuid(), 10m, DateTimeOffset.UtcNow), default(CancellationToken) })!;

            using (var verify = NewDb(dbName))
                (await verify.ServiceAssignments.CountAsync()).Should().Be(1);
        }

        [Fact]
        public async Task No_duplicate_when_already_assigned()
        {
            var dbName = nameof(No_duplicate_when_already_assigned) + Guid.NewGuid();

            Guid regId;
            using (var seed = NewDb(dbName))
            {
                var retreat = Retreat();
                var space = Space(retreat.Id, "Apoio");
                var reg = Reg(retreat.Id, "Bea Rocha", "bea@mail.com", "52998224728", pref: space.Id);
                await seed.Retreats.AddAsync(retreat);
                await seed.ServiceSpaces.AddAsync(space);
                await seed.ServiceRegistrations.AddAsync(reg);
                await seed.ServiceAssignments.AddAsync(new ServiceAssignment(space.Id, reg.Id, ServiceRole.Member));
                regId = reg.Id;
                await seed.SaveChangesAsync();
            }

            var sp = NewProvider(dbName);
            var consumer = NewConsumer(sp, new ServiceAutoAssignOptions { Enabled = true, EnforceMax = true });
            var method = HandleMethod();

            await (Task)method.Invoke(consumer, new object[] { NewEvt(regId, Guid.NewGuid(), 10m, DateTimeOffset.UtcNow), default(CancellationToken) })!;

            using (var verify = NewDb(dbName))
                (await verify.ServiceAssignments.CountAsync()).Should().Be(1);
        }

        [Fact]
        public async Task Assignment_created_and_version_bumped_when_possible()
        {
            var dbName = nameof(Assignment_created_and_version_bumped_when_possible) + Guid.NewGuid();

            Guid regId;
            int beforeVersion;
            using (var seed = NewDb(dbName))
            {
                var retreat = Retreat();
                var space = Space(retreat.Id, "Livre", max: 3);
                var reg = Reg(retreat.Id, "Davi Souza", "davi@mail.com", "52998224729", pref: space.Id);
                await seed.Retreats.AddAsync(retreat);
                await seed.ServiceSpaces.AddAsync(space);
                await seed.ServiceRegistrations.AddAsync(reg);
                beforeVersion = retreat.ServiceSpacesVersion;
                regId = reg.Id;
                await seed.SaveChangesAsync();
            }

            var sp = NewProvider(dbName);
            var consumer = NewConsumer(sp, new ServiceAutoAssignOptions { Enabled = true, EnforceMax = true });
            var method = HandleMethod();

            await (Task)method.Invoke(consumer, new object[] { NewEvt(regId, Guid.NewGuid(), 10m, DateTimeOffset.UtcNow), default(CancellationToken) })!;

            using (var verify = NewDb(dbName))
            {
                (await verify.ServiceAssignments.CountAsync()).Should().Be(1);
                var ver = await verify.Retreats.Select(r => r.ServiceSpacesVersion).SingleAsync();
                ver.Should().Be(beforeVersion + 1);
            }
        }
    }
}
