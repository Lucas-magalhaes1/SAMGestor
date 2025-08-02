using FluentAssertions;
using Moq;
using SAMGestor.Application.Features.Retreats.GetAll;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;


namespace SAMGestor.UnitTests.Application.Features.Retreats.GetAll
{
    public class ListRetreatsHandlerTests
    {
        private readonly Mock<IRetreatRepository> _repo = new();
        private readonly ListRetreatsHandler _handler;

        public ListRetreatsHandlerTests()
        {
            _handler = new ListRetreatsHandler(_repo.Object);
        }

        private static Retreat Build(Guid id, int plusDays)
            => new(
                new FullName($"Retiro {id:N}"),
                "2026",
                "Tema",
                new DateOnly(2026, 1, 1 + plusDays),
                new DateOnly(2026, 1, 2 + plusDays),
                1, 1,
                new DateOnly(2025, 12, 1),
                new DateOnly(2025, 12, 2),
                new Money(100, "BRL"),
                new Money(50,  "BRL"),
                new Percentage(50),
                new Percentage(50));

        [Fact]
        public async Task Handle_Returns_Paginated_List_And_Total()
        {
            // Arrange
            var total = 3;
            var retreats = new List<Retreat>
            {
                Build(Guid.NewGuid(), 0),
                Build(Guid.NewGuid(), 1),
                Build(Guid.NewGuid(), 2)
            };

            _repo.Setup(r => r.CountAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync(total);

            _repo.Setup(r => r.ListAsync(0, 20, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(retreats);

            var query = new ListRetreatsQuery(0, 20);

            // Act
            var resp = await _handler.Handle(query, CancellationToken.None);

            // Assert
            resp.TotalCount.Should().Be(total);
            resp.Items.Should().HaveCount(3);
            resp.Items.Should().AllSatisfy(dto => dto.Name.Should().StartWith("Retiro"));
            resp.Skip.Should().Be(0);
            resp.Take.Should().Be(20);
        }

        [Fact]
        public async Task Handle_Normalizes_Negative_Skip_And_Take_Zero()
        {
            // Arrange
            _repo.Setup(r => r.CountAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync(0);
            _repo.Setup(r => r.ListAsync(0, 20, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<Retreat>());
            
            var query = new ListRetreatsQuery(-5, 0); // skip negativo, take zero

            // Act
            var resp = await _handler.Handle(query, CancellationToken.None);

            // Assert
            _repo.Verify(r => r.ListAsync(0, 20, It.IsAny<CancellationToken>()), Times.Once);
            resp.Skip.Should().Be(0);
            resp.Take.Should().Be(20);
        }
    }
}
