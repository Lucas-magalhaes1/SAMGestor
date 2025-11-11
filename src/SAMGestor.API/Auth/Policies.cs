namespace SAMGestor.API.Auth;

public static class Policies
{
    public const string ReadOnly = "ReadOnly";
    public const string ManageAllButDeleteUsers = "ManageAllButDeleteUsers";
    public const string AdminOnly = "AdminOnly";
    public const string EmailConfirmed = "EmailConfirmed";
}