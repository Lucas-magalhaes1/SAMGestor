namespace SAMGestor.Application.Common.Auth;

public sealed class EmailOptions
{
    public const string SectionName = "Email";
    public string BaseUrl { get; set; } = string.Empty;     // ex.: http://localhost:7071/
    public string SendPath { get; set; } = "email/send";    // endpoint do microserviço fake
}