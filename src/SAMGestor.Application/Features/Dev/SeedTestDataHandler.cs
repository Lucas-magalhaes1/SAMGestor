using Bogus;
using MediatR;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.Application.Features.Dev.Seed;

public sealed class SeedTestDataHandler : IRequestHandler<SeedTestDataCommand, SeedTestDataResponse>
{
    private readonly IRetreatRepository _retreatRepo;
    private readonly IRegistrationRepository _regRepo;
    private readonly IServiceRegistrationRepository _serviceRegRepo;
    private readonly IServiceSpaceRepository _serviceSpaceRepo;
    private readonly IServiceAssignmentRepository _serviceAssignRepo;
    private readonly IUnitOfWork _uow;

    private static readonly string[] BrazilianCities = new[]
    {
        "São Paulo", "Rio de Janeiro", "Belo Horizonte", "Curitiba", "Porto Alegre",
        "Recife", "Salvador", "Fortaleza", "Brasília", "Florianópolis",
        "Cacador", "Goiânia", "Manaus", "Belém", "Campinas"
    };

    private static readonly string[] CommonSurnames = new[]
    {
        "Silva", "Santos", "Souza", "Oliveira", "Lima", "Costa", "Pereira", "Ferreira",
        "Rodrigues", "Alves", "Nascimento", "Araújo", "Ribeiro", "Carvalho", "Gomes"
    };

    public SeedTestDataHandler(
        IRetreatRepository retreatRepo,
        IRegistrationRepository regRepo,
        IServiceRegistrationRepository serviceRegRepo,
        IServiceSpaceRepository serviceSpaceRepo,
        IServiceAssignmentRepository serviceAssignRepo,
        IUnitOfWork uow)
    {
        _retreatRepo = retreatRepo;
        _regRepo = regRepo;
        _serviceRegRepo = serviceRegRepo;
        _serviceSpaceRepo = serviceSpaceRepo;
        _serviceAssignRepo = serviceAssignRepo;
        _uow = uow;
    }

    public async Task<SeedTestDataResponse> Handle(SeedTestDataCommand cmd, CancellationToken ct)
    {
        Randomizer.Seed = new Random(42); // seed fixo para reproduzir

        // ===== SEED 1: Contemplação (300 FAZER + 300 SERVIR Notified) =====
        var seed1Retreat = CreateRetreat(
            "Retiro Contemplação [SEED]",
            "SEED-CONT-2026",
            maleSlots: 100,
            femaleSlots: 100
        );

        await _retreatRepo.AddAsync(seed1Retreat, ct);

        // Criar espaços de serviço padrão para Seed 1
        var seed1Spaces = CreateDefaultServiceSpaces(seed1Retreat.Id);
        await _serviceSpaceRepo.AddRangeAsync(seed1Spaces, ct);
        
        var seed1Regs = GenerateRegistrations(
            seed1Retreat.Id,
            maleCount: 150,
            femaleCount: 150,
            status: RegistrationStatus.NotSelected,
            includeExtendedFields: false
        );

        await _regRepo.AddRangeAsync(seed1Regs, ct);

        // Criar 300 inscrições SERVIR (Notified)
        var seed1ServiceRegs = GenerateServiceRegistrations(
            seed1Retreat.Id,
            seed1Regs,
            seed1Spaces,
            confirmed: false // Apenas Notified
        );

        foreach (var serviceReg in seed1ServiceRegs)
        {
            await _serviceRegRepo.AddAsync(serviceReg, ct);
        }

        // ===== SEED 2: Famílias (200 FAZER + 200 SERVIR Confirmed) =====
        var seed2Retreat = CreateRetreat(
            "Retiro Famílias [SEED]",
            "SEED-FAM-2026",
            maleSlots: 100,
            femaleSlots: 100
        );

        await _retreatRepo.AddAsync(seed2Retreat, ct);

        // Criar espaços de serviço com capacidades variadas para Seed 2
        var seed2Spaces = CreateTestServiceSpaces(seed2Retreat.Id);
        await _serviceSpaceRepo.AddRangeAsync(seed2Spaces, ct);

        var seed2Regs = GenerateRegistrations(
            seed2Retreat.Id,
            maleCount: 100,
            femaleCount: 100,
            status: RegistrationStatus.Confirmed,
            includeExtendedFields: true
        );

        // Metade PaymentConfirmed
        for (int i = 0; i < seed2Regs.Count / 2; i++)
        {
            seed2Regs[i].SetStatus(RegistrationStatus.PaymentConfirmed);
        }

        await _regRepo.AddRangeAsync(seed2Regs, ct);

        // Criar 200 inscrições SERVIR (150 Confirmed + alocados, 50 Notified)
        var (seed2ServiceRegs, seed2Assignments) = GenerateServiceRegistrationsWithAllocations(
            seed2Retreat.Id,
            seed2Regs,
            seed2Spaces
        );

        foreach (var serviceReg in seed2ServiceRegs)
        {
            await _serviceRegRepo.AddAsync(serviceReg, ct);
        }        await _serviceAssignRepo.AddRangeAsync(seed2Assignments, ct);

        await _uow.SaveChangesAsync(ct);

        return new SeedTestDataResponse(
            Seed1RetreatId: seed1Retreat.Id,
            Seed1Registrations: seed1Regs.Count,
            Seed1ServiceRegistrations: seed1ServiceRegs.Count,
            Seed1ServiceSpaces: seed1Spaces.Count,
            Seed2RetreatId: seed2Retreat.Id,
            Seed2Registrations: seed2Regs.Count,
            Seed2ServiceRegistrations: seed2ServiceRegs.Count,
            Seed2ServiceSpaces: seed2Spaces.Count,
            Seed2ServiceAssignments: seed2Assignments.Count
        );
    }

