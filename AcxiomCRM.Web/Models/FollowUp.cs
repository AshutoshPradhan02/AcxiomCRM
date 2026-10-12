using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Web.Models;

public class FollowUp : IValidatableObject
{
    public int FollowUpId { get; set; }

    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int? LeadId { get; set; }
    public Lead? Lead { get; set; }

    public int? OpportunityId { get; set; }
    public Opportunity? Opportunity { get; set; }

    [DataType(DataType.DateTime)]
    public DateTime FollowUpDate { get; set; } = DateTime.Today.AddDays(1).AddHours(9);

    [Required, StringLength(140)]
    public string Subject { get; set; } = string.Empty;

    [Required, StringLength(40)]
    public string FollowUpType { get; set; } = "Call";

    [StringLength(1000)]
    public string? Remarks { get; set; }

    [Required, StringLength(24)]
    public string Status { get; set; } = FollowUpStatuses.Planned;

    [Required, StringLength(450)]
    public string AssignedToId { get; set; } = string.Empty;
    public ApplicationUser? AssignedTo { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Status == FollowUpStatuses.Planned && FollowUpDate.Date < DateTime.Today)
        {
            yield return new ValidationResult("Follow-up date cannot be earlier than today.", [nameof(FollowUpDate)]);
        }

        if (CustomerId is null && LeadId is null && OpportunityId is null)
        {
            yield return new ValidationResult("Select a customer, lead, or opportunity for this follow-up.");
        }
    }
}
