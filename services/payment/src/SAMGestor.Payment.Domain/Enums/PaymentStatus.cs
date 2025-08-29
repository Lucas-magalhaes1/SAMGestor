namespace SAMGestor.Payment.Domain.Enums;

public enum PaymentStatus
{
    Pending = 1,      
    LinkCreated = 2,  
    Paid = 3,         
    Failed = 4,
    Expired = 5
}