    // ===== SERVICE SPACE HELPERS =====

    private static List<ServiceSpace> CreateDefaultServiceSpaces(Guid retreatId)
    {
        var names = new[]
        {
            "Casa da Mãe (CDM)", "Casa do Pai (CDP)", "Tapera", "Apoio", "Manutenção",
            "Cozinha", "Externa", "Loja", "Capela", "Guardião",
            "Cantina", "Madrinha", "Padrinho", "Música", "Teatro", "Saúde", "Secretaria"
        };

        return names.Select(name => 
            new ServiceSpace(retreatId, name, null, maxPeople: 15, minPeople: 3)
        ).ToList();
    }

    private static List<ServiceSpace> CreateTestServiceSpaces(Guid retreatId)
    {
        return new List<ServiceSpace>
        {
            new(retreatId, "Cozinha", "Espaço superlotado de propósito", maxPeople: 12, minPeople: 8),
            new(retreatId, "Casa da Mãe (CDM)", null, maxPeople: 20, minPeople: 15),
            new(retreatId, "Casa do Pai (CDP)", null, maxPeople: 20, minPeople: 15),
            new(retreatId, "Externa", "Espaço vazio de propósito", maxPeople: 25, minPeople: 5),
            new(retreatId, "Apoio", null, maxPeople: 15, minPeople: 10),
            new(retreatId, "Tapera", null, maxPeople: 12, minPeople: 8),
            new(retreatId, "Manutenção", null, maxPeople: 10, minPeople: 5),
            new(retreatId, "Loja", null, maxPeople: 8, minPeople: 4),
            new(retreatId, "Capela", null, maxPeople: 6, minPeople: 3),
            new(retreatId, "Música", null, maxPeople: 10, minPeople: 5)
        };
    }

    // ===== SERVICE REGISTRATION HELPERS =====

