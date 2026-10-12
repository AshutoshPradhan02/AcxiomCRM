using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Web.Models;

public class Customer
{
    public int CustomerId { get; set; }

    [Required, StringLength(24)]
    public string CustomerCode { get; set; } = string.Empty;

    [Required, StringLength(120)]
    public string CustomerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required."), EmailAddress(ErrorMessage = "Enter a valid email address."), StringLength(180)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone is required."), CrmPhone, StringLength(30)]
    public string Phone { get; set; } = string.Empty;

    [StringLength(160)]
    public string? CompanyName { get; set; }

    [StringLength(240)]
    public string? Address { get; set; }

    [StringLength(80)]
    public string? City { get; set; }

    [StringLength(80)]
    public string? State { get; set; }

    [Required, StringLength(24)]
    public string Status { get; set; } = "Active";

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [StringLength(450)]
    public string? CreatedBy { get; set; }

    [Required, StringLength(450)]
    public string AssignedToId { get; set; } = string.Empty;

    public ApplicationUser? AssignedTo { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }
}
