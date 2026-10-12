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
public class ActivitiesController : Controller
{
    private readonly ApplicationDbContext _db; private readonly IScopeService _scope; private readonly IAuditService _audit; private readonly UserManager<ApplicationUser> _userManager;
    public ActivitiesController(ApplicationDbContext db, IScopeService scope, IAuditService audit, UserManager<ApplicationUser> userManager) { _db = db; _scope = scope; _audit = audit; _userManager = userManager; }
    public async Task<IActionResult> Index(string? type, string? status)
    {
        var q = _scope.Activities(User);
        if (!string.IsNullOrWhiteSpace(type)) q = q.Where(a => a.ActivityType == type);
        if (!string.IsNullOrWhiteSpace(status)) q = q.Where(a => a.Status == status);
        return View(await q.OrderByDescending(a => a.ActivityDate).ToListAsync());
    }
    public async Task<IActionResult> Create() { await PopulateAsync(); return View(new CrmActivity { AssignedToId = _userManager.GetUserId(User) ?? string.Empty }); }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CrmActivity activity)
    {
        if (!await _scope.CanAccessOwnerAsync(User, activity.AssignedToId)) ModelState.AddModelError(nameof(CrmActivity.AssignedToId), "You cannot assign records to that user.");
        if (!ModelState.IsValid) { await PopulateAsync(); return View(activity); }
        _db.Activities.Add(activity); await _db.SaveChangesAsync(); await _audit.RecordAsync("Create", "Activity", activity.ActivityId.ToString());
        return RedirectToAction(nameof(Index));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id)
    {
        var a = await _scope.Activities(User).FirstOrDefaultAsync(x => x.ActivityId == id); if (a is null) return NotFound();
        a.Status = ActivityStatuses.Completed; await _db.SaveChangesAsync(); await _audit.RecordAsync("Complete", "Activity", id.ToString());
        return RedirectToAction(nameof(Index));
    }
    private async Task PopulateAsync()
    {
        ViewBag.Customers = new SelectList(await _scope.Customers(User).OrderBy(c => c.CustomerName).ToListAsync(), "CustomerId", "CustomerName");
        ViewBag.Leads = new SelectList(await _scope.Leads(User).OrderBy(l => l.LeadName).ToListAsync(), "LeadId", "LeadName");
        var users = await _db.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
        if (!User.IsInRole(CrmRoles.Admin) && !User.IsInRole(CrmRoles.Manager)) users = users.Where(u => u.Id == _userManager.GetUserId(User)).ToList();
        ViewBag.Owners = new SelectList(users, "Id", "FullName");
    }
}
