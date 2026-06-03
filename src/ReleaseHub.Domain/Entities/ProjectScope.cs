namespace ReleaseHub.Domain.Entities;

public class ProjectScope
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ReleaseTaskId { get; set; }
    public string ProjectKey { get; set; } = string.Empty;
    public string Repository { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string ChangeSummary { get; set; } = string.Empty;
}
