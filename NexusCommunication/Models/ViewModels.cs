namespace NexusCommunication.Models;

public record HomeVm(List<Plan> Plans, List<RetailShop> Shops, List<Feedback> Quotes, int Connections, int Orders, int Outlets, int Cities);
public record PlansBrowserVm(IReadOnlyList<Plan> Plans, bool Compact);
public record TabBarVm(string Gradient, (string Id, string Label, string Icon)[] Tabs);

public record FieldDef(
    string Name, string Label, string Type = "text", bool Required = false, string? Placeholder = null,
    string? Step = null, bool Numeric = false, bool Wide = false, (string Value, string Label)[]? Options = null);
public record CrudHeadVm(string Title, string Subtitle, string AddLabel, FieldDef[] Fields);

public record MonthPoint(string Label, decimal Invoiced, decimal Collected);
public record DueRow(Customer Customer, int Bills, decimal Due, DateTime? Oldest, bool Overdue);
/// <summary>One bar pair of the accounts "collections vs dues" chart</summary>
public record CollectionPoint(string Label, decimal Collected, decimal Dues);
/// <summary>One bar of the admin "connections by plan" chart</summary>
public record PlanCount(string Name, string Category, string Type, int Count);

public class RegisterVm
{
    public string Name { get; set; } = "";
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string Confirm { get; set; } = "";
    public string Phone { get; set; } = "";
    public string? Email { get; set; }
    public string Address { get; set; } = "";
    public string City { get; set; } = "";
    public string? OrderNo { get; set; }
    public string? Error { get; set; }
}

public class AccountVm
{
    public string Name { get; set; } = "";
    public string Username { get; set; } = "";
    public string Role { get; set; } = "";
    public string? Scope { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Designation { get; set; }
    public string? EmpCode { get; set; }
    public DateTime? Since { get; set; }
    /// <summary>Profile picture (customers.ImageUrl or employees.ImageUrl) and which record owns it, for /api/images</summary>
    public string? ImageUrl { get; set; }
    public string? ImageResource { get; set; }
    public int ImageRecordId { get; set; }
}

public class ErrorVm { public int Code { get; set; } = 500; }

public class LoginVm
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string? Error { get; set; }
}

/// <summary>"Forgot password" page: what was typed and what happened</summary>
public class ForgotVm
{
    public string Identifier { get; set; } = "";
    public string? Error { get; set; }
    public bool Done { get; set; }
    /// <summary>Demo mode (no SMTP): the reset link is shown on screen instead of being e-mailed</summary>
    public string? Link { get; set; }
    public string? MaskedEmail { get; set; }
    public int ExpiryMinutes { get; set; } = 30;
}

/// <summary>"Reset password" page (opened from the link)</summary>
public class ResetVm
{
    public string Token { get; set; } = "";
    /// <summary>The link is real, unused and not expired</summary>
    public bool Valid { get; set; }
    public string? Username { get; set; }
    public string Password { get; set; } = "";
    public string Confirm { get; set; } = "";
    public string? Error { get; set; }
    public bool Done { get; set; }
}

public class TrackVm
{
    public string Kind { get; set; } = "none";   // none | order | connection | list | error
    public string? Error { get; set; }
    public string Q { get; set; } = "";
    public bool Advanced { get; set; }
    public Order? Order { get; set; }
    public Connection? Connection { get; set; }          // linked connection (kind=order) or the account itself (kind=connection)
    public decimal Due { get; set; }
    public int OpenBills { get; set; }
    public List<Bill> RecentBills { get; set; } = new();
    public Payment? LastPayment { get; set; }
    public List<Order> Results { get; set; } = new();
    public List<Connection> ConnResults { get; set; } = new();
    /// <summary>signed-in staff see full customer details in search results; the public sees masked values</summary>
    public bool IsStaff { get; set; }
}

public class AdminVm
{
    public int LiveConnections { get; set; }
    public int OrdersCount { get; set; }
    public int PendingOrders { get; set; }
    public decimal Collected { get; set; }
    public decimal Outstanding { get; set; }
    public int StockUnits { get; set; }
    public List<Employee> Employees { get; set; } = new();
    public List<Vendor> Vendors { get; set; } = new();
    public List<Equipment> Equipment { get; set; } = new();
    public List<RetailShop> Shops { get; set; } = new();
    public List<Plan> Plans { get; set; } = new();
    public List<Order> RecentOrders { get; set; } = new();
    public List<Customer> Customers { get; set; } = new();
    public List<Order> AllOrders { get; set; } = new();
    public List<Connection> AllConnections { get; set; } = new();
    public List<Bill> AllBills { get; set; } = new();
    public List<Payment> AllPayments { get; set; } = new();
    public List<Feedback> Feedbacks { get; set; } = new();
    public Dictionary<string, int> OrderStatusCounts { get; set; } = new();
    public Dictionary<string, int> ConnTypeCounts { get; set; } = new();
    public List<MonthPoint> Monthly { get; set; } = new();
    public List<PlanCount> PlanConnections { get; set; } = new();
    public int OpenDemandModems { get; set; }   // dial-up + broadband lines waiting for hardware
    public int OpenDemandPhones { get; set; }   // landline lines waiting for hardware
    public double AvgRating { get; set; }
    public List<ContactMessage> Messages { get; set; } = new();
    public List<Faq> Faqs { get; set; } = new();
    public int UnreadMessages { get; set; }
}

