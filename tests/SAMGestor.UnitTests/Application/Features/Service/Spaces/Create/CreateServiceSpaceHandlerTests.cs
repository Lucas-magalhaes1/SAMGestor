using FluentAssertions;
using Moq;
using SAMGestor.Application.Features.Service.Spaces.Create;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.UnitTests.Application.Features.Service.Spaces.Create
{
    public class CreateServiceSpaceHandlerTests
    {
        private readonly Mock<IRetreatRepository> _retreatRepo = new();
        private readonly Mock<IServiceSpaceRepository> _spaceRepo = new();
        private readonly Mock<IUnitOfWork> _uow = new();

        private CreateServiceSpaceHandler Handler()
            => new CreateServiceSpaceHandler(_retreatRepo.Object, _spaceRepo.Object, _uow.Object);

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

        [Fact]
        public async Task Throw_NotFound_when_retreat_missing()
        {
            var cmd = new CreateServiceSpaceCommand(Guid.NewGuid(), "Apoio", "Desc", 1, 5, true);
            _retreatRepo.Setup(r => r.GetByIdAsync(cmd.RetreatId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync((Retreat?)null);

            await FluentActions.Invoking(() => Handler().Handle(cmd, default))
                .Should().ThrowAsync<NotFoundException>()
                .WithMessage("*Retreat*");
        }

        [Fact]
        public async Task Throw_when_name_already_exists_in_retreat()
        {
            var retreat = OpenRetreat();
            var cmd = new CreateServiceSpaceCommand(retreat.Id, "Apoio", "Desc", 1, 5, true);

            _retreatRepo.Setup(r => r.GetByIdAsync(retreat.Id, default)).ReturnsAsync(retreat);
            _spaceRepo.Setup(s => s.ExistsByNameInRetreatAsync(retreat.Id, cmd.Name, default)).ReturnsAsync(true);

            await FluentActions.Invoking(() => Handler().Handle(cmd, default))
                .Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("Já existe um espaço com esse nome neste retiro.");

            _spaceRepo.Verify(s => s.AddAsync(It.IsAny<ServiceSpace>(), It.IsAny<CancellationToken>()), Times.Never);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Success_creates_active_space_trims_fields_bumps_version_and_saves()
        {
            var retreat = OpenRetreat();
            var cmd = new CreateServiceSpaceCommand(retreat.Id, "  Apoio  ", "  Desc  ", 1, 5, true);

            _retreatRepo.Setup(r => r.GetByIdAsync(retreat.Id, default)).ReturnsAsync(retreat);
            _spaceRepo.Setup(s => s.ExistsByNameInRetreatAsync(retreat.Id, cmd.Name, default)).ReturnsAsync(false);
            _retreatRepo.Setup(r => r.UpdateAsync(retreat, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            ServiceSpace? created = null;
            _spaceRepo.Setup(s => s.AddAsync(It.IsAny<ServiceSpace>(), It.IsAny<CancellationToken>()))
                      .Callback<ServiceSpace, CancellationToken>((sp, _) => created = sp)
                      .Returns(Task.CompletedTask);

            var prevVersion = retreat.ServiceSpacesVersion;
            var res = await Handler().Handle(cmd, default);

            created.Should().NotBeNull();
            created!.Name.Should().Be("Apoio");
            created.Description.Should().Be("Desc");
            created.MinPeople.Should().Be(1);
            created.MaxPeople.Should().Be(5);
            created.IsActive.Should().BeTrue();

            retreat.ServiceSpacesVersion.Should().Be(prevVersion + 1);
            res.SpaceId.Should().Be(created.Id);
            res.Version.Should().Be(retreat.ServiceSpacesVersion);

            _spaceRepo.Verify(s => s.AddAsync(It.IsAny<ServiceSpace>(), It.IsAny<CancellationToken>()), Times.Once);
            _retreatRepo.Verify(r => r.UpdateAsync(retreat, It.IsAny<CancellationToken>()), Times.Once);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Success_creates_inactive_space_when_IsActive_false()
        {
            var retreat = OpenRetreat();
            var cmd = new CreateServiceSpaceCommand(retreat.Id, "Cozinha", "", 2, 6, false);

            _retreatRepo.Setup(r => r.GetByIdAsync(retreat.Id, default)).ReturnsAsync(retreat);
            _spaceRepo.Setup(s => s.ExistsByNameInRetreatAsync(retreat.Id, cmd.Name, default)).ReturnsAsync(false);
            _retreatRepo.Setup(r => r.UpdateAsync(retreat, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            ServiceSpace? created = null;
            _spaceRepo.Setup(s => s.AddAsync(It.IsAny<ServiceSpace>(), It.IsAny<CancellationToken>()))
                      .Callback<ServiceSpace, CancellationToken>((sp, _) => created = sp)
                      .Returns(Task.CompletedTask);

            var res = await Handler().Handle(cmd, default);

            created.Should().NotBeNull();
            created!.IsActive.Should().BeFalse();
            created.Description.Should().BeNull();

            res.SpaceId.Should().Be(created.Id);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
