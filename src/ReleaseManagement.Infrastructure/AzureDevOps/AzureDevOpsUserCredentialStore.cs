using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;

namespace ReleaseManagement.Infrastructure.AzureDevOps;

public interface IAzureDevOpsUserCredentialStore
{
    void Save(AzureDevOpsUserCredential credential);

    AzureDevOpsUserCredential? Get();

    void Clear();
}

public sealed class SessionAzureDevOpsUserCredentialStore : IAzureDevOpsUserCredentialStore
{
    private const string SessionKey = "ado.user.credential";
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IDataProtector _protector;

    public SessionAzureDevOpsUserCredentialStore(
        IHttpContextAccessor httpContextAccessor,
        IDataProtectionProvider dataProtectionProvider)
    {
        _httpContextAccessor = httpContextAccessor;
        _protector = dataProtectionProvider.CreateProtector("ReleaseManagement.AzureDevOps.UserCredential.v1");
    }

    public void Save(AzureDevOpsUserCredential credential)
    {
        var session = GetSession();
        var json = JsonSerializer.Serialize(credential);
        session.SetString(SessionKey, _protector.Protect(json));
    }

    public AzureDevOpsUserCredential? Get()
    {
        var session = _httpContextAccessor.HttpContext?.Session;
        if (session is null)
        {
            return null;
        }

        var protectedValue = session.GetString(SessionKey);
        if (string.IsNullOrWhiteSpace(protectedValue))
        {
            return null;
        }

        try
        {
            var json = _protector.Unprotect(protectedValue);
            return JsonSerializer.Deserialize<AzureDevOpsUserCredential>(json);
        }
        catch
        {
            session.Remove(SessionKey);
            return null;
        }
    }

    public void Clear()
    {
        _httpContextAccessor.HttpContext?.Session?.Remove(SessionKey);
    }

    private ISession GetSession()
    {
        var context = _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HTTP context is required to store Azure DevOps credentials.");

        return context.Session;
    }
}
