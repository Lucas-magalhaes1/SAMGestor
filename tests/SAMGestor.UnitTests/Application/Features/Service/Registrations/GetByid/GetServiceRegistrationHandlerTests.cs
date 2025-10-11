using FluentAssertions;
using Moq;
using SAMGestor.Application.Features.Service.Registrations.GetById;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.UnitTests.Application.Features.Service.Registrations.GetById
{
    public class GetServiceRegistrationHandlerTests
    {
        private readonly Mock<IServiceRegistrationRepository> _regRepo = new();
        private readonly Mock<IServiceSpaceRepository> _spaceRepo = new();

        private GetServiceRegistrationHandler Handler()
            => new GetServiceRegistrationHandler(_regRepo.Object, _spaceRepo.Object);

        private static ServiceRegistration NewReg(Guid retreatId, string name = "Fulano da Silva", Guid? preferredSpaceId = null)
            => new ServiceRegistration(
                retreatId,
                new FullName(name),
                new CPF("52998224725"),
                new EmailAddress($"{Guid.NewGuid()}@mail.com"),
                "11999999999",
                new DateOnly(1990, 1, 1),
                Gender.Male,
                "SP",
                "Oeste",
                preferredSpaceId
            );

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

        [Fact]
        public async Task Throw_NotFound_when_registration_missing()
        {
            var q = new GetServiceRegistrationQuery(Guid.NewGuid(), Guid.NewGuid());
            _regRepo.Setup(r => r.GetByIdAsync(q.RegistrationId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((ServiceRegistration?)null);

            await FluentActions.Invoking(() => Handler().Handle(q, default))
                .Should().ThrowAsync<NotFoundException>()
                .WithMessage("*ServiceRegistration*");
        }

        [Fact]
        public async Task Throw_NotFound_when_retreat_mismatch()
        {
            var retreatId = Guid.NewGuid();
            var otherRetreat = Guid.NewGuid();
            var reg = NewReg(otherRetreat);
            var q = new GetServiceRegistrationQuery(retreatId, reg.Id);

            _regRepo.Setup(r => r.GetByIdAsync(reg.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(reg);

            await FluentActions.Invoking(() => Handler().Handle(q, default))
                .Should().ThrowAsync<NotFoundException>()
                .WithMessage("*ServiceRegistration*");
        }

        [Fact]
        public async Task Return_response_without_preferred_space_when_null()
        {
            var retreatId = Guid.NewGuid();
            var reg = NewReg(retreatId, preferredSpaceId: null);
            var q = new GetServiceRegistrationQuery(retreatId, reg.Id);

            _regRepo.Setup(r => r.GetByIdAsync(reg.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(reg);

            var res = await Handler().Handle(q, default);

            res.Id.Should().Be(reg.Id);
            res.RetreatId.Should().Be(reg.RetreatId);
            res.FullName.Should().Be((string)reg.Name);
            res.Cpf.Should().Be(reg.Cpf.Value);
            res.Email.Should().Be(reg.Email.Value);
            res.Phone.Should().Be(reg.Phone);
            res.BirthDate.Should().Be(reg.BirthDate);
            res.Gender.Should().Be(reg.Gender);
            res.City.Should().Be(reg.City);
            res.Region.Should().Be(reg.Region);
            res.Status.Should().Be(reg.Status);
            res.Enabled.Should().Be(reg.Enabled);
            res.RegistrationDateUtc.Should().Be(reg.RegistrationDate);
            res.PreferredSpace.Should().BeNull();
        }

        [Fact]
        public async Task Preferred_space_missing_in_repo_results_in_null_view()
        {
            var retreatId = Guid.NewGuid();
            var spaceId = Guid.NewGuid();
            var reg = NewReg(retreatId, preferredSpaceId: spaceId);
            var q = new GetServiceRegistrationQuery(retreatId, reg.Id);

            _regRepo.Setup(r => r.GetByIdAsync(reg.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(reg);
            _spaceRepo.Setup(s => s.GetByIdAsync(spaceId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync((ServiceSpace?)null);

            var res = await Handler().Handle(q, default);

            res.PreferredSpace.Should().BeNull();
        }

        [Fact]
        public async Task Preferred_space_found_is_mapped_in_view()
        {
            var retreatId = Guid.NewGuid();
            var space = Space(retreatId, "Apoio");
            var reg = NewReg(retreatId, preferredSpaceId: space.Id);
            var q = new GetServiceRegistrationQuery(retreatId, reg.Id);

            _regRepo.Setup(r => r.GetByIdAsync(reg.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(reg);
            _spaceRepo.Setup(s => s.GetByIdAsync(space.Id, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(space);

            var res = await Handler().Handle(q, default);

            res.PreferredSpace.Should().NotBeNull();
            res.PreferredSpace!.Id.Should().Be(space.Id);
            res.PreferredSpace!.Name.Should().Be(space.Name);
        }
    }
}