    private static List<ServiceRegistration> GenerateServiceRegistrations(
        Guid retreatId,
        List<Registration> fazerRegs,
        List<ServiceSpace> spaces,
        bool confirmed)
    {
        var faker = new Faker("pt_BR");
        var serviceRegs = new List<ServiceRegistration>();

        foreach (var reg in fazerRegs)
        {
            var serviceReg = new ServiceRegistration(
                retreatId,
                reg.Name,
                reg.Cpf,
                reg.Email,
                reg.Phone,
                reg.BirthDate,
                reg.Gender,
                reg.City,
                reg.State.ToString(),
                preferredSpaceId: faker.PickRandom(spaces).Id // preferência aleatória
            );

            // Marcar como Notified
            serviceReg.MarkNotified();

            // Se confirmed = true, confirmar também
            if (confirmed)
            {
                serviceReg.Confirm();
            }

            serviceRegs.Add(serviceReg);
        }

        return serviceRegs;
    }

    private static (List<ServiceRegistration>, List<ServiceAssignment>) GenerateServiceRegistrationsWithAllocations(
        Guid retreatId,
        List<Registration> fazerRegs,
        List<ServiceSpace> spaces)
    {
        var faker = new Faker("pt_BR");
        var serviceRegs = new List<ServiceRegistration>();
        var assignments = new List<ServiceAssignment>();

        var cozinha = spaces.First(s => s.Name == "Cozinha");
        var cdm = spaces.First(s => s.Name == "Casa da Mãe (CDM)");
        var cdp = spaces.First(s => s.Name == "Casa do Pai (CDP)");
        var apoio = spaces.First(s => s.Name == "Apoio");

        for (int i = 0; i < fazerRegs.Count; i++)
        {
            var reg = fazerRegs[i];
            
            // Definir preferências estratégicas (para gerar alertas)
            Guid? preferredSpaceId;
            if (i < 60)
                preferredSpaceId = cozinha.Id; // 60 preferem Cozinha (superlotação!)
            else if (i < 100)
                preferredSpaceId = cdm.Id; // 40 preferem CDM
            else if (i < 140)
                preferredSpaceId = cdp.Id; // 40 preferem CDP
            else if (i < 160)
                preferredSpaceId = apoio.Id; // 20 preferem Apoio
            else
                preferredSpaceId = null; // 40 sem preferência

            var serviceReg = new ServiceRegistration(
                retreatId,
                reg.Name,
                reg.Cpf,
                reg.Email,
                reg.Phone,
                reg.BirthDate,
                reg.Gender,
                reg.City,
                reg.State.ToString(),
                preferredSpaceId
            );

            // Primeiros 150: Confirmed + Alocados
            if (i < 150)
            {
                serviceReg.MarkNotified();
                serviceReg.Confirm();

                // Alocar em espaços
                ServiceSpace targetSpace;
                ServiceRole role;

                if (i < 12) // Cozinha: 12 pessoas (1 coord + 1 vice + 10 membros)
                {
                    targetSpace = cozinha;
                    role = i == 0 ? ServiceRole.Coordinator : (i == 1 ? ServiceRole.Vice : ServiceRole.Member);
                }
                else if (i < 32) // CDM: 20 pessoas
                {
                    targetSpace = cdm;
                    role = i == 12 ? ServiceRole.Coordinator : (i == 13 ? ServiceRole.Vice : ServiceRole.Member);
                }
                else if (i < 52) // CDP: 20 pessoas
                {
                    targetSpace = cdp;
                    role = i == 32 ? ServiceRole.Coordinator : (i == 33 ? ServiceRole.Vice : ServiceRole.Member);
                }
                else if (i < 67) // Apoio: 15 pessoas
                {
                    targetSpace = apoio;
                    role = i == 52 ? ServiceRole.Coordinator : (i == 53 ? ServiceRole.Vice : ServiceRole.Member);
                }
                else // Distribuir restantes em outros espaços
                {
                    targetSpace = faker.PickRandom(spaces.Where(s => s.Name != "Externa").ToList());
                    role = ServiceRole.Member;
                }

                assignments.Add(new ServiceAssignment(targetSpace.Id, serviceReg.Id, role));
            }
            else // Últimos 50: apenas Notified (não confirmaram)
            {
                serviceReg.MarkNotified();
            }

            serviceRegs.Add(serviceReg);
        }

        return (serviceRegs, assignments);
    }

