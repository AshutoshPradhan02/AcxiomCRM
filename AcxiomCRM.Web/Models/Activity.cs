using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Web.Models;

public class CrmActivity
{
    [Key]
    public int ActivityId { get; set; }

    [Required, StringLength(30)]
    public string ActivityType { get; set; } = "Call";

    [Required, StringLength(140)]
    public string Subject { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [DataType(DataType.DateTime)]
    public DateTime ActivityDate { get; set; } = DateTime.UtcNow;

    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int? LeadId { get; set; }
    public Lead? Lead { get; set; }

    [Required, StringLength(450)]
    public string AssignedToId { get; set; } = string.Empty;
    public ApplicationUser? AssignedTo { get; set; }

    [Required, StringLength(24)]
    public string Status { get; set; } = ActivityStatuses.Open;
}
