using AcxiomCRM.Web.Data;
using AcxiomCRM.Web.Models;
using AcxiomCRM.Web.Services;
using AcxiomCRM.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Web.Controllers;

[Authorize(Roles = CrmRoles.Admin)]
public class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager; private readonly ApplicationDbContext _db; private readonly IAuditService _audit;
    public UsersController(UserManager<ApplicationUser> userManager, ApplicationDbContext db, IAuditService audit) { _userManager = userManager; _db = db; _audit = audit; }
    public async Task<IActionResult> Index(string? search)
    {
        var users = await _db.Users.Where(u => string.IsNullOrWhiteSpace(search) || u.Email!.Contains(search) || u.FullName.Contains(search)).OrderBy(u => u.FullName).ToListAsync();
        var rows = new List<dynamic>();
        foreach (var u in users) rows.Add(new { User = u, Role = (await _userManager.GetRolesAsync(u)).FirstOrDefault() ?? "", Locked = u.LockoutEnd > DateTimeOffset.UtcNow });
        ViewBag.Search = search;
        return View(rows);
    }
    public async Task<IActionResult> Create() { await PopulateAsync(); return View(new CreateUserViewModel()); }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserViewModel model)
    {
        if (!new[] { CrmRoles.Admin, CrmRoles.Manager, CrmRoles.SalesExecutive }.Contains(model.Role)) ModelState.AddModelError(nameof(model.Role), "Select a valid role.");
        if (!ModelState.IsValid) { await PopulateAsync(); return View(model); }
        var user = new ApplicationUser { UserName = model.Email, Email = model.Email, EmailConfirmed = true, FullName = model.FullName, ManagerId = model.ManagerId };
        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, model.Role);
            await _audit.RecordAsync("Create", "User", user.Id, newValue: model.Role);
            return RedirectToAction(nameof(Index));
        }
        foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
        await PopulateAsync(); return View(model);
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(string id)
    {
        var user = await _userManager.FindByIdAsync(id); if (user is null) return NotFound();
        user.IsActive = !user.IsActive; await _userManager.UpdateAsync(user);
        await _audit.RecordAsync("UpdateStatus", "User", id, newValue: user.IsActive ? "Active" : "Inactive");
        return RedirectToAction(nameof(Index));
    }
    private async Task PopulateAsync()
    {
        ViewBag.Roles = new SelectList(new[] { CrmRoles.Admin, CrmRoles.Manager, CrmRoles.SalesExecutive });
        ViewBag.Managers = new SelectList(await _db.Users.OrderBy(u => u.FullName).ToListAsync(), "Id", "FullName");
    }
}
