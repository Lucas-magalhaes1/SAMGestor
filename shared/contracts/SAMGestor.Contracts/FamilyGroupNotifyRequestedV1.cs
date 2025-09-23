namespace SAMGestor.Contracts;

public sealed record FamilyGroupNotifyRequestedV1(
    Guid RetreatId,
    Guid FamilyId,
    string GroupLink,
    IReadOnlyList<FamilyGroupNotifyRequestedV1.MemberContact> Members
)
{
    public sealed record MemberContact(Guid RegistrationId, string Name, string? Email, string? PhoneE164);
}