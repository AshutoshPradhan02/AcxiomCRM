namespace AcxiomCRM.Web.Models;

public static class CrmRoles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string SalesExecutive = "SalesExecutive";
}

public static class CustomerStatuses
{
    public static readonly string[] All = ["Active", "Inactive"];
}

public static class LeadStatuses
{
    public const string New = "New";
    public const string Contacted = "Contacted";
    public const string Qualified = "Qualified";
    public const string Unqualified = "Unqualified";
    public const string Converted = "Converted";
    public const string Lost = "Lost";
    public static readonly string[] All = [New, Contacted, Qualified, Unqualified, Converted, Lost];
    public static readonly string[] Open = [New, Contacted, Qualified, Unqualified];
}

public static class OpportunityStages
{
    public const string Qualification = "Qualification";
    public const string Proposal = "Proposal";
    public const string Negotiation = "Negotiation";
    public const string Won = "Won";
    public const string Lost = "Lost";
    public static readonly string[] All = [Qualification, Proposal, Negotiation, Won, Lost];
}

public static class OpportunityStatuses
{
    public const string Open = "Open";
    public const string Won = "Won";
    public const string Lost = "Lost";
    public const string Cancelled = "Cancelled";
    public static readonly string[] All = [Open, Won, Lost, Cancelled];
}

public static class FollowUpStatuses
{
    public const string Planned = "Planned";
    public const string Completed = "Completed";
    public const string Missed = "Missed";
    public const string Cancelled = "Cancelled";
    public static readonly string[] All = [Planned, Completed, Missed, Cancelled];
}

public static class ActivityStatuses
{
    public const string Open = "Open";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";
    public static readonly string[] All = [Open, Completed, Cancelled];
}
