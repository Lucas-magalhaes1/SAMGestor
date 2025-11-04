using FluentAssertions;
using Moq;
using SAMGestor.Application.Features.Registrations.GetAll;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;


namespace SAMGestor.UnitTests.Application.Features.Registrations.GetAll;

public class GetAllRegistrationsHandlerTests
{
    private readonly Mock<IRegistrationRepository> _repo = new();
    private readonly Mock<IStorageService> _storage = new();    
    private readonly GetAllRegistrationsHandler _handler;

    public GetAllRegistrationsHandlerTests()
    {
        _storage
            .Setup(s => s.GetPublicUrl(It.IsAny<string>()))
            .Returns((string key) => $"http://storage.test/{key}");

        _handler = new GetAllRegistrationsHandler(_repo.Object, _storage.Object); 
    }

    [Fact]
    public async Task Handle_applies_status_filter_case_insensitive()
    {
        var retreatId = Guid.NewGuid();
        var items = new List<Domain.Entities.Registration>
        {
            MakeReg(retreatId, "Ana Silva",   "93541134780", "ana@t.com",   Gender.Female, RegistrationStatus.Selected,  yearsAgo: 25, city: "SP"),
            MakeReg(retreatId, "Beto Souza",  "52998224725", "beto@t.com",  Gender.Male,   RegistrationStatus.Confirmed, yearsAgo: 30, city: "SP"),
            MakeReg(retreatId, "Cris Dias",   "15350946056", "cris@t.com",  Gender.Male,   RegistrationStatus.Confirmed, yearsAgo: 22, city: "RJ")
        };

        _repo
            .Setup(r => r.ListAsync(
                retreatId,
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(items);

        var q = new GetAllRegistrationsQuery(retreatId, status: "confirmed", skip: 0, take: 50);
        var resp = await _handler.Handle(q, CancellationToken.None);

        resp.TotalCount.Should().Be(2);
        resp.Items.Select(i => i.Name).Should().BeEquivalentTo(new[] { "Beto Souza", "Cris Dias" });
    }

    [Fact]
    public async Task Handle_applies_gender_age_city_search_hasPhoto_and_pagination()
    {
        var retreatId = Guid.NewGuid();
        var withPhoto = MakeReg(retreatId, "Maria Silva", "93541134780", "maria@t.com", Gender.Female, RegistrationStatus.Selected, yearsAgo: 20, city: "São Paulo");
        SetPhoto(withPhoto);

        var items = new List<Domain.Entities.Registration>
        {
            withPhoto,
            MakeReg(retreatId, "Mario Souza",  "11144477735", "mario@t.com",  Gender.Male,   RegistrationStatus.PendingPayment, yearsAgo: 28, city: "Santos"),
            MakeReg(retreatId, "Marina Dias",  "28625587887", "marina@t.com", Gender.Female, RegistrationStatus.Confirmed,       yearsAgo: 35, city: "São Carlos"),
            MakeReg(retreatId, "Paula Santos", "15350946056", "paula@t.com",  Gender.Female, RegistrationStatus.Canceled,        yearsAgo: 19, city: "São Paulo")
        };

        _repo
            .Setup(r => r.ListAsync(
                retreatId,
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(items);

        var q = new GetAllRegistrationsQuery(
            retreatId: retreatId,
            status: null,
            gender: Gender.Female,
            minAge: 20,
            maxAge: 30,
            city: "são",
            state: null,
            search: "maria",
            hasPhoto: true,
            skip: 0,
            take: 1
        );

        var resp = await _handler.Handle(q, CancellationToken.None);

        resp.TotalCount.Should().Be(1);
        resp.Items.Should().HaveCount(1);
        resp.Items[0].Name.Should().Be("Maria Silva");
        resp.Items[0].Cpf.Should().Be("93541134780");
        resp.Items[0].Gender.Should().Be(nameof(Gender.Female));
    }

    [Fact]
    public async Task Handle_status_param_is_forwarded_to_repository_and_reinforced_in_memory()
    {
        var retreatId = Guid.NewGuid();
        var items = new List<Domain.Entities.Registration>
        {
            MakeReg(retreatId, "Xavier Lima", "52998224725", "x@t.com", Gender.Male, RegistrationStatus.PendingPayment, yearsAgo: 40, city: "SP"),
            MakeReg(retreatId, "Yuri Costa",  "15350946056", "y@t.com", Gender.Male, RegistrationStatus.Confirmed,       yearsAgo: 41, city: "SP")
        };

        _repo
            .Setup(r => r.ListAsync(
                retreatId,
                "PendingPayment",
                It.IsAny<string?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(items);

        var q = new GetAllRegistrationsQuery(retreatId, status: "PendingPayment", skip: 0, take: 10);
        var resp = await _handler.Handle(q, CancellationToken.None);

        _repo.Verify(r => r.ListAsync(
            retreatId,
            "PendingPayment",
            It.IsAny<string?>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Once);

        resp.TotalCount.Should().Be(1);
        resp.Items.Single().Status.Should().Be(nameof(RegistrationStatus.PendingPayment));
        resp.Items.Single().Name.Should().Be("Xavier Lima");
    }

    [Fact]
    public async Task Handle_search_matches_name_cpf_email_case_insensitive()
    {
        var retreatId = Guid.NewGuid();
        var a = MakeReg(retreatId, "Fulano Teste",    "52998224725", "alpha@t.com",   Gender.Male, RegistrationStatus.PendingPayment, yearsAgo: 33, city: "SP");
        var b = MakeReg(retreatId, "Beltrano Souza",  "15350946056", "bravo@t.com",   Gender.Male, RegistrationStatus.PendingPayment, yearsAgo: 34, city: "SP");
        var c = MakeReg(retreatId, "Ciclano Costa",   "11144477735", "charlie@t.com", Gender.Male, RegistrationStatus.PendingPayment, yearsAgo: 35, city: "SP");

        _repo
            .Setup(r => r.ListAsync(
                retreatId,
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Domain.Entities.Registration> { a, b, c });

        var byName  = await _handler.Handle(new GetAllRegistrationsQuery(retreatId, search: "fulano"), CancellationToken.None);
        var byCpf   = await _handler.Handle(new GetAllRegistrationsQuery(retreatId, search: "15350946056"), CancellationToken.None);
        var byEmail = await _handler.Handle(new GetAllRegistrationsQuery(retreatId, search: "CHARLIE@T.COM"), CancellationToken.None);

        byName.TotalCount.Should().Be(1);
        byName.Items.Single().Name.Should().Be("Fulano Teste");

        byCpf.TotalCount.Should().Be(1);
        byCpf.Items.Single().Name.Should().Be("Beltrano Souza");

        byEmail.TotalCount.Should().Be(1);
        byEmail.Items.Single().Name.Should().Be("Ciclano Costa");
    }

    [Fact]
    public async Task Handle_pagination_skips_and_takes_correctly_and_projects_fields()
    {
        var retreatId = Guid.NewGuid();
        var names = new[]
        {
            "Alfa Um","Bravo Dois","Charlie Tres","Delta Quatro","Eco Cinco",
            "Foxtrot Seis","Golf Sete","Hotel Oito","India Nove","Juliet Dez"
        };

        var regs = Enumerable.Range(1, 10)
            .Select(i => MakeReg(
                retreatId,
                names[i - 1],
                $"0000000000{i % 10}",
                $"p{i}@t.com",
                Gender.Male,
                RegistrationStatus.PendingPayment,
                yearsAgo: 20 + i,
                city: "City"))
            .ToList();

        _repo
            .Setup(r => r.ListAsync(
                retreatId,
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(regs);

        var q = new GetAllRegistrationsQuery(retreatId, skip: 3, take: 4);
        var resp = await _handler.Handle(q, CancellationToken.None);

        resp.TotalCount.Should().Be(10);
        resp.Items.Should().HaveCount(4);
        resp.Items.First().Name.Should().Be("Delta Quatro");
        resp.Items.Last().Name.Should().Be("Golf Sete");
        resp.Items.All(i => i.Id != Guid.Empty && !string.IsNullOrWhiteSpace(i.Cpf)).Should().BeTrue();
    }

    private static Domain.Entities.Registration MakeReg(
        Guid retreatId,
        string name,
        string cpf,
        string email,
        Gender gender,
        RegistrationStatus status,
        int yearsAgo,
        string city)
    {
        var birth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-yearsAgo).Date);
        return new Domain.Entities.Registration(
            new FullName(name),
            new CPF(cpf),
            new EmailAddress(email),
            "11999999999",
            birth,
            gender,
            city,
            status,
            retreatId
        );
    }

    private static void SetPhoto(Domain.Entities.Registration reg)
    {
        var url = new UrlAddress("https://example.com/photo.jpg");
        reg.SetPhoto("key/photo.jpg", "image/jpeg", 1234, DateTime.UtcNow, url);
    }
}
