using AcxiomCRM.Web.DTOs;
using AcxiomCRM.Web.Data;
using AcxiomCRM.Web.Models;
using AcxiomCRM.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Web.Controllers.Api;

[ApiController]
[Route("api/customers")]
[Authorize]
public class CustomersApiController : ControllerBase
{
    private readonly ApplicationDbContext _db; private readonly IScopeService _scope; private readonly IAuditService _audit;
    public CustomersApiController(ApplicationDbContext db, IScopeService scope, IAuditService audit) { _db = db; _scope = scope; _audit = audit; }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CustomerDto>>> Get(string? search)
    {
        var query = _scope.Customers(User);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(c => c.CustomerName.Contains(search) || c.Email.Contains(search));
        return await query.Select(c => new CustomerDto(c.CustomerId, c.CustomerCode, c.CustomerName, c.Email, c.Phone, c.CompanyName, c.Status, c.AssignedToId)).ToListAsync();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CustomerDto>> Get(int id)
    {
        var c = await _scope.Customers(User).FirstOrDefaultAsync(x => x.CustomerId == id);
        return c is null ? NotFound(new { error = "Customer not found." }) : new CustomerDto(c.CustomerId, c.CustomerCode, c.CustomerName, c.Email, c.Phone, c.CompanyName, c.Status, c.AssignedToId);
    }

    [HttpPost]
    public async Task<ActionResult<CustomerDto>> Post(CustomerRequest request)
    {
        var conflict = await ValidateAsync(request);
        if (conflict is not null) return conflict;
        var customer = new Customer { CustomerCode = $"CUS-{await _db.Customers.CountAsync() + 1001}", CustomerName = request.CustomerName, Email = request.Email, Phone = request.Phone, CompanyName = request.CompanyName, Address = request.Address, City = request.City, State = request.State, Status = request.Status, AssignedToId = request.AssignedToId, CreatedBy = _scope.UserId(User) };
        _db.Customers.Add(customer); await _db.SaveChangesAsync();
        await _audit.RecordAsync("Create", "Customer", customer.CustomerId.ToString(), newValue: customer.CustomerName);
        var dto = new CustomerDto(customer.CustomerId, customer.CustomerCode, customer.CustomerName, customer.Email, customer.Phone, customer.CompanyName, customer.Status, customer.AssignedToId);
        return CreatedAtAction(nameof(Get), new { id = customer.CustomerId }, dto);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Put(int id, CustomerRequest request)
    {
        var customer = await _scope.Customers(User).FirstOrDefaultAsync(c => c.CustomerId == id);
        if (customer is null) return NotFound(new { error = "Customer not found." });
        var conflict = await ValidateAsync(request, id);
        if (conflict is not null) return conflict;
        customer.CustomerName = request.CustomerName; customer.Email = request.Email; customer.Phone = request.Phone; customer.CompanyName = request.CompanyName; customer.Address = request.Address; customer.City = request.City; customer.State = request.State; customer.Status = request.Status; customer.AssignedToId = request.AssignedToId;
        await _db.SaveChangesAsync(); await _audit.RecordAsync("Update", "Customer", id.ToString(), newValue: customer.CustomerName);
        return Ok(new CustomerDto(customer.CustomerId, customer.CustomerCode, customer.CustomerName, customer.Email, customer.Phone, customer.CompanyName, customer.Status, customer.AssignedToId));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var customer = await _scope.Customers(User).FirstOrDefaultAsync(c => c.CustomerId == id);
        if (customer is null) return NotFound(new { error = "Customer not found." });
        customer.Status = "Inactive"; await _db.SaveChangesAsync(); await _audit.RecordAsync("Deactivate", "Customer", id.ToString());
        return NoContent();
    }

    private async Task<ActionResult?> ValidateAsync(CustomerRequest request, int? id = null)
    {
        if (!CustomerStatuses.All.Contains(request.Status)) return BadRequest(new { error = "Invalid status." });
        if (!await _scope.CanAccessOwnerAsync(User, request.AssignedToId)) return Forbid();
        if (await _db.Customers.AnyAsync(c => c.Email == request.Email && c.CustomerId != id)) return Conflict(new { error = "A customer with this email already exists." });
        if (await _db.Customers.AnyAsync(c => c.Phone == request.Phone && c.CustomerId != id)) return Conflict(new { error = "A customer with this phone already exists." });
        return null;
    }
}
