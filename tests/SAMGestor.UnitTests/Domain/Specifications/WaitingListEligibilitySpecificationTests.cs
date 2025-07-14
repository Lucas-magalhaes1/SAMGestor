using System;
using Moq;
using Xunit;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.Specifications;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.UnitTests.Domain.Specifications
{
    public class WaitingListEligibilitySpecificationTests
    {
        
        private static Registration CreateRegistration(RegistrationStatus status)
        {
            return new Registration(
                new FullName("Beatriz Souza"),
                new CPF("77766655544"),
                new EmailAddress("bia@mail.com"),
                "11991119999",
                DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-23)),
                Gender.Female,
                "Campinas",
                status,
                ParticipationCategory.Guest,
                "Oeste",
                Guid.NewGuid());
        }

        [Fact]
        public void Should_Return_True_When_Status_NotSelected_And_Not_In_WaitingList()
        {
           
            var reg = CreateRegistration(RegistrationStatus.NotSelected);

            var waitingRepo = new Mock<IWaitingListRepository>();
            waitingRepo.Setup(r => r.Exists(reg.Id, reg.RetreatId)).Returns(false);

            var spec = new WaitingListEligibilitySpecification(waitingRepo.Object);

           
            Assert.True(spec.IsSatisfiedBy(reg));
        }

        [Fact]
        public void Should_Return_False_When_Status_NotSelected_But_Already_In_WaitingList()
        {
            var reg = CreateRegistration(RegistrationStatus.NotSelected);

            var waitingRepo = new Mock<IWaitingListRepository>();
            waitingRepo.Setup(r => r.Exists(reg.Id, reg.RetreatId)).Returns(true);

            var spec = new WaitingListEligibilitySpecification(waitingRepo.Object);

            Assert.False(spec.IsSatisfiedBy(reg));
        }

        [Fact]
        public void Should_Return_False_When_Status_Is_Other_Than_NotSelected()
        {
            var reg = CreateRegistration(RegistrationStatus.Selected); 

            var waitingRepo = new Mock<IWaitingListRepository>();
            waitingRepo.Setup(r => r.Exists(reg.Id, reg.RetreatId)).Returns(false);

            var spec = new WaitingListEligibilitySpecification(waitingRepo.Object);

            Assert.False(spec.IsSatisfiedBy(reg));
        }
    }
}
