using System;
using Xunit;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Specifications;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.UnitTests.Domain.Specifications;

public class PaymentRecordedSpecificationTests
{
    private static Registration NewReg(RegistrationStatus status)
    {
        return new Registration(
            new FullName("Laura Silva"),
            new CPF("33322211100"),
            new EmailAddress("laura@mail.com"),
            "11994443322",
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25)),
            Gender.Female,
            "Sorocaba",
            status,
            ParticipationCategory.Guest,
            "Oeste",
            Guid.NewGuid());
    }

    [Fact]
    public void Should_Pass_When_Status_Is_PaymentConfirmed()
    {
        var reg = NewReg(RegistrationStatus.PaymentConfirmed);
        var spec = new PaymentRecordedSpecification();

        Assert.True(spec.IsSatisfiedBy(reg));
    }

    [Fact]
    public void Should_Fail_When_Status_Is_Still_Pending()
    {
        var reg = NewReg(RegistrationStatus.PendingPayment);
        var spec = new PaymentRecordedSpecification();

        Assert.False(spec.IsSatisfiedBy(reg));
    }
}