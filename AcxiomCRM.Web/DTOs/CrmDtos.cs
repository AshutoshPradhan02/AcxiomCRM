using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Web.DTOs;

public record CustomerDto(int CustomerId, string CustomerCode, string CustomerName, string Email, string Phone, string? CompanyName, string Status, string AssignedToId);

public class CustomerRequest
{
    [Required, StringLength(120)] public string CustomerName { get; set; } = string.Empty;
    [Required, EmailAddress(ErrorMessage = "Enter a valid email address."), StringLength(180)] public string Email { get; set; } = string.Empty;
    [Required, RegularExpression(@"^[0-9+\-\s()]{7,20}$", ErrorMessage = "Enter a valid phone number.")] public string Phone { get; set; } = string.Empty;
    [StringLength(160)] public string? CompanyName { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string Status { get; set; } = "Active";
    [Required] public string AssignedToId { get; set; } = string.Empty;
}

public record LeadDto(int LeadId, string LeadCode, string LeadName, string? Email, string? Phone, string? CompanyName, string Source, decimal ExpectedValue, string Status, string AssignedToId);

public class LeadRequest
{
    [Required, StringLength(120)] public string LeadName { get; set; } = string.Empty;
    [EmailAddress(ErrorMessage = "Enter a valid email address.")] public string? Email { get; set; }
    [RegularExpression(@"^[0-9+\-\s()]{7,20}$", ErrorMessage = "Enter a valid phone number.")] public string? Phone { get; set; }
    public string? CompanyName { get; set; }
    [Required] public string Source { get; set; } = "API";
    [Range(0, 999999999)] public decimal ExpectedValue { get; set; }
    [Required] public string Status { get; set; } = "New";
    [Required] public string AssignedToId { get; set; } = string.Empty;
}

public record OpportunityDto(int OpportunityId, string OpportunityName, int CustomerId, decimal Amount, string Stage, int Probability, DateTime ExpectedCloseDate, string Status, string AssignedToId, decimal WeightedPipeline);

public class OpportunityRequest
{
    [Required, StringLength(140)] public string OpportunityName { get; set; } = string.Empty;
    [Required] public int CustomerId { get; set; }
    [Range(typeof(decimal), "0", "999999999", ErrorMessage = "Opportunity Amount must be greater than 0.")] public decimal Amount { get; set; }
    [Required] public string Stage { get; set; } = "Qualification";
    [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")] public int Probability { get; set; } = 10;
    public DateTime ExpectedCloseDate { get; set; } = DateTime.Today.AddDays(30);
    public string Status { get; set; } = "Open";
    [Required] public string AssignedToId { get; set; } = string.Empty;
}
