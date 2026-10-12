using AcxiomCRM.Web.DTOs;
using AcxiomCRM.Web.Data;
using AcxiomCRM.Web.Models;
using AcxiomCRM.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Web.Controllers.Api;

[ApiController]
[Route("api/leads")]
[Authorize]
public class LeadsApiController : ControllerBase
{
    private readonly ApplicationDbContext _db; private readonly IScopeService _scope; private readonly IAuditService _audit;
    public LeadsApiController(ApplicationDbContext db, IScopeService scope, IAuditService audit) { _db = db; _scope = scope; _audit = audit; }
    [HttpGet] public async Task<IEnumerable<LeadDto>> Get() => await _scope.Leads(User).Select(l => new LeadDto(l.LeadId, l.LeadCode, l.LeadName, l.Email, l.Phone, l.CompanyName, l.Source, l.ExpectedValue, l.Status, l.AssignedToId)).ToListAsync();
    [HttpPost] public async Task<ActionResult<LeadDto>> Post(LeadRequest r)
    {
        if (!LeadStatuses.All.Contains(r.Status)) return BadRequest(new { error = "Invalid lead status." });
        if (!await _scope.CanAccessOwnerAsync(User, r.AssignedToId)) return Forbid();
        var lead = new Lead { LeadCode = $"LED-{await _db.Leads.CountAsync() + 1001}", LeadName = r.LeadName, Email = r.Email, Phone = r.Phone, CompanyName = r.CompanyName, Source = r.Source, ExpectedValue = r.ExpectedValue, Status = r.Status, AssignedToId = r.AssignedToId };
        _db.Leads.Add(lead); await _db.SaveChangesAsync(); await _audit.RecordAsync("Create", "Lead", lead.LeadId.ToString());
        return Created($"/api/leads/{lead.LeadId}", new LeadDto(lead.LeadId, lead.LeadCode, lead.LeadName, lead.Email, lead.Phone, lead.CompanyName, lead.Source, lead.ExpectedValue, lead.Status, lead.AssignedToId));
    }
}
