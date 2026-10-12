using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Web.Models;

public class Lead
{
    public int LeadId { get; set; }

    [Required, StringLength(24)]
    public string LeadCode { get; set; } = string.Empty;

    [Required, StringLength(120)]
    public string LeadName { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Enter a valid email address."), StringLength(180)]
    public string? Email { get; set; }

    [CrmPhone, StringLength(30)]
    public string? Phone { get; set; }

    [StringLength(160)]
    public string? CompanyName { get; set; }

    [Required, StringLength(80)]
    public string Source { get; set; } = "Website";

    [Range(0, 999999999)]
    public decimal ExpectedValue { get; set; }

    [Required, StringLength(24)]
    public string Status { get; set; } = LeadStatuses.New;

    [Required, StringLength(24)]
    public string Priority { get; set; } = "Medium";

    [Required, StringLength(450)]
    public string AssignedToId { get; set; } = string.Empty;

    public ApplicationUser? AssignedTo { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [StringLength(1000)]
    public string? Notes { get; set; }

    public int? ConvertedCustomerId { get; set; }
    public Customer? ConvertedCustomer { get; set; }
}
