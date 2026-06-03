using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReleaseHub.Application.Abstractions.Email;
using ReleaseHub.Application.Options;

namespace ReleaseHub.Infrastructure.Email;

public sealed class HttpEmailClient : IEmailClient
{
    public const string HttpClientName = "EmailService";
    private readonly HttpClient _http;
    private readonly EmailServiceOptions _options;
    private readonly ILogger<HttpEmailClient> _log;

    public HttpEmailClient(HttpClient http, IOptions<EmailServiceOptions> options, ILogger<HttpEmailClient> log)
    {
        _http = http;
        _options = options.Value;
        _log = log;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        var body = new
        {
            from = _options.FromAddress,
            to = message.To,
            cc = message.Cc,
            subject = message.Subject,
            body = message.Body,
            isHtml = message.IsHtml
        };

        using var resp = await _http.PostAsJsonAsync(_options.SendPath, body, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var detail = await resp.Content.ReadAsStringAsync(ct);
            _log.LogError("Email service returned {Status}: {Detail}", resp.StatusCode, detail);
            throw new InvalidOperationException($"Email service returned {(int)resp.StatusCode}.");
        }
    }
}
