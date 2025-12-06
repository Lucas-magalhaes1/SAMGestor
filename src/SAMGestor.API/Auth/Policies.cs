namespace SAMGestor.API.Auth;

public static class Policies
{
    public const string Authenticated = "Authenticated";        // Qualquer usuário autenticado
    public const string ReadOnly = "ReadOnly";                  // Consultant + Manager + Admin
    public const string ManagerOrAbove = "ManagerOrAbove";     // Manager + Admin
    public const string AdminOnly = "AdminOnly";                // Só Admin
    public const string EmailConfirmed = "EmailConfirmed";      // E-mail confirmado
}