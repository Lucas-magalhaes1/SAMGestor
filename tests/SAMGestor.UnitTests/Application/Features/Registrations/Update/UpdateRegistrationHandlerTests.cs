using FluentAssertions;
using Moq;
using SAMGestor.Application.Features.Registrations.Update;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.UnitTests.Application.Features.Registrations.Update;

public class UpdateRegistrationHandlerTests
{
    private static Registration ExistingRegistration(Guid retreatId)
    {
        var reg = new Registration(
            new FullName("João Silva"),
            new CPF("52998224725"),
            new EmailAddress("joao@test.com"),
            "11999999999",
            new DateOnly(2000, 1, 1),
            Gender.Male,
            "São Paulo",
            RegistrationStatus.NotSelected,
            retreatId
        );

        reg.SetMaritalStatus(MaritalStatus.Single);
        reg.SetPregnancy(PregnancyStatus.None);
        reg.SetShirtSize(ShirtSize.M);
        reg.SetAnthropometrics(70.0m, 175.0m);
        reg.SetProfession("Estudante");
        reg.SetAddress("Rua Velha, 100", "Bairro Antigo", UF.SP, "São Paulo");
        reg.SetWhatsapp("11988887777");
        reg.SetNeighborPhone("1133334444");
        reg.SetRelativePhone("11911112222");
        reg.SetFather(ParentStatus.Alive, "Pai João", "1133332222");
        reg.SetMother(ParentStatus.Alive, "Mãe Maria", "11911113333");
        reg.SetFamilyLoss(false, null);
        reg.SetSubmitterInfo(false, RelationshipDegree.None, null);
        reg.SetReligion("Católica");
        reg.SetPreviousUncalledApplications(RahaminAttempt.None);
        reg.SetRahaminVidaCompleted(RahaminVidaEdition.None);
        reg.SetAlcoholUse(AlcoholUsePattern.None);
        reg.SetSmoker(false);
        reg.SetDrugUse(false, null);
        reg.SetAllergies(false, null);
        reg.SetMedicalRestriction(false, null);
        reg.SetMedications(false, null);

        return reg;
    }

    private static UpdateRegistrationCommand UpdateCmd(
        Guid registrationId,
        CPF? cpf = null,
        string? photoKey = null,
        string? docKey = null)
    {
        return new UpdateRegistrationCommand(
            registrationId,
            new FullName("João Silva Atualizado"),
            cpf ?? new CPF("52998224725"),
            new EmailAddress("joao.novo@test.com"),
            "11988888888",
            new DateOnly(2000, 6, 15),
            Gender.Male,
            "Campinas",
            MaritalStatus.Married,
            PregnancyStatus.None,
            ShirtSize.G,
            85.0m,
            185.0m,
            "Desenvolvedor",
            "Rua Nova, 200",
            "Bairro Novo",
            UF.SP,
            "11977776666",
            "joao.fb",
            "joao.ig",
            "1144445555",
            "11922223333",
            ParentStatus.Alive,
            "Pai José",
            "1144443333",
            ParentStatus.Deceased,
            null,
            null,
            true,
            "Perda recente",
            true,
            RelationshipDegree.Friend,
            "Maria Amiga",
            "Evangélica",
            RahaminAttempt.RahaminPortaI_2015_EUA,
            RahaminVidaEdition.VidaI_2016_03_Cacador,
            AlcoholUsePattern.Weekends,
            true,
            false,
            null,
            true,
            "Lactose",
            false,
            null,
            true,
            "Vitamina D",
            "Problema no joelho",
            null,
            photoKey,
            photoKey != null ? "image/jpeg" : null,
            photoKey != null ? 1024L : null,
            photoKey != null ? "https://storage.com/photo.jpg" : null,
            docKey != null ? IdDocumentType.RG : null,
            docKey != null ? "123456789" : null,
            docKey,
            docKey != null ? "application/pdf" : null,
            docKey != null ? 2048L : null,
            docKey != null ? "https://storage.com/doc.pdf" : null
        );
    }

