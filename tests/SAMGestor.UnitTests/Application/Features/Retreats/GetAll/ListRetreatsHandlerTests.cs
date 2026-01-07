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
            var totalCount = 3;
            var retreats = new List<Retreat>
            {
                Build(Guid.NewGuid(), 0),
                Build(Guid.NewGuid(), 1),
                Build(Guid.NewGuid(), 2)
            };

            // ✅ Mock retorna tupla agora
            _repo.Setup(r => r.ListAsync(0, 20, It.IsAny<CancellationToken>()))
                .ReturnsAsync((retreats, totalCount));

            var query = new ListRetreatsQuery(0, 20);

            // Act
            var resp = await _handler.Handle(query, CancellationToken.None);

            // Assert
            resp.TotalCount.Should().Be(totalCount);
            resp.Items.Should().HaveCount(3);
            resp.Items.Should().AllSatisfy(dto => dto.Name.Should().StartWith("Retiro"));
            resp.Skip.Should().Be(0);
            resp.Take.Should().Be(20);
        }
        [Fact]
        public async Task Handle_Uses_Skip_Take_From_Query_Without_Normalization()
        {
            // Arrange
            var retreats = new List<Retreat>();
    
            // ✅ Mock retorna tupla
            _repo.Setup(r => r.ListAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((retreats, 0));
    
            // ✅ Normalização agora é feita no PagedQuery, não no handler
            var query = new ListRetreatsQuery(Skip: -5, Take: 0); 

            // Act
            var resp = await _handler.Handle(query, CancellationToken.None);

            // Assert
            // ✅ PagedQuery normaliza: skip negativo vira 0, take 0 permanece 0
            resp.Skip.Should().Be(0);
            resp.Take.Should().Be(0);
        }
    }
}
