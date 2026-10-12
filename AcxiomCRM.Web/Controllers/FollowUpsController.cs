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
public class FollowUpsController : Controller
{
    private readonly ApplicationDbContext _db; private readonly IScopeService _scope; private readonly IAuditService _audit; private readonly UserManager<ApplicationUser> _userManager;
    public FollowUpsController(ApplicationDbContext db, IScopeService scope, IAuditService audit, UserManager<ApplicationUser> userManager) { _db = db; _scope = scope; _audit = audit; _userManager = userManager; }
    public async Task<IActionResult> Index(string? status)
    {
        var query = _scope.FollowUps(User);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(f => f.Status == status);
        ViewBag.Status = status;
        return View(await query.OrderBy(f => f.FollowUpDate).ToListAsync());
    }
    public async Task<IActionResult> Create() { await PopulateAsync(); return View(new FollowUp { AssignedToId = _userManager.GetUserId(User) ?? string.Empty }); }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FollowUp followUp)
    {
        await ValidateFollowUpAsync(followUp);
        if (!ModelState.IsValid) { await PopulateAsync(); return View(followUp); }
        _db.FollowUps.Add(followUp); await _db.SaveChangesAsync();
        await _audit.RecordAsync("Create", "FollowUp", followUp.FollowUpId.ToString(), newValue: followUp.Subject);
        return RedirectToAction(nameof(Index));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id)
    {
        var f = await _scope.FollowUps(User).FirstOrDefaultAsync(x => x.FollowUpId == id);
        if (f is null) return NotFound();
        f.Status = FollowUpStatuses.Completed; await _db.SaveChangesAsync();
        await _audit.RecordAsync("Complete", "FollowUp", id.ToString());
        return RedirectToAction(nameof(Index));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reschedule(int id, DateTime followUpDate)
    {
        var f = await _scope.FollowUps(User).FirstOrDefaultAsync(x => x.FollowUpId == id);
        if (f is null) return NotFound();
        if (followUpDate.Date < DateTime.Today) { TempData["Warning"] = "Follow-up date cannot be earlier than today."; return RedirectToAction(nameof(Index)); }
        var old = f.FollowUpDate.ToString("s"); f.FollowUpDate = followUpDate; f.Status = FollowUpStatuses.Planned; await _db.SaveChangesAsync();
        await _audit.RecordAsync("Reschedule", "FollowUp", id.ToString(), oldValue: old, newValue: followUpDate.ToString("s"));
        return RedirectToAction(nameof(Index));
    }
    private async Task ValidateFollowUpAsync(FollowUp followUp)
    {
        if (!FollowUpStatuses.All.Contains(followUp.Status)) ModelState.AddModelError(nameof(FollowUp.Status), "Select a valid status.");
        if (!await _scope.CanAccessOwnerAsync(User, followUp.AssignedToId)) ModelState.AddModelError(nameof(FollowUp.AssignedToId), "You cannot assign records to that user.");
    }
    private async Task PopulateAsync()
    {
        ViewBag.Customers = new SelectList(await _scope.Customers(User).OrderBy(c => c.CustomerName).ToListAsync(), "CustomerId", "CustomerName");
        ViewBag.Leads = new SelectList(await _scope.Leads(User).OrderBy(l => l.LeadName).ToListAsync(), "LeadId", "LeadName");
        ViewBag.Opportunities = new SelectList(await _scope.Opportunities(User).OrderBy(o => o.OpportunityName).ToListAsync(), "OpportunityId", "OpportunityName");
        var users = await _db.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
        if (!User.IsInRole(CrmRoles.Admin) && !User.IsInRole(CrmRoles.Manager)) users = users.Where(u => u.Id == _userManager.GetUserId(User)).ToList();
        ViewBag.Owners = new SelectList(users, "Id", "FullName");
        ViewBag.Statuses = new SelectList(FollowUpStatuses.All);
    }
}