public class AccountsVm
{
    public int Invoices { get; set; }
    public int Open { get; set; }
    public decimal Outstanding { get; set; }
    public decimal Collected { get; set; }
    public decimal AtCounter { get; set; }
    public decimal AtOffice { get; set; }
    public decimal AtOnline { get; set; }
    public List<Connection> Billable { get; set; } = new();
    public List<Bill> Bills { get; set; } = new();
    public List<Payment> Payments { get; set; } = new();
    public List<DueRow> DueRows { get; set; } = new();
    public Dictionary<int, decimal> DueByConnection { get; set; } = new();
    public int Overdue { get; set; }
    public List<MonthPoint> Monthly { get; set; } = new();
    public List<CollectionPoint> CollectionsVsDues { get; set; } = new();
}

public class TechnicalVm
{
    public int Pending { get; set; }
    public int Feasible { get; set; }
    public int Active { get; set; }
    public int Inactive { get; set; }
    public int LowStock { get; set; }
    public List<Order> PendingOrders { get; set; } = new();
    public List<Order> FeasibleOrders { get; set; } = new();
    public List<Connection> Connections { get; set; } = new();
    public List<Equipment> Equipment { get; set; } = new();
    public List<Order> AllOrders { get; set; } = new();
    public List<Plan> Plans { get; set; } = new();
    public List<Vendor> Vendors { get; set; } = new();
}

public class RetailVm
{
    public int Orders { get; set; }
    public int Pending { get; set; }
    public int Connected { get; set; }
    public decimal Collected { get; set; }
    public List<Customer> Customers { get; set; } = new();
    public List<Plan> Plans { get; set; } = new();
    public List<Order> ShopOrders { get; set; } = new();
    public List<Connection> Connections { get; set; } = new();
    public List<Bill> Bills { get; set; } = new();
    public List<Payment> Payments { get; set; } = new();
}

public class PortalVm
{
    public Customer? Customer { get; set; }
    public int Connections { get; set; }
    public int Live { get; set; }
    public int Orders { get; set; }
    public int PendingOrders { get; set; }
    public decimal Dues { get; set; }
    public List<Order> OrderList { get; set; } = new();
    public List<Connection> ConnectionList { get; set; } = new();
    public List<Bill> Bills { get; set; } = new();
    public List<Plan> Plans { get; set; } = new();
    /// <summary>Open bills that are overdue or due within BillReminder.WarnDays days (oldest due date first) - drives the reminder banner</summary>
    public List<Bill> DueReminders { get; set; } = new();
}

/* ------------- JSON request bodies for /api endpoints ------------- */

public class NewCustomerDto
{
    public string? Name { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? CityCode { get; set; }
    public string? IdProof { get; set; }
    public string? AltPhone { get; set; }
}

public class ContactRequest
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Message { get; set; }
    /// <summary>hidden honeypot field - real visitors leave it empty</summary>
    public string? Website { get; set; }
}

public class PlaceOrderRequest
{
    public string? ConnectionType { get; set; }
    public int? PlanId { get; set; }
    public int? Quantity { get; set; }
    public bool? Corporate { get; set; }
    public int? CustomerId { get; set; }
    public NewCustomerDto? Customer { get; set; }
}

public class OrderStatusRequest { public int Id { get; set; } public string? Status { get; set; } public string? Remark { get; set; } }
public class ProvisionRequest { public int OrderId { get; set; } public int? EquipmentId { get; set; } }
public class ConnectionStatusRequest { public int Id { get; set; } public string? Status { get; set; } public int? PlanId { get; set; } }

public class GenerateBillRequest
{
    public int ConnectionId { get; set; }
    public decimal? Rental { get; set; }
    public decimal? Usage { get; set; }
    public decimal? Discount { get; set; }
    public string? Period { get; set; }
}

public class RecordPaymentRequest
{
    public int BillId { get; set; }
    public decimal Amount { get; set; }
    public string? Method { get; set; }
    public string? Reference { get; set; }
}
