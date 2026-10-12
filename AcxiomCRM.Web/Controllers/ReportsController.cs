using AcxiomCRM.Web.Models;
using AcxiomCRM.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace AcxiomCRM.Web.Controllers;

[Authorize]
public class ReportsController : Controller
{
    private readonly IScopeService _scope;
    public ReportsController(IScopeService scope) => _scope = scope;
    public async Task<IActionResult> Index()
    {
        ViewBag.CustomersByStatus = await _scope.Customers(User).GroupBy(c => c.Status).Select(g => new ReportRow(g.Key, g.Count(), 0)).ToListAsync();
        ViewBag.LeadsByStatus = await _scope.Leads(User).GroupBy(l => l.Status).Select(g => new ReportRow(g.Key, g.Count(), 0)).ToListAsync();
        ViewBag.Pipeline = await _scope.Opportunities(User).GroupBy(o => o.Stage).Select(g => new ReportRow(g.Key, g.Count(), g.Sum(o => o.Amount * o.Probability / 100m))).ToListAsync();
        ViewBag.FollowUps = await _scope.FollowUps(User).GroupBy(f => f.Status).Select(g => new ReportRow(g.Key, g.Count(), 0)).ToListAsync();
        return View();
    }
    public async Task<IActionResult> PipelineCsv()
    {
        var rows = await _scope.Opportunities(User).GroupBy(o => o.Stage).Select(g => new { Stage = g.Key, Count = g.Count(), Amount = g.Sum(o => o.Amount), Weighted = g.Sum(o => o.Amount * o.Probability / 100m) }).ToListAsync();
        var csv = new StringBuilder("Stage,Count,Amount,Weighted\r\n");
        foreach (var r in rows) csv.AppendLine($"{r.Stage},{r.Count},{r.Amount},{r.Weighted}");
        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "pipeline-report.csv");
    }
    public record ReportRow(string Label, int Count, decimal Amount);
}
