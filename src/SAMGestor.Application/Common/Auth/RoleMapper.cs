using SAMGestor.Domain.Enums;

namespace SAMGestor.Application.Common.Auth;

public static class RoleMapper
{
    public static string ToShortName(this UserRole role) => role switch
    {
        UserRole.Administrator => RoleNames.Admin,
        UserRole.Manager       => RoleNames.Manager,
        UserRole.Consultant    => RoleNames.Consultant,
        _ => RoleNames.Consultant
    };
}