using FluentAssertions;
using Moq;
using SAMGestor.Application.Features.Service.Registrations.Create;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.UnitTests.Application.Features.Service.Registrations.Create
{
    public class CreateServiceRegistrationHandlerTests
    {
        private readonly Mock<IServiceRegistrationRepository> _regRepo = new();
        private readonly Mock<IServiceSpaceRepository>        _spaceRepo = new();
        private readonly Mock<IRetreatRepository>             _retRepo = new();
        private readonly Mock<IUnitOfWork>                    _uow = new();

        private CreateServiceRegistrationHandler Handler()
            => new CreateServiceRegistrationHandler(_regRepo.Object, _spaceRepo.Object, _retRepo.Object, _uow.Object);

        private static Retreat OpenRetreat()
            => new Retreat(
                new FullName("Retiro Teste"),
                "ED1",
                "Tema",
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(12)),
                10, 10,
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
                new Money(0, "BRL"), new Money(0, "BRL"),
                new Percentage(50), new Percentage(50));

        private static Retreat ClosedRetreat()
            => new Retreat(
                new FullName("Retiro Fechado"),
                "ED1",
                "Tema",
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(12)),
                10, 10,
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)),
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)),
                new Money(0, "BRL"), new Money(0, "BRL"),
                new Percentage(50), new Percentage(50));

        private static ServiceSpace Space(Guid retreatId, string name, int min = 0, int max = 10, bool active = true)
        {
            var s = new ServiceSpace(retreatId, name, description: null, maxPeople: max, minPeople: min);
            if (!active)
            {
                var method = typeof(ServiceSpace).GetMethod("Deactivate") ?? typeof(ServiceSpace).GetMethod("Disable");
                method?.Invoke(s, null);
            }
            return s;
        }

        private static CreateServiceRegistrationCommand NewCmd(Guid retreatId, Guid? preferredSpaceId = null) =>
            new CreateServiceRegistrationCommand(
                RetreatId: retreatId,
                Name: new FullName("Fulano da Silva"),
                Cpf: new CPF("52998224725"),
                Email: new EmailAddress("fulano@mail.com"),
                Phone: "11999999999",
                BirthDate: new DateOnly(1990, 1, 1),
                Gender: Gender.Male,
                City: "SP",
                Region: "Oeste",
                PreferredSpaceId: preferredSpaceId
            );

        [Fact]
        public async Task Throw_NotFound_when_retreat_missing()
        {
            var cmd = NewCmd(Guid.NewGuid());
            _retRepo.Setup(r => r.GetByIdAsync(cmd.RetreatId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Retreat?)null);

            await FluentActions.Invoking(() => Handler().Handle(cmd, default))
                .Should().ThrowAsync<NotFoundException>()
                .WithMessage("*Retreat*");
        }

        [Fact]
        public async Task Throw_when_registration_window_closed()
        {
            var retreat = ClosedRetreat();
            var cmd = NewCmd(retreat.Id);

            _retRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(retreat);

            await FluentActions.Invoking(() => Handler().Handle(cmd, default))
                .Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("Registration period closed.");
        }

        [Fact]
        public async Task Throw_when_cpf_blocked()
        {
            var retreat = OpenRetreat();
            var cmd = NewCmd(retreat.Id);

            _retRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(retreat);

            _regRepo.Setup(r => r.IsCpfBlockedAsync(cmd.Cpf, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(true);

            await FluentActions.Invoking(() => Handler().Handle(cmd, default))
                .Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("CPF is blocked.");
        }

        [Fact]
        public async Task Throw_when_cpf_already_exists_in_retreat()
        {
            var retreat = OpenRetreat();
            var cmd = NewCmd(retreat.Id);

            _retRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(retreat);

            _regRepo.Setup(r => r.IsCpfBlockedAsync(cmd.Cpf, It.IsAny<CancellationToken>())).ReturnsAsync(false);
            _regRepo.Setup(r => r.ExistsByCpfInRetreatAsync(cmd.Cpf, cmd.RetreatId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            await FluentActions.Invoking(() => Handler().Handle(cmd, default))
                .Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("CPF already registered for this retreat (Serve).");
        }

        [Fact]
        public async Task Throw_when_email_already_exists_in_retreat()
        {
            var retreat = OpenRetreat();
            var cmd = NewCmd(retreat.Id);

            _retRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(retreat);

            _regRepo.Setup(r => r.IsCpfBlockedAsync(cmd.Cpf, It.IsAny<CancellationToken>())).ReturnsAsync(false);
            _regRepo.Setup(r => r.ExistsByCpfInRetreatAsync(cmd.Cpf, cmd.RetreatId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
            _regRepo.Setup(r => r.ExistsByEmailInRetreatAsync(cmd.Email, cmd.RetreatId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            await FluentActions.Invoking(() => Handler().Handle(cmd, default))
                .Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("Email already registered for this retreat (Serve).");
        }

        [Fact]
        public async Task Throw_when_has_active_spaces_and_preferred_space_missing()
        {
            var retreat = OpenRetreat();
            var cmd = NewCmd(retreat.Id, preferredSpaceId: null);

            _retRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(retreat);

            _regRepo.Setup(r => r.IsCpfBlockedAsync(cmd.Cpf, It.IsAny<CancellationToken>())).ReturnsAsync(false);
            _regRepo.Setup(r => r.ExistsByCpfInRetreatAsync(cmd.Cpf, cmd.RetreatId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
            _regRepo.Setup(r => r.ExistsByEmailInRetreatAsync(cmd.Email, cmd.RetreatId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

            _spaceRepo.Setup(s => s.HasActiveByRetreatAsync(cmd.RetreatId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            await FluentActions.Invoking(() => Handler().Handle(cmd, default))
                .Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("Preferred space is required.");
        }

        [Fact]
        public async Task Throw_when_preferred_space_not_found_or_different_retreat()
        {
            var retreat = OpenRetreat();
            var wrongRetreat = OpenRetreat();
            var space = Space(wrongRetreat.Id, "Apoio");
            var cmd = NewCmd(retreat.Id, space.Id);

            _retRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(retreat);

            _regRepo.Setup(r => r.IsCpfBlockedAsync(cmd.Cpf, It.IsAny<CancellationToken>())).ReturnsAsync(false);
            _regRepo.Setup(r => r.ExistsByCpfInRetreatAsync(cmd.Cpf, cmd.RetreatId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
            _regRepo.Setup(r => r.ExistsByEmailInRetreatAsync(cmd.Email, cmd.RetreatId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

            _spaceRepo.Setup(s => s.HasActiveByRetreatAsync(cmd.RetreatId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
            _spaceRepo.Setup(s => s.GetByIdAsync(cmd.PreferredSpaceId!.Value, It.IsAny<CancellationToken>())).ReturnsAsync(space);

            await FluentActions.Invoking(() => Handler().Handle(cmd, default))
                .Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("Preferred space not found for this retreat.");
        }

        [Fact]
        public async Task Throw_when_preferred_space_inactive()
        {
            var retreat = OpenRetreat();
            var space = Space(retreat.Id, "Cozinha", active: false);
            var cmd = NewCmd(retreat.Id, space.Id);

            _retRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(retreat);

            _regRepo.Setup(r => r.IsCpfBlockedAsync(cmd.Cpf, It.IsAny<CancellationToken>())).ReturnsAsync(false);
            _regRepo.Setup(r => r.ExistsByCpfInRetreatAsync(cmd.Cpf, cmd.RetreatId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
            _regRepo.Setup(r => r.ExistsByEmailInRetreatAsync(cmd.Email, cmd.RetreatId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

            _spaceRepo.Setup(s => s.HasActiveByRetreatAsync(cmd.RetreatId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
            _spaceRepo.Setup(s => s.GetByIdAsync(cmd.PreferredSpaceId!.Value, It.IsAny<CancellationToken>())).ReturnsAsync(space);

            await FluentActions.Invoking(() => Handler().Handle(cmd, default))
                .Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("Preferred space is inactive.");
        }

        [Fact]
        public async Task Success_when_no_active_spaces_and_no_preferred_space()
        {
            var retreat = OpenRetreat();
            var cmd = NewCmd(retreat.Id, preferredSpaceId: null);

            _retRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(retreat);

            _regRepo.Setup(r => r.IsCpfBlockedAsync(cmd.Cpf, It.IsAny<CancellationToken>())).ReturnsAsync(false);
            _regRepo.Setup(r => r.ExistsByCpfInRetreatAsync(cmd.Cpf, cmd.RetreatId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
            _regRepo.Setup(r => r.ExistsByEmailInRetreatAsync(cmd.Email, cmd.RetreatId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

            _spaceRepo.Setup(s => s.HasActiveByRetreatAsync(cmd.RetreatId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

            _regRepo.Setup(r => r.AddAsync(It.IsAny<ServiceRegistration>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var res = await Handler().Handle(cmd, default);

            res.ServiceRegistrationId.Should().NotBeEmpty();
            _regRepo.Verify(r => r.AddAsync(It.IsAny<ServiceRegistration>(), It.IsAny<CancellationToken>()), Times.Once);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Success_when_preferred_space_valid_and_active()
        {
            var retreat = OpenRetreat();
            var space = Space(retreat.Id, "Apoio");
            var cmd = NewCmd(retreat.Id, space.Id);

            _retRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(retreat);

            _regRepo.Setup(r => r.IsCpfBlockedAsync(cmd.Cpf, It.IsAny<CancellationToken>())).ReturnsAsync(false);
            _regRepo.Setup(r => r.ExistsByCpfInRetreatAsync(cmd.Cpf, cmd.RetreatId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
            _regRepo.Setup(r => r.ExistsByEmailInRetreatAsync(cmd.Email, cmd.RetreatId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

            _spaceRepo.Setup(s => s.HasActiveByRetreatAsync(cmd.RetreatId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
            _spaceRepo.Setup(s => s.GetByIdAsync(cmd.PreferredSpaceId!.Value, It.IsAny<CancellationToken>())).ReturnsAsync(space);

            _regRepo.Setup(r => r.AddAsync(It.IsAny<ServiceRegistration>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var res = await Handler().Handle(cmd, default);

            res.ServiceRegistrationId.Should().NotBeEmpty();
            _regRepo.Verify(r => r.AddAsync(It.IsAny<ServiceRegistration>(), It.IsAny<CancellationToken>()), Times.Once);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Translate_unique_violation_to_business_rule_exception()
        {
            var retreat = OpenRetreat();
            var cmd = NewCmd(retreat.Id, preferredSpaceId: null);

            _retRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(retreat);

            _regRepo.Setup(r => r.IsCpfBlockedAsync(cmd.Cpf, It.IsAny<CancellationToken>())).ReturnsAsync(false);
            _regRepo.Setup(r => r.ExistsByCpfInRetreatAsync(cmd.Cpf, cmd.RetreatId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
            _regRepo.Setup(r => r.ExistsByEmailInRetreatAsync(cmd.Email, cmd.RetreatId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
            _spaceRepo.Setup(s => s.HasActiveByRetreatAsync(cmd.RetreatId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

            _regRepo.Setup(r => r.AddAsync(It.IsAny<ServiceRegistration>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new UniqueConstraintViolationException("dup"));

            await FluentActions.Invoking(() => Handler().Handle(cmd, default))
                .Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("CPF or e-mail already registered for this retreat.");
        }
    }
}
