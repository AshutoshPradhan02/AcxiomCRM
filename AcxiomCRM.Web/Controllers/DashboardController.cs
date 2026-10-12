using AcxiomCRM.Web.Models;
using AcxiomCRM.Web.Services;
using AcxiomCRM.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IScopeService _scope;

    public DashboardController(IScopeService scope)
    {
        _scope = scope;
    }

    public async Task<IActionResult> Index()
    {
        var customers = _scope.Customers(User);
        var leads = _scope.Leads(User);
        var opportunities = _scope.Opportunities(User);
        var followUps = _scope.FollowUps(User);
        var openOpps = opportunities.Where(o => o.Status == OpportunityStatuses.Open);
        var model = new DashboardViewModel
        {
            TotalCustomers = await customers.CountAsync(),
            TotalLeads = await leads.CountAsync(),
            OpenLeads = await leads.CountAsync(l => LeadStatuses.Open.Contains(l.Status)),
            TotalOpportunities = await opportunities.CountAsync(),
            OpenOpportunities = await opportunities.CountAsync(o => o.Status == OpportunityStatuses.Open),
            WonOpportunities = await opportunities.CountAsync(o => o.Status == OpportunityStatuses.Won),
            LostOpportunities = await opportunities.CountAsync(o => o.Status == OpportunityStatuses.Lost),
            PendingFollowUps = await followUps.CountAsync(f => f.Status == FollowUpStatuses.Planned),
            OverdueFollowUps = await followUps.CountAsync(f => f.Status == FollowUpStatuses.Planned && f.FollowUpDate.Date < DateTime.Today),
            TotalPipelineValue = await openOpps.SumAsync(o => (decimal?)o.Amount) ?? 0,
            WeightedPipelineValue = await openOpps.SumAsync(o => (decimal?)(o.Amount * o.Probability / 100m)) ?? 0,
            LeadStatusData = await leads.GroupBy(l => l.Status).Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count),
            OpportunityStageData = await opportunities.GroupBy(o => o.Stage).Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count),
            MonthlySalesData = await opportunities.Where(o => o.Status == OpportunityStatuses.Won)
                .GroupBy(o => new { o.ExpectedCloseDate.Year, o.ExpectedCloseDate.Month })
                .Select(g => new { Key = $"{g.Key.Year}-{g.Key.Month:00}", Amount = g.Sum(o => o.Amount) })
                .ToDictionaryAsync(x => x.Key, x => x.Amount)
        };
        return View(model);
    }
}
