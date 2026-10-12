using AcxiomCRM.Web.Data;
using AcxiomCRM.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AcxiomCRM.Web.Services;

public interface IScopeService
{
    IQueryable<Customer> Customers(ClaimsPrincipal user);
    IQueryable<Lead> Leads(ClaimsPrincipal user);
    IQueryable<Opportunity> Opportunities(ClaimsPrincipal user);
    IQueryable<FollowUp> FollowUps(ClaimsPrincipal user);
    IQueryable<CrmActivity> Activities(ClaimsPrincipal user);
    Task<bool> CanAccessOwnerAsync(ClaimsPrincipal user, string ownerId);
    string? UserId(ClaimsPrincipal user);
}

public class ScopeService : IScopeService
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public ScopeService(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public string? UserId(ClaimsPrincipal user) => _userManager.GetUserId(user);

    public IQueryable<Customer> Customers(ClaimsPrincipal user)
    {
        var query = _db.Customers.Include(c => c.AssignedTo).AsQueryable();
        return ApplyOwnerScope(query, user);
    }

    public IQueryable<Lead> Leads(ClaimsPrincipal user)
    {
        var query = _db.Leads.Include(l => l.AssignedTo).AsQueryable();
        return ApplyOwnerScope(query, user);
    }

    public IQueryable<Opportunity> Opportunities(ClaimsPrincipal user)
    {
        var query = _db.Opportunities.Include(o => o.Customer).Include(o => o.AssignedTo).AsQueryable();
        return ApplyOwnerScope(query, user);
    }

    public IQueryable<FollowUp> FollowUps(ClaimsPrincipal user)
    {
        var query = _db.FollowUps.Include(f => f.Customer).Include(f => f.Lead).Include(f => f.Opportunity).Include(f => f.AssignedTo).AsQueryable();
        return ApplyOwnerScope(query, user);
    }

    public IQueryable<CrmActivity> Activities(ClaimsPrincipal user)
    {
        var query = _db.Activities.Include(a => a.Customer).Include(a => a.Lead).Include(a => a.AssignedTo).AsQueryable();
        return ApplyOwnerScope(query, user);
    }

    public async Task<bool> CanAccessOwnerAsync(ClaimsPrincipal user, string ownerId)
    {
        if (user.IsInRole(CrmRoles.Admin)) return true;
        var currentId = UserId(user);
        if (currentId == ownerId) return true;
        if (!user.IsInRole(CrmRoles.Manager)) return false;
        return await _db.Users.AnyAsync(u => u.Id == ownerId && u.ManagerId == currentId);
    }

    private IQueryable<T> ApplyOwnerScope<T>(IQueryable<T> query, ClaimsPrincipal user) where T : class
    {
        if (user.IsInRole(CrmRoles.Admin)) return query;
        var id = UserId(user);
        if (user.IsInRole(CrmRoles.Manager))
        {
            var memberIds = _db.Users.Where(u => u.ManagerId == id || u.Id == id).Select(u => u.Id);
            return query.Where(e => memberIds.Contains(EF.Property<string>(e, "AssignedToId")));
        }
        return query.Where(e => EF.Property<string>(e, "AssignedToId") == id);
    }
}