    // ===== RETREAT & REGISTRATION HELPERS (ORIGINAL - SEM MUDANÇAS) =====

    private static Retreat CreateRetreat(string name, string edition, int maleSlots, int femaleSlots)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        
        return new Retreat(
            name: new FullName(name),
            edition: edition,
            theme: "Encontro com Cristo",
            startDate: today.AddDays(60),
            endDate: today.AddDays(62),
            maleSlots: maleSlots,
            femaleSlots: femaleSlots,
            registrationStart: today.AddDays(-30),
            registrationEnd: today.AddDays(30),
            feeFazer: new Money(250.00m, "BRL"),
            feeServir: new Money(150.00m, "BRL"),
            westPct: new Percentage(60),
            othersPct: new Percentage(40)
        );
    }

    private List<Registration> GenerateRegistrations(
        Guid retreatId,
        int maleCount,
        int femaleCount,
        RegistrationStatus status,
        bool includeExtendedFields)
    {
        var registrations = new List<Registration>();
        var usedEmails = new HashSet<string>();
        var usedCpfs = new HashSet<string>();

        // Gerar homens
        var maleFaker = CreateFaker(Gender.Male, retreatId, status, includeExtendedFields);
        for (int i = 0; i < maleCount; i++)
        {
            var reg = maleFaker.Generate();
            EnsureUnique(reg, usedEmails, usedCpfs);
            registrations.Add(reg);
        }

        // Gerar mulheres
        var femaleFaker = CreateFaker(Gender.Female, retreatId, status, includeExtendedFields);
        for (int i = 0; i < femaleCount; i++)
        {
            var reg = femaleFaker.Generate();
            EnsureUnique(reg, usedEmails, usedCpfs);
            registrations.Add(reg);
        }

        return registrations;
    }

    private static Faker<Registration> CreateFaker(
        Gender gender,
        Guid retreatId,
        RegistrationStatus status,
        bool includeExtendedFields)
    {
        return new Faker<Registration>("pt_BR")
            .CustomInstantiator(f =>
            {
                var firstName = gender == Gender.Male
                    ? f.Name.FirstName(Bogus.DataSets.Name.Gender.Male)
                    : f.Name.FirstName(Bogus.DataSets.Name.Gender.Female);

                var lastName = f.Random.Bool(0.3f)
                    ? f.PickRandom(CommonSurnames)
                    : f.Name.LastName();

                var fullName = $"{firstName} {lastName}";
                var cpf = GenerateValidCPF(f);
                var email = f.Internet.Email(firstName, lastName).ToLowerInvariant();
                var phone = f.Phone.PhoneNumber("###########");
                var birthDate = DateOnly.FromDateTime(f.Date.Past(yearsToGoBack: 42, refDate: DateTime.Today.AddYears(-18)));
                var city = f.PickRandom(BrazilianCities);

                var reg = new Registration(
                    name: new FullName(fullName),
                    cpf: new CPF(cpf),
                    email: new EmailAddress(email),
                    phone: phone,
                    birthDate: birthDate,
                    gender: gender,
                    city: city,
                    status: status,
                    retreatId: retreatId
                );

                // ✅ CAMPOS OBRIGATÓRIOS BÁSICOS
                var state = f.PickRandom<UF>();
                reg.SetAddress(null, null, state, city);

                // Pais (obrigatórios)
                reg.SetFather(
                    f.PickRandom<ParentStatus>(),
                    f.Name.FullName(Bogus.DataSets.Name.Gender.Male),
                    f.Phone.PhoneNumber("###########")
                );
                reg.SetMother(
                    f.PickRandom<ParentStatus>(),
                    f.Name.FullName(Bogus.DataSets.Name.Gender.Female),
                    f.Phone.PhoneNumber("###########")
                );

                // Religião (obrigatório)
                reg.SetReligion(f.PickRandom("Católico", "Evangélico", "Espírita", "Sem religião", "Outra"));

                // Termos (obrigatório)
                reg.AcceptTerms("seed-v1.0", DateTime.UtcNow.AddDays(-f.Random.Int(1, 30)));

                // Marketing opt-in
                reg.SetMarketingOptIn(true, DateTime.UtcNow.AddDays(-f.Random.Int(1, 30)));

                // Client context
                reg.SetClientContext(f.Internet.Ip(), f.Internet.UserAgent());

                if (includeExtendedFields)
                {
                    EnrichRegistration(reg, f, gender);
                }
                else
                {
                    SetBasicFields(reg, f, gender);
                }

                return reg;
            });
    }

    private static void SetBasicFields(Registration reg, Faker f, Gender gender)
    {
        // Campos básicos necessários
        reg.SetMaritalStatus(f.PickRandom<Domain.Enums.MaritalStatus>());
        reg.SetShirtSize(f.PickRandom<Domain.Enums.ShirtSize>());
        reg.SetAnthropometrics(
            weightKg: f.Random.Decimal(50, 120),
            heightCm: f.Random.Decimal(150, 200)
        );
        reg.SetProfession(f.Name.JobTitle());

        // ✅ NÃO chamar SetAddress novamente pra não sobrescrever o estado
        // Usar reflection pra setar endereço sem mexer no estado
        var streetAndNumberProp = typeof(Registration).GetProperty("StreetAndNumber", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        var neighborhoodProp = typeof(Registration).GetProperty("Neighborhood", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        
        streetAndNumberProp?.SetValue(reg, f.Address.StreetAddress());
        neighborhoodProp?.SetValue(reg, f.Address.SecondaryAddress());

        // Contatos
        reg.SetWhatsapp(f.Phone.PhoneNumber("###########"));
        reg.SetNeighborPhone(f.Phone.PhoneNumber("###########"));
        reg.SetRelativePhone(f.Phone.PhoneNumber("###########"));

        // Perda familiar (condicional)
        var hadLoss = f.Random.Bool(0.1f);
        reg.SetFamilyLoss(hadLoss, hadLoss ? "Perda de familiar próximo" : null);

        // Submitter (condicional)
        var hasSubmitter = f.Random.Bool(0.3f);
        reg.SetSubmitterInfo(
            hasSubmitter,
            hasSubmitter ? f.PickRandom(RelationshipDegree.Friend, RelationshipDegree.Cousin) : RelationshipDegree.None,
            hasSubmitter ? f.Name.FullName() : null
        );

        // Tentativas anteriores
        reg.SetPreviousUncalledApplications(f.Random.Bool(0.15f) 
            ? f.PickRandom(RahaminAttempt.RahaminVidaX_2023_02_Cacador, RahaminAttempt.RahaminVidaXI_2023_10_Cacador)
            : RahaminAttempt.None);

        // Álcool/Tabaco/Drogas
        reg.SetAlcoholUse(f.PickRandom<AlcoholUsePattern>());
        reg.SetSmoker(f.Random.Bool(0.2f));
        
        var usesDrugs = f.Random.Bool(0.05f);
        reg.SetDrugUse(usesDrugs, usesDrugs ? f.PickRandom("Ocasional", "Regular") : null);

        // Alergias (condicional)
        var hasAllergies = f.Random.Bool(0.15f);
        reg.SetAllergies(hasAllergies, hasAllergies ? f.PickRandom("Frutos do mar", "Lactose", "Glúten", "Amendoim") : null);

        // Restrição médica (condicional)
        var hasMedicalRestriction = f.Random.Bool(0.1f);
        reg.SetMedicalRestriction(hasMedicalRestriction, hasMedicalRestriction ? f.PickRandom("Hipertensão", "Diabetes", "Asma") : null);

        // Medicamentos (condicional)
        var takesMedication = f.Random.Bool(0.2f);
        reg.SetMedications(takesMedication, takesMedication ? f.PickRandom("Losartana 50mg", "Metformina", "Omeprazol") : null);

        // Limitação física (opcional)
        if (f.Random.Bool(0.05f))
            reg.SetPhysicalLimitationDetails(f.PickRandom("Dificuldade para caminhar longas distâncias", "Problemas na coluna"));

        // Cirurgia recente (opcional)
        if (f.Random.Bool(0.03f))
            reg.SetRecentSurgeryOrProcedureDetails(f.PickRandom("Apendicectomia há 3 meses", "Cirurgia de hérnia"));

        // Gravidez (só mulheres)
        if (gender == Gender.Female && f.Random.Bool(0.05f))
        {
            reg.SetPregnancy(f.PickRandom(
                PregnancyStatus.Weeks0To12,
                PregnancyStatus.Weeks13To24
            ));
        }

        // RahaminVida completado
        if (f.Random.Bool(0.1f))
        {
            reg.SetRahaminVidaCompleted(f.PickRandom(
                RahaminVidaEdition.VidaX_2023_02_Cacador,
                RahaminVidaEdition.VidaXI_2023_10_Cacador,
                RahaminVidaEdition.VidaXII_2024_02_Cacador
            ));
        }
    }

    private static void EnrichRegistration(Registration reg, Faker f, Gender gender)
    {
        // Campos básicos
        reg.SetMaritalStatus(f.PickRandom<Domain.Enums.MaritalStatus>());
        reg.SetShirtSize(f.PickRandom<Domain.Enums.ShirtSize>());
        reg.SetAnthropometrics(
            weightKg: f.Random.Decimal(50, 120),
            heightCm: f.Random.Decimal(150, 200)
        );
        reg.SetProfession(f.Name.JobTitle());

        // ✅ Usar reflection pra setar endereço sem sobrescrever o estado
        var streetAndNumberProp = typeof(Registration).GetProperty("StreetAndNumber", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        var neighborhoodProp = typeof(Registration).GetProperty("Neighborhood", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        
        streetAndNumberProp?.SetValue(reg, f.Address.StreetAddress());
        neighborhoodProp?.SetValue(reg, f.Address.SecondaryAddress());

        // Contatos sociais (mais chance de ter)
        reg.SetWhatsapp(f.Phone.PhoneNumber("###########"));
        if (f.Random.Bool(0.8f))
            reg.SetFacebookUsername(f.Internet.UserName());
        if (f.Random.Bool(0.7f))
            reg.SetInstagramHandle($"@{f.Internet.UserName()}");
        
        reg.SetNeighborPhone(f.Phone.PhoneNumber("###########"));
        reg.SetRelativePhone(f.Phone.PhoneNumber("###########"));

        // Perda familiar
        var hadLoss = f.Random.Bool(0.15f);
        reg.SetFamilyLoss(hadLoss, hadLoss ? f.Lorem.Sentence(10) : null);

        // Submitter
        var hasSubmitter = f.Random.Bool(0.4f);
        reg.SetSubmitterInfo(
            hasSubmitter,
            hasSubmitter ? f.PickRandom(
                RelationshipDegree.Friend, 
                RelationshipDegree.Cousin, 
                RelationshipDegree.Spouse,
                RelationshipDegree.Mother
            ) : RelationshipDegree.None,
            hasSubmitter ? $"{f.Name.FullName()}, {f.Name.FullName()}" : null
        );

        // Tentativas anteriores
        reg.SetPreviousUncalledApplications(f.Random.Bool(0.2f) 
            ? f.PickRandom(
                RahaminAttempt.RahaminVidaX_2023_02_Cacador, 
                RahaminAttempt.RahaminVidaXI_2023_10_Cacador,
                RahaminAttempt.RahaminVidaXII_2024_02_Cacador
            )
            : RahaminAttempt.None);

        // Álcool/Tabaco/Drogas
        reg.SetAlcoholUse(f.PickRandom<AlcoholUsePattern>());
        reg.SetSmoker(f.Random.Bool(0.25f));
        
        var usesDrugs = f.Random.Bool(0.08f);
        reg.SetDrugUse(usesDrugs, usesDrugs ? f.PickRandom("Ocasional", "Regular", "Raro") : null);

        // Alergias
        var hasAllergies = f.Random.Bool(0.2f);
        reg.SetAllergies(hasAllergies, hasAllergies ? f.PickRandom(
            "Frutos do mar", 
            "Lactose", 
            "Glúten", 
            "Amendoim",
            "Pólen",
            "Medicamentos (Penicilina)"
        ) : null);

        // Restrição médica
        var hasMedicalRestriction = f.Random.Bool(0.15f);
        reg.SetMedicalRestriction(hasMedicalRestriction, hasMedicalRestriction ? f.PickRandom(
            "Hipertensão arterial", 
            "Diabetes tipo 2", 
            "Asma brônquica",
            "Problemas cardíacos",
            "Epilepsia controlada"
        ) : null);

        // Medicamentos
        var takesMedication = f.Random.Bool(0.3f);
        reg.SetMedications(takesMedication, takesMedication ? f.PickRandom(
            "Losartana 50mg - 1x ao dia",
            "Metformina 850mg - 2x ao dia", 
            "Omeprazol 20mg - 1x ao dia",
            "Sinvastatina 20mg - noturno",
            "Levotiroxina 50mcg - jejum"
        ) : null);

        // Limitação física
        if (f.Random.Bool(0.08f))
            reg.SetPhysicalLimitationDetails(f.PickRandom(
                "Dificuldade para caminhar longas distâncias",
                "Problemas na coluna lombar",
                "Artrose nos joelhos",
                "Mobilidade reduzida no braço direito"
            ));

        // Cirurgia recente
        if (f.Random.Bool(0.05f))
            reg.SetRecentSurgeryOrProcedureDetails(f.PickRandom(
                "Apendicectomia realizada há 3 meses",
                "Cirurgia de hérnia inguinal há 6 meses",
                "Procedimento de catarata há 2 meses",
                "Cirurgia ortopédica no joelho há 4 meses"
            ));

        // Gravidez (só mulheres)
        if (gender == Gender.Female && f.Random.Bool(0.06f))
        {
            reg.SetPregnancy(f.PickRandom(
                PregnancyStatus.Weeks0To12,
                PregnancyStatus.Weeks13To24
            ));
        }

        // RahaminVida completado
        if (f.Random.Bool(0.15f))
        {
            reg.SetRahaminVidaCompleted(f.PickRandom(
                RahaminVidaEdition.VidaX_2023_02_Cacador,
                RahaminVidaEdition.VidaXI_2023_10_Cacador,
                RahaminVidaEdition.VidaXII_2024_02_Cacador,
                RahaminVidaEdition.VidaXIII_2024_11_Cacador
            ));
        }
    }

    private static string GenerateValidCPF(Faker f)
    {
        return f.Random.ReplaceNumbers("###########");
    }

    private static void EnsureUnique(Registration reg, HashSet<string> usedEmails, HashSet<string> usedCpfs)
    {
        var baseEmail = reg.Email.Value;
        var counter = 1;
        while (usedEmails.Contains(reg.Email.Value))
        {
            var parts = baseEmail.Split('@');
            var newEmail = $"{parts[0]}{counter}@{parts[1]}";
            typeof(Registration)
                .GetProperty("Email", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)!
                .SetValue(reg, new EmailAddress(newEmail));
            counter++;
        }
        usedEmails.Add(reg.Email.Value);

        var baseCpf = reg.Cpf.Value;
        counter = 1;
        while (usedCpfs.Contains(reg.Cpf.Value))
        {
            var newCpf = baseCpf.Substring(0, 9) + counter.ToString("D2");
            typeof(Registration)
                .GetProperty("Cpf", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)!
                .SetValue(reg, new CPF(newCpf));
            counter++;
        }
        usedCpfs.Add(reg.Cpf.Value);
    }
}
