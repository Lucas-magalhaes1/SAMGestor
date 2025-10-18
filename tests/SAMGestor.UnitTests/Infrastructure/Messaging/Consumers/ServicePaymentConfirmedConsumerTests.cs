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
using Xunit;

namespace SAMGestor.UnitTests.Infrastructure.Messaging.Consumers
{
    public class ServicePaymentConfirmedConsumerTests
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
            var conn = (RabbitMqConnection)FormatterServices.GetUninitializedObject(typeof(RabbitMqConnection));
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
        public async Task Creates_payment_links_confirms_and_autoassigns_when_possible()
        {
            var dbName = nameof(Creates_payment_links_confirms_and_autoassigns_when_possible) + Guid.NewGuid();

            using (var seed = NewDb(dbName))
            {
                var retreat = Retreat();
                var space = Space(retreat.Id, "Apoio", min: 0, max: 5, locked: false, active: true);
                var reg = Reg(retreat.Id, "João Silva", "joao@mail.com", "52998224725", pref: space.Id);
                await seed.Retreats.AddAsync(retreat);
                await seed.ServiceSpaces.AddAsync(space);
                await seed.ServiceRegistrations.AddAsync(reg);
                await seed.SaveChangesAsync();
            }

            var sp = NewProvider(dbName);
            var consumer = NewConsumer(sp, new ServiceAutoAssignOptions { Enabled = true, EnforceMax = true });
            var method = HandleMethod();

            Guid regId;
            using (var check = NewDb(dbName))
                regId = (await check.ServiceRegistrations.SingleAsync()).Id;

            var evt = NewEvt(regId, Guid.NewGuid(), 123.45m, DateTimeOffset.UtcNow, "pix");
            await (Task)method.Invoke(consumer, new object[] { evt, default(CancellationToken) })!;

            using (var verify = NewDb(dbName))
            {
                var payment = await verify.Payments.SingleAsync();
                payment.Id.Should().NotBeEmpty();
                payment.Status.Should().Be(PaymentStatus.Paid);
                payment.PaidAt.Should().NotBeNull();

                (await verify.ServiceRegistrationPayments.CountAsync()).Should().Be(1);

                var updatedReg = await verify.ServiceRegistrations.SingleAsync();
                updatedReg.Status.Should().Be(ServiceRegistrationStatus.Confirmed);

                var assign = await verify.ServiceAssignments.SingleAsync();
                assign.ServiceRegistrationId.Should().Be(updatedReg.Id);
                var spaceId = (await verify.ServiceSpaces.SingleAsync()).Id;
                assign.ServiceSpaceId.Should().Be(spaceId);
                assign.Role.Should().Be(ServiceRole.Member);
            }
        }

        [Fact]
        public async Task Idempotent_on_duplicate_event_payment_link_assignment()
        {
            var dbName = nameof(Idempotent_on_duplicate_event_payment_link_assignment) + Guid.NewGuid();
            Guid regId;

            using (var seed = NewDb(dbName))
            {
                var retreat = Retreat();
                var space = Space(retreat.Id, "Capela");
                var reg = Reg(retreat.Id, "Maria Souza", "maria@mail.com", "52998224726", pref: space.Id);
                await seed.Retreats.AddAsync(retreat);
                await seed.ServiceSpaces.AddAsync(space);
                await seed.ServiceRegistrations.AddAsync(reg);
                regId = reg.Id;
                await seed.SaveChangesAsync();
            }

            var sp = NewProvider(dbName);
            var consumer = NewConsumer(sp, new ServiceAutoAssignOptions { Enabled = true, EnforceMax = true });
            var method = HandleMethod();

            var payId = Guid.NewGuid();
            var evt = NewEvt(regId, payId, 50m, DateTimeOffset.UtcNow, "card");
            await (Task)method.Invoke(consumer, new object[] { evt, default(CancellationToken) })!;
            await (Task)method.Invoke(consumer, new object[] { evt, default(CancellationToken) })!;

            using (var verify = NewDb(dbName))
            {
                (await verify.Payments.CountAsync()).Should().Be(1);
                (await verify.ServiceRegistrationPayments.CountAsync()).Should().Be(1);
                (await verify.ServiceAssignments.CountAsync()).Should().Be(1);

                var payment = await verify.Payments.SingleAsync();
                payment.Status.Should().Be(PaymentStatus.Paid);
            }
        }

        [Fact]
        public async Task Does_nothing_when_registration_missing()
        {
            var dbName = nameof(Does_nothing_when_registration_missing) + Guid.NewGuid();

            var sp = NewProvider(dbName);
            var consumer = NewConsumer(sp, new ServiceAutoAssignOptions { Enabled = true, EnforceMax = true });
            var method = HandleMethod();

            var evt = NewEvt(Guid.NewGuid(), Guid.NewGuid(), 10m, DateTimeOffset.UtcNow, "pix");
            await (Task)method.Invoke(consumer, new object[] { evt, default(CancellationToken) })!;

            using (var verify = NewDb(dbName))
            {
                (await verify.Payments.CountAsync()).Should().Be(0);
                (await verify.ServiceRegistrationPayments.CountAsync()).Should().Be(0);
                (await verify.ServiceAssignments.CountAsync()).Should().Be(0);
            }
        }

        [Fact]
        public async Task Updates_existing_payment_to_paid_and_sets_paidAt()
        {
            var dbName = nameof(Updates_existing_payment_to_paid_and_sets_paidAt) + Guid.NewGuid();
            Guid regId, payId;

            using (var seed = NewDb(dbName))
            {
                var retreat = Retreat();
                var reg = Reg(retreat.Id, "Bruno Silva", "bruno@mail.com", "52998224727");
                await seed.Retreats.AddAsync(retreat);
                await seed.ServiceRegistrations.AddAsync(reg);

                var existing = new Payment(reg.Id, new Money(1, "BRL"), PaymentMethod.Pix);
                await seed.Payments.AddAsync(existing);
                regId = reg.Id;
                payId = existing.Id;

                await seed.SaveChangesAsync();
            }

            var sp = NewProvider(dbName);
            var consumer = NewConsumer(sp, new ServiceAutoAssignOptions { Enabled = false });
            var method = HandleMethod();

            var at = DateTimeOffset.UtcNow;
            var evt = NewEvt(regId, payId, 1m, at, "pix");
            await (Task)method.Invoke(consumer, new object[] { evt, default(CancellationToken) })!;

            using (var verify = NewDb(dbName))
            {
                var payment = await verify.Payments.SingleAsync(p => p.Id == payId);
                payment.Status.Should().Be(PaymentStatus.Paid);
                payment.PaidAt.Should().Be(at.UtcDateTime);
            }
        }
    }
}
