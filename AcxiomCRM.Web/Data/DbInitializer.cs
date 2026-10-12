using AcxiomCRM.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Web.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.EnsureCreatedAsync();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { CrmRoles.Admin, CrmRoles.Manager, CrmRoles.SalesExecutive })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var adminEmail = configuration["AdminBootstrap:Email"];
        var adminPassword = configuration["AdminBootstrap:Password"];
        if (environment.IsDevelopment() && !string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword))
        {
            var admin = await userManager.FindByEmailAsync(adminEmail);
            if (admin is null)
            {
                admin = new ApplicationUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true, FullName = "Development Admin" };
                var result = await userManager.CreateAsync(admin, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, CrmRoles.Admin);
                }
            }
        }

        var manager = await EnsureUserAsync(userManager, "manager@acxiomcrm.local", "Manager User", CrmRoles.Manager, "Manager#12345");
        var sales = await EnsureUserAsync(userManager, "sales@acxiomcrm.local", "Sales Executive", CrmRoles.SalesExecutive, "Sales#12345", manager.Id);

        if (!await db.Customers.AnyAsync())
        {
            var customers = new[]
            {
                new Customer { CustomerCode = "CUS-1001", CustomerName = "Asha Kapoor", Email = "asha.kapoor@example.com", Phone = "+91 9876543210", CompanyName = "Northstar Retail", City = "Bengaluru", State = "Karnataka", Status = "Active", AssignedToId = sales.Id, CreatedBy = manager.Id },
                new Customer { CustomerCode = "CUS-1002", CustomerName = "Rahul Mehta", Email = "rahul.mehta@example.com", Phone = "+91 9876543211", CompanyName = "Vertex Finance", City = "Mumbai", State = "Maharashtra", Status = "Active", AssignedToId = sales.Id, CreatedBy = manager.Id },
                new Customer { CustomerCode = "CUS-1003", CustomerName = "Priya Sharma", Email = "priya.sharma@example.com", Phone = "+91 9876543212", CompanyName = "Helio Manufacturing", City = "Pune", State = "Maharashtra", Status = "Inactive", AssignedToId = sales.Id, CreatedBy = manager.Id }
            };
            db.Customers.AddRange(customers);
            await db.SaveChangesAsync();

            db.Leads.AddRange(
                new Lead { LeadCode = "LED-1001", LeadName = "Cloud Analytics Pilot", Email = "pilot@example.com", Phone = "+91 9123456780", CompanyName = "BluePeak Systems", Source = "Website", ExpectedValue = 75000, Status = LeadStatuses.New, Priority = "High", AssignedToId = sales.Id },
                new Lead { LeadCode = "LED-1002", LeadName = "Retail Loyalty Upgrade", Email = "loyalty@example.com", Phone = "+91 9123456781", CompanyName = "MarketWorks", Source = "Referral", ExpectedValue = 125000, Status = LeadStatuses.Qualified, Priority = "Medium", AssignedToId = sales.Id },
                new Lead { LeadCode = "LED-1003", LeadName = "Dormant Prospect", Email = "lost@example.com", Phone = "+91 9123456782", CompanyName = "Old Bridge", Source = "Event", ExpectedValue = 20000, Status = LeadStatuses.Lost, Priority = "Low", AssignedToId = sales.Id });
            await db.SaveChangesAsync();

            db.Opportunities.AddRange(
                new Opportunity { OpportunityName = "Northstar CRM Expansion", CustomerId = customers[0].CustomerId, Amount = 250000, Stage = OpportunityStages.Proposal, Probability = 55, ExpectedCloseDate = DateTime.Today.AddDays(25), Status = OpportunityStatuses.Open, AssignedToId = sales.Id },
                new Opportunity { OpportunityName = "Vertex Migration", CustomerId = customers[1].CustomerId, Amount = 180000, Stage = OpportunityStages.Won, Probability = 100, ExpectedCloseDate = DateTime.Today.AddDays(-7), Status = OpportunityStatuses.Won, AssignedToId = sales.Id },
                new Opportunity { OpportunityName = "Helio Reactivation", CustomerId = customers[2].CustomerId, Amount = 90000, Stage = OpportunityStages.Lost, Probability = 0, ExpectedCloseDate = DateTime.Today.AddDays(-15), Status = OpportunityStatuses.Lost, AssignedToId = sales.Id });
            await db.SaveChangesAsync();

            db.FollowUps.AddRange(
                new FollowUp { CustomerId = customers[0].CustomerId, Subject = "Proposal review", FollowUpType = "Meeting", FollowUpDate = DateTime.Today.AddDays(2).AddHours(11), Status = FollowUpStatuses.Planned, AssignedToId = sales.Id, Remarks = "Review commercial terms." },
                new FollowUp { CustomerId = customers[1].CustomerId, Subject = "Implementation check-in", FollowUpType = "Call", FollowUpDate = DateTime.Today.AddDays(-3).AddHours(10), Status = FollowUpStatuses.Completed, AssignedToId = sales.Id, Remarks = "Completed migration check-in." });
            db.Activities.AddRange(
                new CrmActivity { CustomerId = customers[0].CustomerId, ActivityType = "Email", Subject = "Sent proposal", Description = "Shared revised proposal.", ActivityDate = DateTime.UtcNow.AddDays(-1), AssignedToId = sales.Id, Status = ActivityStatuses.Completed },
                new CrmActivity { LeadId = 1, ActivityType = "Call", Subject = "Discovery call", Description = "Initial qualification.", ActivityDate = DateTime.UtcNow, AssignedToId = sales.Id, Status = ActivityStatuses.Open });
            await db.SaveChangesAsync();
        }
    }

    private static async Task<ApplicationUser> EnsureUserAsync(UserManager<ApplicationUser> userManager, string email, string fullName, string role, string password, string? managerId = null)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true, FullName = fullName, ManagerId = managerId };
            await userManager.CreateAsync(user, password);
            await userManager.AddToRoleAsync(user, role);
        }
        return user;
    }
}
