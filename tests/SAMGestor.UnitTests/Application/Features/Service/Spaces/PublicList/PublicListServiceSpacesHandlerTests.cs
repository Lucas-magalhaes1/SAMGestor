using FluentAssertions;
using Moq;
using SAMGestor.Application.Features.Service.Spaces.PublicList;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.UnitTests.Application.Features.Service.Spaces.PublicList
{
    public class PublicListServiceSpacesHandlerTests
    {
        private readonly Mock<IRetreatRepository> _retreatRepo = new();
        private readonly Mock<IServiceSpaceRepository> _spaceRepo = new();

        private PublicListServiceSpacesHandler Handler()
            => new PublicListServiceSpacesHandler(_retreatRepo.Object, _spaceRepo.Object);

        private static Retreat OpenRetreat()
            => new Retreat(
                new FullName("Retiro Teste"),
                "ED1", "Tema",
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(12)),
                10, 10,
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
                new Money(0, "BRL"), new Money(0, "BRL"),
                new Percentage(50), new Percentage(50));

        private static ServiceSpace Space(Guid retreatId, string name, string? desc, bool active = true)
        {
            var s = new ServiceSpace(retreatId, name, desc, maxPeople: 10, minPeople: 0);
            if (!active) s.Deactivate();
            return s;
        }

        [Fact]
        public async Task Throw_NotFound_when_retreat_missing()
        {
            var q = new PublicListServiceSpacesQuery(Guid.NewGuid());
            _retreatRepo.Setup(r => r.GetByIdAsync(q.RetreatId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync((Retreat?)null);

            await FluentActions.Invoking(() => Handler().Handle(q, default))
                .Should().ThrowAsync<NotFoundException>()
                .WithMessage("*Retreat*");
        }

        [Fact]
        public async Task Return_empty_when_no_active_spaces()
        {
            var retreat = OpenRetreat();
            _retreatRepo.Setup(r => r.GetByIdAsync(retreat.Id, default)).ReturnsAsync(retreat);
            _spaceRepo.Setup(s => s.ListActiveByRetreatAsync(retreat.Id, default))
                      .ReturnsAsync(new List<ServiceSpace>());

            var res = await Handler().Handle(new PublicListServiceSpacesQuery(retreat.Id), default);

            res.Version.Should().Be(retreat.ServiceSpacesVersion);
            res.Items.Should().BeEmpty();
        }

        [Fact]
        public async Task Return_active_spaces_sorted_by_name_with_basic_fields()
        {
            var retreat = OpenRetreat();
            var s1 = Space(retreat.Id, "Cozinha", "Preparo");
            var s2 = Space(retreat.Id, "Apoio", "Geral");
            var s3 = Space(retreat.Id, "Capela", null);

            _retreatRepo.Setup(r => r.GetByIdAsync(retreat.Id, default)).ReturnsAsync(retreat);
            _spaceRepo.Setup(s => s.ListActiveByRetreatAsync(retreat.Id, default))
                      .ReturnsAsync(new List<ServiceSpace> { s1, s2, s3 });

            var res = await Handler().Handle(new PublicListServiceSpacesQuery(retreat.Id), default);

            res.Items.Select(i => i.Name).Should().Equal("Apoio", "Capela", "Cozinha");
            res.Items.First().Description.Should().Be("Geral");
            res.Items[1].Description.Should().BeNull();
            res.Items.Last().Description.Should().Be("Preparo");
        }
    }
}
