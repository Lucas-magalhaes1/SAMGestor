using FluentAssertions;
using Moq;
using SAMGestor.Application.Features.Retreats.Create;
using SAMGestor.Application.Interfaces;
using SAMGestor.Application.Services;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.UnitTests.Application.Features.Retreats.Create
{
    public class CreateRetreatHandlerTests
    {
        private readonly Mock<IRetreatRepository>     _repo;
        private readonly Mock<IUnitOfWork>            _uow;
        private readonly Mock<IServiceSpaceRepository> _svcSpaceRepo;
        private readonly ServiceSpacesSeeder          _seeder;
        private readonly CreateRetreatHandler         _handler;

        public CreateRetreatHandlerTests()
        {
            _repo         = new Mock<IRetreatRepository>();
            _uow          = new Mock<IUnitOfWork>();
            _svcSpaceRepo = new Mock<IServiceSpaceRepository>();
            _seeder  = new ServiceSpacesSeeder(_svcSpaceRepo.Object);
            _handler = new CreateRetreatHandler(_repo.Object, _uow.Object, _seeder);
        }

        private static CreateRetreatCommand NewCommand() =>
            new CreateRetreatCommand(
                new FullName("Handler Test"),
                "Edition1",
                "Theme",
                new DateOnly(2025, 1, 1),
                new DateOnly(2025, 1, 2),
                5, 5,
                new DateOnly(2025, 1, 1),
                new DateOnly(2025, 1, 2),
                new Money(100, "BRL"),
                new Money(50, "BRL"),
                new Percentage(60),
                new Percentage(40)
            );

        [Fact]
        public async Task Handle_Should_Create_Seed_DefaultSpaces_BumpVersion_And_ReturnResponse_When_Valid()
        {
            var cmd = NewCommand();

            _repo.Setup(r => r.ExistsByNameEditionAsync(cmd.Name, cmd.Edition, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);

            _repo.Setup(r => r.AddAsync(It.IsAny<Retreat>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

            _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _svcSpaceRepo.Setup(s => s.ListByRetreatAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync(new List<ServiceSpace>());

            _svcSpaceRepo.Setup(s => s.AddRangeAsync(
                    It.Is<IEnumerable<ServiceSpace>>(list =>
                        list != null &&
                        list.Count() == DefaultServiceSpaces.Names.Length && 
                        list.All(ss => DefaultServiceSpaces.Names.Contains(ss.Name)) &&
                        list.All(ss => ss.MaxPeople == 8 && ss.MinPeople == 0)
                    ),
                    It.IsAny<CancellationToken>()))
                         .Returns(Task.CompletedTask);
            
            var response = await _handler.Handle(cmd, CancellationToken.None);
            
            response.Should().NotBeNull();
            response.RetreatId.Should().NotBeEmpty();

            _repo.Verify(r => r.AddAsync(It.Is<Retreat>(rt =>
                   rt.Name.Value == cmd.Name.Value &&
                   rt.Edition    == cmd.Edition),
                It.IsAny<CancellationToken>()), Times.Once);

          
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
            _svcSpaceRepo.Verify(s => s.ListByRetreatAsync(response.RetreatId, It.IsAny<CancellationToken>()), Times.Once);
            _svcSpaceRepo.Verify(s => s.AddRangeAsync(It.IsAny<IEnumerable<ServiceSpace>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ThrowBusinessRuleException_When_Duplicate()
        {
           
            var cmd = NewCommand();

            _repo.Setup(r => r.ExistsByNameEditionAsync(cmd.Name, cmd.Edition, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);
            
            var act = async () => await _handler.Handle(cmd, CancellationToken.None);
            
            await act.Should().ThrowAsync<BusinessRuleException>()
                     .WithMessage("Retreat already exists.");

            _repo.Verify(r => r.AddAsync(It.IsAny<Retreat>(), It.IsAny<CancellationToken>()), Times.Never);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

            _svcSpaceRepo.Verify(s => s.ListByRetreatAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
            _svcSpaceRepo.Verify(s => s.AddRangeAsync(It.IsAny<IEnumerable<ServiceSpace>>(), It.IsAny<CancellationToken>()), Times.Never);
        }
        [Fact]
public async Task Handle_Should_Seed_Only_Missing_ServiceSpaces_When_Some_Exist()
{
   
    var cmd = NewCommand();

    _repo.Setup(r => r.ExistsByNameEditionAsync(cmd.Name, cmd.Edition, It.IsAny<CancellationToken>()))
         .ReturnsAsync(false);

    Retreat? created = null;
    _repo.Setup(r => r.AddAsync(It.IsAny<Retreat>(), It.IsAny<CancellationToken>()))
         .Callback<Retreat, CancellationToken>((rt, _) => created = rt)
         .Returns(Task.CompletedTask);

    _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
        .Returns(Task.CompletedTask);

    var existingNames = new[] { "Casa da Mãe (CDM)", "Capela" };

    _svcSpaceRepo.Setup(s => s.ListByRetreatAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(() => new List<ServiceSpace>
                 {
                     new ServiceSpace(created!.Id, existingNames[0], description: null, maxPeople: 99, minPeople: 0),
                     new ServiceSpace(created!.Id, existingNames[1], description: null, maxPeople: 99, minPeople: 0),
                 });

    _svcSpaceRepo.Setup(s => s.AddRangeAsync(
            It.Is<IEnumerable<ServiceSpace>>(list =>
                list != null &&
                list.Count() == DefaultServiceSpaces.Names.Length - existingNames.Length &&
                !list.Any(ss => existingNames.Contains(ss.Name)) &&
                list.All(ss => DefaultServiceSpaces.Names.Contains(ss.Name)) &&
                list.All(ss => ss.MaxPeople == 8 && ss.MinPeople == 0) &&
                
                list.All(ss => ss.RetreatId == created!.Id)
            ),
            It.IsAny<CancellationToken>()))
         .Returns(Task.CompletedTask);
    
    var response = await _handler.Handle(cmd, CancellationToken.None);
    
    response.Should().NotBeNull();
    response.RetreatId.Should().Be(created!.Id);
    
    _repo.Verify(r => r.AddAsync(It.IsAny<Retreat>(), It.IsAny<CancellationToken>()), Times.Once);
    _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    _svcSpaceRepo.Verify(s => s.ListByRetreatAsync(created!.Id, It.IsAny<CancellationToken>()), Times.Once);
    _svcSpaceRepo.Verify(s => s.AddRangeAsync(It.IsAny<IEnumerable<ServiceSpace>>(), It.IsAny<CancellationToken>()), Times.Once);
}

    }
}
