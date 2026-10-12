using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Web.Models;

public class Opportunity : IValidatableObject
{
    public int OpportunityId { get; set; }

    [Required, StringLength(140)]
    public string OpportunityName { get; set; } = string.Empty;

    [Required]
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int? LeadId { get; set; }
    public Lead? Lead { get; set; }

    [Range(typeof(decimal), "0", "999999999", ErrorMessage = "Opportunity Amount must be greater than 0.")]
    public decimal Amount { get; set; }

    [Required, StringLength(30)]
    public string Stage { get; set; } = OpportunityStages.Qualification;

    [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
    public int Probability { get; set; } = 10;

    [DataType(DataType.Date)]
    public DateTime ExpectedCloseDate { get; set; } = DateTime.Today.AddDays(30);

    [Required, StringLength(24)]
    public string Status { get; set; } = OpportunityStatuses.Open;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [Required, StringLength(450)]
    public string AssignedToId { get; set; } = string.Empty;
    public ApplicationUser? AssignedTo { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    public decimal WeightedPipeline => Amount * Probability / 100m;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Status == OpportunityStatuses.Open && Amount <= 0)
        {
            yield return new ValidationResult("Opportunity Amount must be greater than 0.", [nameof(Amount)]);
        }

        if (Status == OpportunityStatuses.Open && ExpectedCloseDate.Date < DateTime.Today)
        {
            yield return new ValidationResult("Expected Close Date cannot be in the past.", [nameof(ExpectedCloseDate)]);
        }
    }
}
