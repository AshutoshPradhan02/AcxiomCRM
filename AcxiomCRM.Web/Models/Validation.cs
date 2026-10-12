using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace AcxiomCRM.Web.Models;

public sealed class CrmPhoneAttribute : ValidationAttribute
{
    private static readonly Regex PhonePattern = new(@"^[0-9+\-\s()]{7,20}$", RegexOptions.Compiled);

    public CrmPhoneAttribute() : base("Enter a valid phone number.")
    {
    }

    public override bool IsValid(object? value)
    {
        if (value is null) return false;
        return value is string text && PhonePattern.IsMatch(text.Trim());
    }
}

public sealed class TodayOrFutureAttribute : ValidationAttribute
{
    public TodayOrFutureAttribute() : base("Follow-up date cannot be earlier than today.")
    {
    }

    public override bool IsValid(object? value)
    {
        if (value is null) return true;
        return value is DateTime date && date.Date >= DateTime.Today;
    }
}
