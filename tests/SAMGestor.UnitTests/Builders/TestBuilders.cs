using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.ValueObjects;

public static class TestBuilders
{
    public static Retreat NewOpenRetreat()
        => new Retreat(
            new FullName("Retiro X"), "ED1", "Tema",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(12)),
            100, 100,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
            new Money(0,"BRL"), new Money(0,"BRL"),
            new Percentage(50), new Percentage(50));

    public static Registration NewReg(Guid retreatId, string name, Gender g, RegistrationStatus st = RegistrationStatus.Confirmed)
        => new Registration(
            new FullName(name), new CPF("52998224725"), new EmailAddress($"{Guid.NewGuid()}@mail.com"),
            "11999999999", new DateOnly(1990,1,1), g, "SP", st, retreatId);

    public static Family NewFamily(Guid retreatId, string name = "Família 1", int capacity = 4)
        => new Family(new FamilyName(name), retreatId, capacity);

    public static FamilyMember Link(Guid retreatId, Guid familyId, Guid regId, int pos)
        => new FamilyMember(retreatId, familyId, regId, pos);
}