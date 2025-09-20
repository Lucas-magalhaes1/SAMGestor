namespace SAMGestor.Contracts;

public sealed record FamilyGroupCreateRequestedV1(
    Guid RetreatId,
    Guid FamilyId,
    string Channel,           // "email" | "whatsapp"
    bool  ForceRecreate,
    IReadOnlyList<FamilyGroupCreateRequestedV1.MemberContact> Members
)
{
    public sealed record MemberContact(Guid RegistrationId, string Name, string? Email, string? PhoneE164);
}