    [Fact]
    public async Task Success_atualiza_todos_dados()
    {
        var retreatId = Guid.NewGuid();
        var reg = ExistingRegistration(retreatId);

        var regRepo = new Mock<IRegistrationRepository>();
        regRepo.Setup(r => r.GetByIdForUpdateAsync(reg.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(reg);
        regRepo.Setup(r => r.UpdateAsync(It.IsAny<Registration>(), It.IsAny<CancellationToken>()))
               .Returns(Task.CompletedTask);

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
           .Returns(Task.CompletedTask);

        var handler = new UpdateRegistrationHandler(regRepo.Object, uow.Object, Mock.Of<IStorageService>());

        var res = await handler.Handle(UpdateCmd(reg.Id), default);

        res.RegistrationId.Should().Be(reg.Id);
        reg.Name.Value.Should().Be("João Silva Atualizado");
        reg.Email.Value.Should().Be("joao.novo@test.com");
        reg.Phone.Should().Be("11988888888");
        reg.City.Should().Be("Campinas");
        reg.MaritalStatus.Should().Be(MaritalStatus.Married);
        reg.ShirtSize.Should().Be(ShirtSize.G);
        reg.WeightKg.Should().Be(85.0m);
        reg.HeightCm.Should().Be(185.0m);
        reg.Profession.Should().Be("Desenvolvedor");
        reg.Religion.Should().Be("Evangélica");
        reg.Smoker.Should().BeTrue();
        reg.HasAllergies.Should().BeTrue();

        regRepo.Verify(r => r.UpdateAsync(reg, It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Success_atualiza_com_foto()
    {
        var retreatId = Guid.NewGuid();
        var reg = ExistingRegistration(retreatId);

        var regRepo = new Mock<IRegistrationRepository>();
        regRepo.Setup(r => r.GetByIdForUpdateAsync(reg.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(reg);
        regRepo.Setup(r => r.UpdateAsync(It.IsAny<Registration>(), It.IsAny<CancellationToken>()))
               .Returns(Task.CompletedTask);

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
           .Returns(Task.CompletedTask);

        var handler = new UpdateRegistrationHandler(regRepo.Object, uow.Object, Mock.Of<IStorageService>());

        var res = await handler.Handle(
            UpdateCmd(reg.Id, photoKey: "photos/test.jpg"), 
            default);

        res.RegistrationId.Should().Be(reg.Id);
        res.PhotoUrl.Should().Be("https://storage.com/photo.jpg");
        reg.PhotoStorageKey.Should().Be("photos/test.jpg");
        reg.PhotoContentType.Should().Be("image/jpeg");

        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Success_atualiza_com_documento()
    {
        var retreatId = Guid.NewGuid();
        var reg = ExistingRegistration(retreatId);

        var regRepo = new Mock<IRegistrationRepository>();
        regRepo.Setup(r => r.GetByIdForUpdateAsync(reg.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(reg);
        regRepo.Setup(r => r.UpdateAsync(It.IsAny<Registration>(), It.IsAny<CancellationToken>()))
               .Returns(Task.CompletedTask);

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
           .Returns(Task.CompletedTask);

        var handler = new UpdateRegistrationHandler(regRepo.Object, uow.Object, Mock.Of<IStorageService>());

        var res = await handler.Handle(
            UpdateCmd(reg.Id, docKey: "docs/test.pdf"), 
            default);

        res.RegistrationId.Should().Be(reg.Id);
        res.DocumentUrl.Should().Be("https://storage.com/doc.pdf");
        reg.IdDocumentStorageKey.Should().Be("docs/test.pdf");
        reg.IdDocumentType.Should().Be(IdDocumentType.RG);

        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Success_atualiza_com_foto_e_documento()
    {
        var retreatId = Guid.NewGuid();
        var reg = ExistingRegistration(retreatId);

        var regRepo = new Mock<IRegistrationRepository>();
        regRepo.Setup(r => r.GetByIdForUpdateAsync(reg.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(reg);
        regRepo.Setup(r => r.UpdateAsync(It.IsAny<Registration>(), It.IsAny<CancellationToken>()))
               .Returns(Task.CompletedTask);

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
           .Returns(Task.CompletedTask);

        var handler = new UpdateRegistrationHandler(regRepo.Object, uow.Object, Mock.Of<IStorageService>());

        var res = await handler.Handle(
            UpdateCmd(reg.Id, photoKey: "photos/test.jpg", docKey: "docs/test.pdf"), 
            default);

        res.RegistrationId.Should().Be(reg.Id);
        res.PhotoUrl.Should().Be("https://storage.com/photo.jpg");
        res.DocumentUrl.Should().Be("https://storage.com/doc.pdf");

        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Falha_quando_registro_nao_existe()
    {
        var regRepo = new Mock<IRegistrationRepository>();
        regRepo.Setup(r => r.GetByIdForUpdateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync((Registration?)null);

        var handler = new UpdateRegistrationHandler(
            regRepo.Object,
            new Mock<IUnitOfWork>().Object, 
            Mock.Of<IStorageService>());

        var fakeId = Guid.NewGuid();
        await FluentActions.Invoking(() => handler.Handle(UpdateCmd(fakeId), default))
            .Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*{nameof(Registration)}*{fakeId}*");
    }

    [Fact]
    public async Task Falha_quando_muda_cpf_para_bloqueado()
    {
        var retreatId = Guid.NewGuid();
        var reg = ExistingRegistration(retreatId);
        var newCpf = new CPF("11122233344");

        var regRepo = new Mock<IRegistrationRepository>();
        regRepo.Setup(r => r.GetByIdForUpdateAsync(reg.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(reg);
        regRepo.Setup(r => r.IsCpfBlockedAsync(newCpf, It.IsAny<CancellationToken>()))
               .ReturnsAsync(true);

        var handler = new UpdateRegistrationHandler(
            regRepo.Object,
            new Mock<IUnitOfWork>().Object, 
            Mock.Of<IStorageService>());

        await FluentActions.Invoking(() => handler.Handle(UpdateCmd(reg.Id, cpf: newCpf), default))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*CPF is blocked*");
    }

    [Fact]
    public async Task Falha_quando_muda_cpf_ja_registrado_no_retiro()
    {
        var retreatId = Guid.NewGuid();
        var reg = ExistingRegistration(retreatId);
        var newCpf = new CPF("11122233344");

        var regRepo = new Mock<IRegistrationRepository>();
        regRepo.Setup(r => r.GetByIdForUpdateAsync(reg.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(reg);
        regRepo.Setup(r => r.IsCpfBlockedAsync(newCpf, It.IsAny<CancellationToken>()))
               .ReturnsAsync(false);
        regRepo.Setup(r => r.ExistsByCpfInRetreatAsync(newCpf, retreatId, It.IsAny<CancellationToken>()))
               .ReturnsAsync(true);

        var handler = new UpdateRegistrationHandler(
            regRepo.Object,
            new Mock<IUnitOfWork>().Object, 
            Mock.Of<IStorageService>());

        await FluentActions.Invoking(() => handler.Handle(UpdateCmd(reg.Id, cpf: newCpf), default))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*CPF already registered for this retreat*");
    }

    [Fact]
    public async Task Success_muda_cpf_quando_nao_bloqueado_nem_duplicado()
    {
        var retreatId = Guid.NewGuid();
        var reg = ExistingRegistration(retreatId);
        var newCpf = new CPF("11122233344");

        var regRepo = new Mock<IRegistrationRepository>();
        regRepo.Setup(r => r.GetByIdForUpdateAsync(reg.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(reg);
        regRepo.Setup(r => r.IsCpfBlockedAsync(newCpf, It.IsAny<CancellationToken>()))
               .ReturnsAsync(false);
        regRepo.Setup(r => r.ExistsByCpfInRetreatAsync(newCpf, retreatId, It.IsAny<CancellationToken>()))
               .ReturnsAsync(false);
        regRepo.Setup(r => r.UpdateAsync(It.IsAny<Registration>(), It.IsAny<CancellationToken>()))
               .Returns(Task.CompletedTask);

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
           .Returns(Task.CompletedTask);

        var handler = new UpdateRegistrationHandler(regRepo.Object, uow.Object, Mock.Of<IStorageService>());

        var res = await handler.Handle(UpdateCmd(reg.Id, cpf: newCpf), default);

        res.RegistrationId.Should().Be(reg.Id);
        reg.Cpf.Should().Be(newCpf);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
