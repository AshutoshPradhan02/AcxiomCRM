using AcxiomCRM.Web.Models;
using AcxiomCRM.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Web.Controllers.Api;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsApiController : ControllerBase
{
    private readonly IScopeService _scope;
    public ReportsApiController(IScopeService scope) => _scope = scope;
    [HttpGet("pipeline")]
    public async Task<IActionResult> Pipeline()
    {
        var data = await _scope.Opportunities(User).Where(o => o.Status == OpportunityStatuses.Open)
            .GroupBy(o => new { o.Stage, Owner = o.AssignedTo!.FullName })
            .Select(g => new { g.Key.Stage, g.Key.Owner, Amount = g.Sum(o => o.Amount), Weighted = g.Sum(o => o.Amount * o.Probability / 100m), Count = g.Count() })
            .ToListAsync();
        return Ok(data);
    }
}
