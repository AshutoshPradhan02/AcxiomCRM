using AcxiomCRM.Web.Data;
using AcxiomCRM.Web.Models;
using AcxiomCRM.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Web.Controllers;

[Authorize]
public class LeadsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IScopeService _scope;
    private readonly IAuditService _audit;
    private readonly UserManager<ApplicationUser> _userManager;

    public LeadsController(ApplicationDbContext db, IScopeService scope, IAuditService audit, UserManager<ApplicationUser> userManager)
    {
        _db = db; _scope = scope; _audit = audit; _userManager = userManager;
    }

    public async Task<IActionResult> Index(string? search, string? status)
    {
        var query = _scope.Leads(User);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(l => l.LeadName.Contains(search) || (l.CompanyName != null && l.CompanyName.Contains(search)));
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(l => l.Status == status);
        ViewBag.Search = search; ViewBag.Status = status; ViewBag.Statuses = LeadStatuses.All;
        return View(await query.OrderByDescending(l => l.CreatedDate).ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var lead = await _scope.Leads(User).FirstOrDefaultAsync(l => l.LeadId == id);
        return lead is null ? NotFound() : View(lead);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateAsync();
        return View(new Lead { LeadCode = $"LED-{await _db.Leads.CountAsync() + 1001}", AssignedToId = _userManager.GetUserId(User) ?? string.Empty });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Lead lead)
    {
        await ValidateLeadAsync(lead);
        if (!ModelState.IsValid) { await PopulateAsync(); return View(lead); }
        lead.CreatedDate = DateTime.UtcNow;
        _db.Leads.Add(lead);
        await _db.SaveChangesAsync();
        await _audit.RecordAsync("Create", "Lead", lead.LeadId.ToString(), newValue: lead.LeadName);
        TempData["Success"] = "Lead created.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var lead = await _scope.Leads(User).FirstOrDefaultAsync(l => l.LeadId == id);
        if (lead is null) return NotFound();
        await PopulateAsync();
        return View(lead);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Lead lead)
    {
        if (id != lead.LeadId) return NotFound();
        var existing = await _scope.Leads(User).AsNoTracking().FirstOrDefaultAsync(l => l.LeadId == id);
        if (existing is null) return NotFound();
        await ValidateLeadAsync(lead);
        if (!IsValidTransition(existing.Status, lead.Status)) ModelState.AddModelError(nameof(Lead.Status), "Invalid lead status transition.");
        if (!ModelState.IsValid) { await PopulateAsync(); return View(lead); }
        lead.CreatedDate = existing.CreatedDate;
        lead.ConvertedCustomerId = existing.ConvertedCustomerId;
        _db.Update(lead);
        await _db.SaveChangesAsync();
        await _audit.RecordAsync("Update", "Lead", id.ToString(), oldValue: existing.Status, newValue: lead.Status);
        TempData["Success"] = "Lead updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Convert(int id)
    {
        var lead = await _scope.Leads(User).FirstOrDefaultAsync(l => l.LeadId == id);
        if (lead is null) return NotFound();
        if (lead.Status != LeadStatuses.Qualified)
        {
            TempData["Warning"] = "Only qualified leads can be converted.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var customer = await _db.Customers.FirstOrDefaultAsync(c => (lead.Email != null && c.Email == lead.Email) || (lead.Phone != null && c.Phone == lead.Phone));
        if (customer is null)
        {
            customer = new Customer
            {
                CustomerCode = $"CUS-{await _db.Customers.CountAsync() + 1001}",
                CustomerName = lead.LeadName,
                Email = lead.Email ?? $"lead{id}@example.local",
                Phone = lead.Phone ?? $"+91 900000{id:0000}",
                CompanyName = lead.CompanyName,
                Status = "Active",
                AssignedToId = lead.AssignedToId,
                CreatedBy = _userManager.GetUserId(User)
            };
            _db.Customers.Add(customer);
            await _db.SaveChangesAsync();
        }

        if (!await _db.Opportunities.AnyAsync(o => o.LeadId == lead.LeadId))
        {
            _db.Opportunities.Add(new Opportunity
            {
                OpportunityName = $"{lead.LeadName} Opportunity",
                CustomerId = customer.CustomerId,
                LeadId = lead.LeadId,
                Amount = Math.Max(lead.ExpectedValue, 1),
                Stage = OpportunityStages.Qualification,
                Probability = 25,
                ExpectedCloseDate = DateTime.Today.AddDays(30),
                Status = OpportunityStatuses.Open,
                AssignedToId = lead.AssignedToId
            });
        }
        lead.Status = LeadStatuses.Converted;
        lead.ConvertedCustomerId = customer.CustomerId;
        await _db.SaveChangesAsync();
        await _audit.RecordAsync("Convert", "Lead", lead.LeadId.ToString(), newValue: $"CustomerId={customer.CustomerId}");
        TempData["Success"] = "Lead converted without duplicate customer or opportunity creation.";
        return RedirectToAction("Details", "Customers", new { id = customer.CustomerId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var lead = await _scope.Leads(User).FirstOrDefaultAsync(l => l.LeadId == id);
        if (lead is null) return NotFound();
        lead.Status = LeadStatuses.Lost;
        await _db.SaveChangesAsync();
        await _audit.RecordAsync("Deactivate", "Lead", id.ToString(), newValue: LeadStatuses.Lost);
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateLeadAsync(Lead lead)
    {
        if (!LeadStatuses.All.Contains(lead.Status)) ModelState.AddModelError(nameof(Lead.Status), "Select a valid lead status.");
        if (!await _scope.CanAccessOwnerAsync(User, lead.AssignedToId)) ModelState.AddModelError(nameof(Lead.AssignedToId), "You cannot assign records to that user.");
    }

    private static bool IsValidTransition(string oldStatus, string newStatus) =>
        oldStatus == newStatus || oldStatus != LeadStatuses.Converted;

    private async Task PopulateAsync()
    {
        var users = await _db.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
        if (!User.IsInRole(CrmRoles.Admin) && !User.IsInRole(CrmRoles.Manager))
        {
            var id = _userManager.GetUserId(User);
            users = users.Where(u => u.Id == id).ToList();
        }
        ViewBag.Owners = new SelectList(users, "Id", "FullName");
        ViewBag.Statuses = new SelectList(LeadStatuses.All);
    }
}
