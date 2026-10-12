namespace AcxiomCRM.Web.ViewModels;

public class DashboardViewModel
{
    public int TotalCustomers { get; set; }
    public int TotalLeads { get; set; }
    public int OpenLeads { get; set; }
    public int TotalOpportunities { get; set; }
    public int OpenOpportunities { get; set; }
    public int WonOpportunities { get; set; }
    public int LostOpportunities { get; set; }
    public int PendingFollowUps { get; set; }
    public int OverdueFollowUps { get; set; }
    public decimal TotalPipelineValue { get; set; }
    public decimal WeightedPipelineValue { get; set; }
    public Dictionary<string, int> LeadStatusData { get; set; } = [];
    public Dictionary<string, int> OpportunityStageData { get; set; } = [];
    public Dictionary<string, decimal> MonthlySalesData { get; set; } = [];
}
