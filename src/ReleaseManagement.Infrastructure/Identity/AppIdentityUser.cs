using Microsoft.AspNetCore.Identity;

namespace ReleaseManagement.Infrastructure.Identity;

public sealed class AppIdentityUser : IdentityUser<Guid>
{
    public string? ExternalId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string? Department { get; set; }

    public string? Position { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; }

    public DateTime UpdatedDate { get; set; }
}
