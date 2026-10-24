using NexusCommunication.Models;

namespace NexusCommunication.Services;

/// <summary>
/// Bill due reminders (portal banner + bell alert). No extra table: everything is worked out from the existing Bills.
/// A bill needs a reminder when it still has money due and its due date is overdue or within <see cref="WarnDays"/> days.
/// </summary>
public static class BillReminder
{
    /// <summary>How many days before the due date the reminder starts</summary>
    public const int WarnDays = 5;

    /// <summary>Bills (already limited to one customer) that need a reminder now - used by the database query</summary>
    public static IQueryable<Bill> Due(IQueryable<Bill> bills, DateTime nowUtc)
    {
        var limit = nowUtc.AddDays(WarnDays);
        return bills.Where(b => b.Status != BillStatus.Paid && b.DueAmount > 0 && b.DueDate != null && b.DueDate <= limit);
    }

    /// <summary>Same rule for bills that are already in memory</summary>
    public static bool IsDue(Bill b, DateTime nowUtc) =>
        b.Status != BillStatus.Paid && b.DueAmount > 0 && b.DueDate != null && b.DueDate <= nowUtc.AddDays(WarnDays);

    /// <summary>Same rule the accounts desk uses for "overdue"</summary>
    public static bool IsOverdue(Bill b, DateTime nowUtc) => b.DueDate != null && b.DueDate < nowUtc;

    /// <summary>"overdue by 4 days" / "due today" / "due tomorrow" / "due in 3 days"</summary>
    public static string When(Bill b, DateTime nowUtc)
    {
        if (b.DueDate == null) return "";
        if (IsOverdue(b, nowUtc))
        {
            var late = (int)(nowUtc - b.DueDate.Value).TotalDays;
            return late <= 0 ? "overdue" : $"overdue by {late} day{(late == 1 ? "" : "s")}";
        }
        var left = (b.DueDate.Value.ToLocalTime().Date - DateTime.Now.Date).Days;
        return left <= 0 ? "due today" : left == 1 ? "due tomorrow" : $"due in {left} days";
    }
}
