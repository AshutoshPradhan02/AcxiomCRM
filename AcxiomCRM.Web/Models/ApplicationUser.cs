using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Web.Models;

public class ApplicationUser : IdentityUser
{
    [StringLength(120)]
    public string FullName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public string? ManagerId { get; set; }

    public ApplicationUser? Manager { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
