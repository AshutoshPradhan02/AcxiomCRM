using AcxiomCRM.Web.Data;
using AcxiomCRM.Web.Models;
using Microsoft.AspNetCore.Http;

namespace AcxiomCRM.Web.Services;

public interface IAuditService
{
    Task RecordAsync(string action, string entityName, string? recordId, string result = "Success", string? oldValue = null, string? newValue = null, string? metadata = null, string? userId = null);
}

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditService(ApplicationDbContext db, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task RecordAsync(string action, string entityName, string? recordId, string result = "Success", string? oldValue = null, string? newValue = null, string? metadata = null, string? userId = null)
    {
        var context = _httpContextAccessor.HttpContext;
        var safeMetadata = metadata;
        if (safeMetadata?.Contains("password", StringComparison.OrdinalIgnoreCase) == true ||
            safeMetadata?.Contains("token", StringComparison.OrdinalIgnoreCase) == true)
        {
            safeMetadata = "[redacted]";
        }

        _db.AuditLogs.Add(new AuditLog
        {
            Action = action,
            EntityName = entityName,
            RecordId = recordId,
            Result = result,
            OldValue = oldValue,
            NewValue = newValue,
            Metadata = safeMetadata,
            UserId = userId ?? context?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
            IpAddress = context?.Connection.RemoteIpAddress?.ToString()
        });

        await _db.SaveChangesAsync();
    }
}
