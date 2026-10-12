using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Web.Models;

public class AuditLog
{
    public int AuditLogId { get; set; }

    [StringLength(450)]
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    [Required, StringLength(80)]
    public string Action { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string EntityName { get; set; } = string.Empty;

    [StringLength(80)]
    public string? RecordId { get; set; }

    [StringLength(2000)]
    public string? OldValue { get; set; }

    [StringLength(2000)]
    public string? NewValue { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [Required, StringLength(24)]
    public string Result { get; set; } = "Success";

    [StringLength(80)]
    public string? IpAddress { get; set; }

    [StringLength(1000)]
    public string? Metadata { get; set; }
}
