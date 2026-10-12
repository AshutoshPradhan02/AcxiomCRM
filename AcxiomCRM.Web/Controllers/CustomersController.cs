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
public class CustomersController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IScopeService _scope;
    private readonly IAuditService _audit;
    private readonly UserManager<ApplicationUser> _userManager;

    public CustomersController(ApplicationDbContext db, IScopeService scope, IAuditService audit, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _scope = scope;
        _audit = audit;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(string? search, string? status, string sort = "name", int page = 1)
    {
        const int pageSize = 10;
        var query = _scope.Customers(User);
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(c => c.CustomerName.Contains(search) || c.Email.Contains(search) || c.Phone.Contains(search) || (c.CompanyName != null && c.CompanyName.Contains(search)));
        }
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(c => c.Status == status);
        query = sort switch
        {
            "created" => query.OrderByDescending(c => c.CreatedDate),
            "company" => query.OrderBy(c => c.CompanyName),
            _ => query.OrderBy(c => c.CustomerName)
        };

        ViewBag.Search = search;
        ViewBag.Status = status;
        ViewBag.Sort = sort;
        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling(await query.CountAsync() / (double)pageSize);
        return View(await query.Skip((Math.Max(page, 1) - 1) * pageSize).Take(pageSize).ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var customer = await _scope.Customers(User).FirstOrDefaultAsync(c => c.CustomerId == id);
        if (customer is null) return NotFound();
        return View(customer);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateOwnersAsync();
        return View(new Customer { CustomerCode = await NextCodeAsync(), AssignedToId = _userManager.GetUserId(User) ?? string.Empty });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Customer customer)
    {
        await ValidateCustomerAsync(customer);
        if (!await _scope.CanAccessOwnerAsync(User, customer.AssignedToId))
        {
            ModelState.AddModelError(nameof(Customer.AssignedToId), "You cannot assign records to that user.");
        }
        if (!ModelState.IsValid)
        {
            await PopulateOwnersAsync();
            return View(customer);
        }

        customer.CreatedBy = _userManager.GetUserId(User);
        customer.CreatedDate = DateTime.UtcNow;
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();
        await _audit.RecordAsync("Create", "Customer", customer.CustomerId.ToString(), newValue: $"{customer.CustomerCode} {customer.CustomerName}");
        TempData["Success"] = "Customer created.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var customer = await _scope.Customers(User).FirstOrDefaultAsync(c => c.CustomerId == id);
        if (customer is null) return NotFound();
        await PopulateOwnersAsync();
        return View(customer);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Customer customer)
    {
        if (id != customer.CustomerId) return NotFound();
        var existing = await _scope.Customers(User).AsNoTracking().FirstOrDefaultAsync(c => c.CustomerId == id);
        if (existing is null) return NotFound();
        await ValidateCustomerAsync(customer, id);
        if (!await _scope.CanAccessOwnerAsync(User, customer.AssignedToId))
        {
            ModelState.AddModelError(nameof(Customer.AssignedToId), "You cannot assign records to that user.");
        }
        if (!ModelState.IsValid)
        {
            await PopulateOwnersAsync();
            return View(customer);
        }

        customer.CreatedBy = existing.CreatedBy;
        customer.CreatedDate = existing.CreatedDate;
        _db.Update(customer);
        await _db.SaveChangesAsync();
        await _audit.RecordAsync("Update", "Customer", id.ToString(), oldValue: existing.CustomerName, newValue: customer.CustomerName);
        TempData["Success"] = "Customer updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var customer = await _scope.Customers(User).FirstOrDefaultAsync(c => c.CustomerId == id);
        if (customer is null) return NotFound();
        customer.Status = "Inactive";
        await _db.SaveChangesAsync();
        await _audit.RecordAsync("Deactivate", "Customer", id.ToString(), oldValue: "Active", newValue: "Inactive");
        TempData["Warning"] = "Customer deactivated.";
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateCustomerAsync(Customer customer, int? id = null)
    {
        if (!CustomerStatuses.All.Contains(customer.Status))
        {
            ModelState.AddModelError(nameof(Customer.Status), "Select a valid customer status.");
        }
        if (await _db.Customers.AnyAsync(c => c.Email == customer.Email && c.CustomerId != id))
        {
            ModelState.AddModelError(nameof(Customer.Email), "A customer with this email already exists.");
        }
        if (await _db.Customers.AnyAsync(c => c.Phone == customer.Phone && c.CustomerId != id))
        {
            ModelState.AddModelError(nameof(Customer.Phone), "A customer with this phone already exists.");
        }
    }

    private async Task PopulateOwnersAsync()
    {
        var users = await _db.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
        if (!User.IsInRole(CrmRoles.Admin) && !User.IsInRole(CrmRoles.Manager))
        {
            var id = _userManager.GetUserId(User);
            users = users.Where(u => u.Id == id).ToList();
        }
        ViewBag.Owners = new SelectList(users, "Id", "FullName");
        ViewBag.Statuses = new SelectList(CustomerStatuses.All);
    }

    private async Task<string> NextCodeAsync()
    {
        var next = await _db.Customers.CountAsync() + 1001;
        return $"CUS-{next}";
    }
}
