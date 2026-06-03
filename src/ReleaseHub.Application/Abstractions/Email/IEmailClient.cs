namespace ReleaseHub.Application.Abstractions.Email;

public interface IEmailClient
{
    Task SendAsync(EmailMessage message, CancellationToken ct = default);
}
