using System;
using Xunit;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Specifications;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.UnitTests.Domain.Specifications;

public class ConfirmedSetsPendingPaymentSpecificationTests
{
    private static Registration NewReg(RegistrationStatus status)
    {
        return new Registration(
            new FullName("Paulo Sousa"),
            new CPF("88877766655"),
            new EmailAddress("paulo@mail.com"),
            "11995554433",
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-24)),
            Gender.Male,
            "Campinas",
            status,
            ParticipationCategory.Guest,
            "Oeste",
            Guid.NewGuid());
    }

    [Fact]
    public void Should_Pass_When_Status_Is_PendingPayment_After_Confirm()
    {
        var reg = NewReg(RegistrationStatus.PendingPayment);
        var spec = new ConfirmedSetsPendingPaymentSpecification();

        Assert.True(spec.IsSatisfiedBy(reg));
    }

    [Fact]
    public void Should_Fail_When_Status_Not_PendingPayment_After_Confirm()
    {
        var reg = NewReg(RegistrationStatus.Confirmed); 
        var spec = new ConfirmedSetsPendingPaymentSpecification();

        Assert.False(spec.IsSatisfiedBy(reg));
    }
}