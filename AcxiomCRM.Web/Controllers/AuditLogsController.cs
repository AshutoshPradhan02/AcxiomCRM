using AcxiomCRM.Web.Data;
using AcxiomCRM.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Web.Controllers;

[Authorize(Roles = CrmRoles.Admin)]
public class AuditLogsController : Controller
{
    private readonly ApplicationDbContext _db;
    public AuditLogsController(ApplicationDbContext db) => _db = db;
    public async Task<IActionResult> Index(string? module, string? action, DateTime? from, DateTime? to)
    {
        var q = _db.AuditLogs.Include(a => a.User).AsQueryable();
        if (!string.IsNullOrWhiteSpace(module)) q = q.Where(a => a.EntityName == module);
        if (!string.IsNullOrWhiteSpace(action)) q = q.Where(a => a.Action == action);
        if (from.HasValue) q = q.Where(a => a.CreatedDate >= from.Value);
        if (to.HasValue) q = q.Where(a => a.CreatedDate <= to.Value.AddDays(1));
        ViewBag.Module = module; ViewBag.Action = action; ViewBag.From = from?.ToString("yyyy-MM-dd"); ViewBag.To = to?.ToString("yyyy-MM-dd");
        return View(await q.OrderByDescending(a => a.CreatedDate).Take(250).ToListAsync());
    }
}
