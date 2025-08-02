using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using SAMGestor.Application.Features.Retreats.GetById;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;
using Xunit;

namespace SAMGestor.UnitTests.Application.Features.Retreats.GetById
{
    public class GetRetreatByIdHandlerTests
    {
        private readonly Mock<IRetreatRepository> _repo = new();
        private readonly GetRetreatByIdHandler _handler;

        public GetRetreatByIdHandlerTests()
        {
            _handler = new GetRetreatByIdHandler(_repo.Object);
        }

        private static Retreat Build(Guid id)
            => new(
                new FullName("Retiro Handler"),
                "2032",
                "Tema",
                new DateOnly(2032, 1, 1),
                new DateOnly(2032, 1, 3),
                2, 2,
                new DateOnly(2031, 12, 1),
                new DateOnly(2031, 12, 10),
                new Money(200, "BRL"),
                new Money(80, "BRL"),
                new Percentage(60),
                new Percentage(40));

        [Fact]
        public async Task Handle_Returns_Response_When_Found()
        {
            // Arrange
            var id = Guid.NewGuid();
            var retreat = Build(id);

            _repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(retreat);

            var query = new GetRetreatByIdQuery(id);

            // Act
            var resp = await _handler.Handle(query, CancellationToken.None);

            // Assert
            resp.Id.Should().Be(retreat.Id);  
            resp.Name.Should().Be(retreat.Name.Value);
            resp.WestRegionPct.Should().Be(60);
            resp.OtherRegionPct.Should().Be(40);
        }

        [Fact]
        public async Task Handle_Throws_NotFound_When_Missing()
        {
            // Arrange
            var id = Guid.NewGuid();
            _repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Retreat?)null);

            var query = new GetRetreatByIdQuery(id);

            // Act
            Func<Task> act = () => _handler.Handle(query, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>()
                     .WithMessage($"*{id}*");
        }
    }
}
