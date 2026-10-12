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
public class OpportunitiesController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IScopeService _scope;
    private readonly IAuditService _audit;
    private readonly UserManager<ApplicationUser> _userManager;
    public OpportunitiesController(ApplicationDbContext db, IScopeService scope, IAuditService audit, UserManager<ApplicationUser> userManager)
    { _db = db; _scope = scope; _audit = audit; _userManager = userManager; }

    public async Task<IActionResult> Index(string? search, string? stage, string? status)
    {
        var query = _scope.Opportunities(User);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(o => o.OpportunityName.Contains(search) || o.Customer!.CustomerName.Contains(search));
        if (!string.IsNullOrWhiteSpace(stage)) query = query.Where(o => o.Stage == stage);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(o => o.Status == status);
        ViewBag.Search = search; ViewBag.Stage = stage; ViewBag.Status = status;
        return View(await query.OrderByDescending(o => o.CreatedDate).ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var item = await _scope.Opportunities(User).FirstOrDefaultAsync(o => o.OpportunityId == id);
        return item is null ? NotFound() : View(item);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateAsync();
        return View(new Opportunity { AssignedToId = _userManager.GetUserId(User) ?? string.Empty, ExpectedCloseDate = DateTime.Today.AddDays(30) });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Opportunity opportunity)
    {
        await ValidateOpportunityAsync(opportunity);
        if (!ModelState.IsValid) { await PopulateAsync(); return View(opportunity); }
        _db.Opportunities.Add(opportunity);
        await _db.SaveChangesAsync();
        await _audit.RecordAsync("Create", "Opportunity", opportunity.OpportunityId.ToString(), newValue: opportunity.OpportunityName);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await _scope.Opportunities(User).FirstOrDefaultAsync(o => o.OpportunityId == id);
        if (item is null) return NotFound();
        await PopulateAsync();
        return View(item);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Opportunity opportunity)
    {
        if (id != opportunity.OpportunityId) return NotFound();
        var existing = await _scope.Opportunities(User).AsNoTracking().FirstOrDefaultAsync(o => o.OpportunityId == id);
        if (existing is null) return NotFound();
        await ValidateOpportunityAsync(opportunity);
        if (!ModelState.IsValid) { await PopulateAsync(); return View(opportunity); }
        opportunity.CreatedDate = existing.CreatedDate;
        _db.Update(opportunity);
        await _db.SaveChangesAsync();
        if (opportunity.Status is OpportunityStatuses.Won or OpportunityStatuses.Lost)
            await _audit.RecordAsync("Outcome", "Opportunity", id.ToString(), oldValue: existing.Status, newValue: opportunity.Status);
        else
            await _audit.RecordAsync("Update", "Opportunity", id.ToString(), oldValue: existing.Stage, newValue: opportunity.Stage);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _scope.Opportunities(User).FirstOrDefaultAsync(o => o.OpportunityId == id);
        if (item is null) return NotFound();
        item.Status = OpportunityStatuses.Cancelled;
        await _db.SaveChangesAsync();
        await _audit.RecordAsync("Deactivate", "Opportunity", id.ToString(), newValue: OpportunityStatuses.Cancelled);
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateOpportunityAsync(Opportunity opportunity)
    {
        if (!OpportunityStages.All.Contains(opportunity.Stage)) ModelState.AddModelError(nameof(Opportunity.Stage), "Select a valid stage.");
        if (!OpportunityStatuses.All.Contains(opportunity.Status)) ModelState.AddModelError(nameof(Opportunity.Status), "Select a valid status.");
        if (!await _scope.Customers(User).AnyAsync(c => c.CustomerId == opportunity.CustomerId)) ModelState.AddModelError(nameof(Opportunity.CustomerId), "Select a valid customer.");
        if (!await _scope.CanAccessOwnerAsync(User, opportunity.AssignedToId)) ModelState.AddModelError(nameof(Opportunity.AssignedToId), "You cannot assign records to that user.");
    }

    private async Task PopulateAsync()
    {
        ViewBag.Customers = new SelectList(await _scope.Customers(User).OrderBy(c => c.CustomerName).ToListAsync(), "CustomerId", "CustomerName");
        var users = await _db.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
        if (!User.IsInRole(CrmRoles.Admin) && !User.IsInRole(CrmRoles.Manager)) users = users.Where(u => u.Id == _userManager.GetUserId(User)).ToList();
        ViewBag.Owners = new SelectList(users, "Id", "FullName");
        ViewBag.Stages = new SelectList(OpportunityStages.All);
        ViewBag.Statuses = new SelectList(OpportunityStatuses.All);
    }
}
