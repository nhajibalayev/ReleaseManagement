using System.ComponentModel.DataAnnotations;

namespace ReleaseHub.Web.Models;

public sealed class CreateReleaseTaskFormModel
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Scope")]
    public string Description { get; set; } = string.Empty;
}
