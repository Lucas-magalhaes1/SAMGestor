namespace SAMGestor.Domain.Enums;

public static class SlotPolicy
{
    public static readonly RegistrationStatus[] OccupyingStatuses = new[]
    {
        RegistrationStatus.Selected,
        RegistrationStatus.PendingPayment,
        RegistrationStatus.PaymentConfirmed,
        RegistrationStatus.Confirmed
    };

    public static bool OccupiesSlot(RegistrationStatus s)
        => OccupyingStatuses.Contains(s);
}