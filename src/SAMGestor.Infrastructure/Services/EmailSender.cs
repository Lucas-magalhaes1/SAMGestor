using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using SAMGestor.Application.Common.Auth;
using SAMGestor.Application.Interfaces.Auth;

namespace SAMGestor.Infrastructure.Services;

public sealed class EmailSender : IEmailSender
{
    private readonly HttpClient _http;
    private readonly EmailOptions _opt;

    public EmailSender(HttpClient http, IOptions<EmailOptions> options)
    {
        _http = http;
        _opt = options.Value;

        if (!string.IsNullOrWhiteSpace(_opt.BaseUrl))
        {
            _http.BaseAddress = new Uri(_opt.BaseUrl, UriKind.Absolute);
        }
    }

    public async Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        var payload = new
        {
            to = message.To,
            subject = message.Subject,
            templateKey = message.TemplateKey,
            variables = message.Variables
        };

        var resp = await _http.PostAsJsonAsync(_opt.SendPath, payload, ct);
        resp.EnsureSuccessStatusCode();
    }
}