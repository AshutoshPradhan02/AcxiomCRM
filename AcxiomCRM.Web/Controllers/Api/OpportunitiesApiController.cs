using AcxiomCRM.Web.DTOs;
using AcxiomCRM.Web.Data;
using AcxiomCRM.Web.Models;
using AcxiomCRM.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Web.Controllers.Api;

[ApiController]
[Route("api/opportunities")]
[Authorize]
public class OpportunitiesApiController : ControllerBase
{
    private readonly ApplicationDbContext _db; private readonly IScopeService _scope; private readonly IAuditService _audit;
    public OpportunitiesApiController(ApplicationDbContext db, IScopeService scope, IAuditService audit) { _db = db; _scope = scope; _audit = audit; }
    [HttpGet] public async Task<IEnumerable<OpportunityDto>> Get() => await _scope.Opportunities(User).Select(o => new OpportunityDto(o.OpportunityId, o.OpportunityName, o.CustomerId, o.Amount, o.Stage, o.Probability, o.ExpectedCloseDate, o.Status, o.AssignedToId, o.Amount * o.Probability / 100m)).ToListAsync();
    [HttpPost] public async Task<ActionResult<OpportunityDto>> Post(OpportunityRequest r)
    {
        if (r.Status == OpportunityStatuses.Open && r.Amount <= 0) ModelState.AddModelError(nameof(r.Amount), "Opportunity Amount must be greater than 0.");
        if (r.Status == OpportunityStatuses.Open && r.ExpectedCloseDate.Date < DateTime.Today) ModelState.AddModelError(nameof(r.ExpectedCloseDate), "Expected Close Date cannot be in the past.");
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        if (!OpportunityStages.All.Contains(r.Stage) || !OpportunityStatuses.All.Contains(r.Status)) return BadRequest(new { error = "Invalid stage or status." });
        if (!await _scope.Customers(User).AnyAsync(c => c.CustomerId == r.CustomerId)) return NotFound(new { error = "Customer not found." });
        if (!await _scope.CanAccessOwnerAsync(User, r.AssignedToId)) return Forbid();
        var o = new Opportunity { OpportunityName = r.OpportunityName, CustomerId = r.CustomerId, Amount = r.Amount, Stage = r.Stage, Probability = r.Probability, ExpectedCloseDate = r.ExpectedCloseDate, Status = r.Status, AssignedToId = r.AssignedToId };
        _db.Opportunities.Add(o); await _db.SaveChangesAsync(); await _audit.RecordAsync("Create", "Opportunity", o.OpportunityId.ToString());
        return Created($"/api/opportunities/{o.OpportunityId}", new OpportunityDto(o.OpportunityId, o.OpportunityName, o.CustomerId, o.Amount, o.Stage, o.Probability, o.ExpectedCloseDate, o.Status, o.AssignedToId, o.WeightedPipeline));
    }
}
