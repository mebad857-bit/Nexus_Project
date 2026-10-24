using System.ComponentModel.DataAnnotations;

namespace NexusCommunication.Models;

/* ---------- string constants (stored as text in SQL Server, same values as the original app) ---------- */

public static class AppRoles
{
    public const string Admin = "admin";
    public const string Accounts = "accounts";
    public const string Technical = "technical";
    public const string Retail = "retail";
    public const string Customer = "customer";
}

public static class ConnTypes
{
    public const string Dialup = "dialup";
    public const string Broadband = "broadband";
    public const string Landline = "landline";
    public static readonly string[] All = { Dialup, Broadband, Landline };
}

public static class OrderStatus
{
    public const string Pending = "pending";
    public const string Feasible = "feasible";
    public const string Infeasible = "infeasible";
    public const string Connected = "connected";
    public const string Cancelled = "cancelled";
}

public static class ConnStatus
{
    public const string Active = "active";
    public const string TemporarilyInactive = "temporarily_inactive";
    public const string PermanentlyInactive = "permanently_inactive";
    public static readonly string[] All = { Active, TemporarilyInactive, PermanentlyInactive };
}

public static class BillStatus
{
    public const string Pending = "pending";
    public const string Partial = "partial";
    public const string Paid = "paid";
}

/* ------------------------------------ entities ------------------------------------ */

public class User
{
    public int Id { get; set; }
    [Required, StringLength(50)] public string Username { get; set; } = "";
    [Required, StringLength(300)] public string PasswordHash { get; set; } = "";
    [Required, StringLength(200)] public string Name { get; set; } = "";
    [Required, StringLength(20)] public string Role { get; set; } = AppRoles.Customer;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class RetailShop
{
    public int Id { get; set; }
    [Required, StringLength(30)] public string Code { get; set; } = "";
    [Required, StringLength(200)] public string Name { get; set; } = "";
    [Required, StringLength(100)] public string City { get; set; } = "";
    [Required, StringLength(400)] public string Address { get; set; } = "";
    [Required, StringLength(50)] public string Phone { get; set; } = "";
    [StringLength(200)] public string? Manager { get; set; }
    /// <summary>Web path of the uploaded / seeded picture (e.g. /uploads/products/abc.jpg or /images/products/x.jpg)</summary>
    [StringLength(300)] public string? ImageUrl { get; set; }
}

public class Employee
{
    public int Id { get; set; }
    [Required, StringLength(30)] public string EmpCode { get; set; } = "";
    public int? UserId { get; set; }
    public User? User { get; set; }
    [Required, StringLength(200)] public string Name { get; set; } = "";
    [StringLength(200)] public string Email { get; set; } = "";
    [StringLength(50)] public string Phone { get; set; } = "";
    [Required, StringLength(30), RegularExpression("^(admin|accounts|technical|retail)$", ErrorMessage = "Invalid department")]
    public string Department { get; set; } = "";
    [Required, StringLength(100)] public string Designation { get; set; } = "";
    public int? ShopId { get; set; }
    public RetailShop? Shop { get; set; }
    [Required, StringLength(20), RegularExpression("^(active|inactive)$", ErrorMessage = "Invalid status")]
    public string Status { get; set; } = "active";
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    /// <summary>Web path of the uploaded / seeded picture (e.g. /uploads/products/abc.jpg or /images/products/x.jpg)</summary>
    [StringLength(300)] public string? ImageUrl { get; set; }
}

public class Vendor
{
    public int Id { get; set; }
    [Required, StringLength(200)] public string Name { get; set; } = "";
    [Required, StringLength(200)] public string ContactPerson { get; set; } = "";
    [Required, StringLength(200)] public string Email { get; set; } = "";
    [Required, StringLength(50)] public string Phone { get; set; } = "";
    [Required, StringLength(400)] public string Address { get; set; } = "";
    [Required, StringLength(400)] public string Products { get; set; } = "";
    /// <summary>Web path of the uploaded / seeded picture (e.g. /uploads/products/abc.jpg or /images/products/x.jpg)</summary>
    [StringLength(300)] public string? ImageUrl { get; set; }
}

public class Equipment
{
    public int Id { get; set; }
    [Required, StringLength(200)] public string Name { get; set; } = "";
    [Required, StringLength(30), RegularExpression("^(modem|router|telephone|cabling)$", ErrorMessage = "Invalid equipment type")]
    public string Type { get; set; } = "";
    public int? VendorId { get; set; }
    public Vendor? Vendor { get; set; }
    [Range(0, 10000000)] public decimal UnitPrice { get; set; }
    [Range(0, int.MaxValue)] public int Stock { get; set; }
    [Range(0, int.MaxValue)] public int ReorderLevel { get; set; } = 10;
    /// <summary>Web path of the uploaded / seeded picture (e.g. /uploads/products/abc.jpg or /images/products/x.jpg)</summary>
    [StringLength(300)] public string? ImageUrl { get; set; }
}

public class Plan
{
    public int Id { get; set; }
    [Required, StringLength(20), RegularExpression("^(dialup|broadband|landline)$", ErrorMessage = "Invalid connection type")]
    public string ConnectionType { get; set; } = "";
    [Required, StringLength(100)] public string Category { get; set; } = "";
    [Required, StringLength(100)] public string Name { get; set; } = "";
    [StringLength(50)] public string? Speed { get; set; }
    [Range(0, 10000000)] public decimal Price { get; set; }
    [Required, StringLength(50)] public string ValidityLabel { get; set; } = "";
    [Range(1, 240)] public int ValidityMonths { get; set; } = 1;
    [Range(0, 10000000)] public decimal Deposit { get; set; }
    [StringLength(400)] public string? CallRates { get; set; }
    [Required, StringLength(1000)] public string Description { get; set; } = "";
    public bool Active { get; set; } = true;
    /// <summary>Web path of the uploaded / seeded picture (e.g. /uploads/products/abc.jpg or /images/products/x.jpg)</summary>
    [StringLength(300)] public string? ImageUrl { get; set; }
}

public class Customer
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public User? User { get; set; }
    [Required, StringLength(200)] public string Name { get; set; } = "";
    [StringLength(200)] public string? Email { get; set; }
    [Required, StringLength(50)] public string Phone { get; set; } = "";
    [Required, StringLength(400)] public string Address { get; set; } = "";
    [Required, StringLength(100)] public string City { get; set; } = "";
    [Required, StringLength(10)] public string CityCode { get; set; } = "101";
    [StringLength(200)] public string? IdProof { get; set; }
    [StringLength(50)] public string? AltPhone { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>Web path of the uploaded / seeded picture (e.g. /uploads/products/abc.jpg or /images/products/x.jpg)</summary>
    [StringLength(300)] public string? ImageUrl { get; set; }
}

public class Order
{
    public int Id { get; set; }
    /// <summary>Public tracking id, e.g. D0000000001 (letter + 10 digits)</summary>
    [Required, StringLength(20)] public string OrderNo { get; set; } = "";
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    [Required, StringLength(20)] public string ConnectionType { get; set; } = "";
    public int? PlanId { get; set; }
    public Plan? Plan { get; set; }
    public int Quantity { get; set; } = 1;
    public bool Corporate { get; set; }
    public int DiscountPct { get; set; }
    [Required, StringLength(20)] public string Status { get; set; } = OrderStatus.Pending;
    [StringLength(1000)] public string? Remark { get; set; }
    public int? ShopId { get; set; }
    public RetailShop? Shop { get; set; }
    [StringLength(200)] public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Connection
{
    public int Id { get; set; }
    /// <summary>Account id, e.g. B101000000000001 (letter + 3 digit city + 12 digit serial)</summary>
    [Required, StringLength(30)] public string AccountId { get; set; } = "";
    public int? OrderId { get; set; }
    public Order? Order { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public int? PlanId { get; set; }
    public Plan? Plan { get; set; }
    [Required, StringLength(20)] public string ConnectionType { get; set; } = "";
    [Required, StringLength(100)] public string City { get; set; } = "";
    [Required, StringLength(10)] public string CityCode { get; set; } = "";
    public int? EquipmentId { get; set; }
    public Equipment? Equipment { get; set; }
    [Required, StringLength(30)] public string Status { get; set; } = ConnStatus.Active;
    public DateTime ActivatedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Bill
{
    public int Id { get; set; }
    [Required, StringLength(30)] public string BillNo { get; set; } = "";
    public int ConnectionId { get; set; }
    public Connection Connection { get; set; } = null!;
    [Required, StringLength(50)] public string Period { get; set; } = "";
    public decimal Rental { get; set; }
    public decimal Usage { get; set; }
    public decimal Tax { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal DueAmount { get; set; }
    [Required, StringLength(20)] public string Status { get; set; } = BillStatus.Pending;
    public DateTime? DueDate { get; set; }
    [StringLength(200)] public string? GeneratedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Timestamp] public byte[]? RowVersion { get; set; }
    public List<Payment> Payments { get; set; } = new();
}

public class Payment
{
    public int Id { get; set; }
    public int BillId { get; set; }
    public Bill Bill { get; set; } = null!;
    public decimal Amount { get; set; }
    [Required, StringLength(20)] public string Method { get; set; } = "cash";
    [StringLength(50)] public string? Reference { get; set; }
    [Required, StringLength(20)] public string Channel { get; set; } = "retail";
    [StringLength(200)] public string? ReceivedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Feedback
{
    public int Id { get; set; }
    [Required, StringLength(200)] public string Name { get; set; } = "";
    [StringLength(200)] public string? Contact { get; set; }
    [StringLength(20), RegularExpression("^(dialup|broadband|landline)$", ErrorMessage = "Invalid connection type")]
    public string? ConnectionType { get; set; }
    [Range(1, 5)] public int Rating { get; set; } = 5;
    [Required, StringLength(4000)] public string Message { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Question / answer pair shown on the public FAQ page; managed from the admin console.</summary>
public class Faq
{
    public int Id { get; set; }
    [Required, StringLength(300)] public string Question { get; set; } = "";
    [Required, StringLength(2000)] public string Answer { get; set; } = "";
    [Range(0, 100000)] public int SortOrder { get; set; } = 100;
    public bool Active { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Message sent through the public Contact us form; the admin "Messages" tab shows it with read / unread state.</summary>
public class ContactMessage
{
    public int Id { get; set; }
    [Required, StringLength(200)] public string Name { get; set; } = "";
    [Required, StringLength(200)] public string Email { get; set; } = "";
    [Required, StringLength(4000)] public string Message { get; set; } = "";
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/* ---------------------------- notifications + audit log ---------------------------- */

public static class NotifType
{
    public const string Order = "order";
    public const string Bill = "bill";
    public const string Payment = "payment";
    public const string Complaint = "complaint";
    public const string Ticket = "ticket";
    public const string Info = "info";
}

/// <summary>
/// One "forgot password" link. Only the SHA-256 hash of the token is stored (the real token lives only in the link),
/// so a copy of the database cannot be used to reset anybody's password. UserId is deliberately not a foreign key.
/// </summary>
public class PasswordResetToken
{
    public int Id { get; set; }
    public int UserId { get; set; }
    [Required, StringLength(64)] public string TokenHash { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
    /// <summary>Set when the link has been used - a link works only once</summary>
    public DateTime? UsedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [StringLength(45)] public string? RequestIp { get; set; }
}

/// <summary>
/// One alert for ONE person (the bell). Every event creates a row per recipient, so each person has their own
/// read / unread state: a customer only ever gets their own alerts, staff only the alerts meant for their role.
/// UserId is deliberately not a foreign key - deleting an employee must never be blocked by old alerts.
/// </summary>
public class Notification
{
    public int Id { get; set; }
    public int UserId { get; set; }
    [Required, StringLength(30)] public string Type { get; set; } = NotifType.Info;
    [Required, StringLength(200)] public string Title { get; set; } = "";
    [Required, StringLength(1000)] public string Message { get; set; } = "";
    /// <summary>Same-site page (and tab) to open when the alert is clicked, e.g. /portal#bills</summary>
    [StringLength(300)] public string? Link { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/* ---------------------------- complaint / ticket system ---------------------------- */

public static class TicketStatus
{
    public const string Open = "open";
    public const string Assigned = "assigned";
    public const string InProgress = "in_progress";
    public const string Resolved = "resolved";
    public const string Closed = "closed";
    public static readonly string[] All = { Open, Assigned, InProgress, Resolved, Closed };
    public static string Label(string s) => s switch
    {
        Open => "Open", Assigned => "Assigned", InProgress => "In progress", Resolved => "Resolved", Closed => "Closed", _ => s,
    };
}

public static class TicketCategories
{
    public static readonly string[] All = { "Internet / connectivity", "Landline / call quality", "Billing & payments", "Installation / equipment", "Account & profile", "Other" };
}

/// <summary>
/// A customer complaint. Open -> Assigned (admin picks a technical-desk employee) -> In progress -> Resolved (technical desk) -> Closed
/// (customer confirms, or admin closes). The customer sees the status and every reply in the portal.
/// </summary>
public class Ticket
{
    public int Id { get; set; }
    /// <summary>Readable id, e.g. TK-000012</summary>
    [Required, StringLength(20)] public string TicketNo { get; set; } = "";
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    [Required, StringLength(60)] public string Category { get; set; } = "Other";
    [Required, StringLength(200)] public string Subject { get; set; } = "";
    [Required, StringLength(4000)] public string Description { get; set; } = "";
    /// <summary>Optional photo of the problem (/uploads/tickets/...)</summary>
    [StringLength(300)] public string? ImageUrl { get; set; }
    [Required, StringLength(20)] public string Status { get; set; } = TicketStatus.Open;
    /// <summary>Technical-desk employee who works on it</summary>
    public int? AssignedEmployeeId { get; set; }
    public Employee? AssignedEmployee { get; set; }
    public DateTime? AssignedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public List<TicketReply> Replies { get; set; } = new();
}

/// <summary>One message in a ticket's conversation (customer, technical desk or admin). Author name / role are copied in so the thread survives account changes.</summary>
public class TicketReply
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;
    public int? AuthorUserId { get; set; }
    [StringLength(200)] public string AuthorName { get; set; } = "";
    [StringLength(20)] public string AuthorRole { get; set; } = "";
    [Required, StringLength(2000)] public string Message { get; set; } = "";
    [StringLength(300)] public string? ImageUrl { get; set; }
    /// <summary>True for automatic lines like "Assigned to Rohan" or "Marked resolved" (shown as a timeline note, not a chat bubble)</summary>
    public bool IsSystem { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/* ---------------------------- demo online payment ---------------------------- */

public static class OnlineMethods
{
    public const string Card = "card";
    public const string JazzCash = "jazzcash";
    public const string Easypaisa = "easypaisa";
    public static readonly string[] All = { Card, JazzCash, Easypaisa };
    public static string Label(string m) => m switch { Card => "Card", JazzCash => "JazzCash", Easypaisa => "Easypaisa", _ => m };
}

/// <summary>
/// One attempt of the DEMO online payment (success or failure). No real money moves and NO card / wallet / PIN data is ever sent to the
/// server or stored - only which method the customer picked. A successful attempt also creates a normal Payment row
/// (Channel = "online", Reference = TransactionId), so the accounts desk, invoice and exports show it like any other receipt.
/// </summary>
public class OnlinePayment
{
    public int Id { get; set; }
    /// <summary>Readable id shown to the customer and printed on the receipt, e.g. DEMO-20261014-A1B2C3</summary>
    [Required, StringLength(40)] public string TransactionId { get; set; } = "";
    public int BillId { get; set; }
    public Bill Bill { get; set; } = null!;
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    [Required, StringLength(20)] public string Method { get; set; } = OnlineMethods.Card;
    public decimal Amount { get; set; }
    /// <summary>success | failed</summary>
    [Required, StringLength(10)] public string Status { get; set; } = "success";
    [StringLength(200)] public string? FailureReason { get; set; }
    /// <summary>The Payment row created on success</summary>
    public int? PaymentId { get; set; }
    public Payment? Payment { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Who did what, and when. User name / role are copied in, so the trail survives deleting the account.</summary>
public class AuditLog
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    [StringLength(200)] public string UserName { get; set; } = "";
    [StringLength(20)] public string Role { get; set; } = "";
    /// <summary>create | update | delete | status | payment | image | login | logout</summary>
    [Required, StringLength(30)] public string Action { get; set; } = "";
    /// <summary>What kind of record, e.g. Plan, Employee, Order</summary>
    [Required, StringLength(50)] public string Entity { get; set; } = "";
    [StringLength(50)] public string? EntityId { get; set; }
    /// <summary>Readable description including old -> new values for edits</summary>
    [StringLength(1500)] public string Summary { get; set; } = "";
    [StringLength(45)] public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public static class AuditAction
{
    public const string Create = "create";
    public const string Update = "update";
    public const string Delete = "delete";
    public const string Status = "status";
    public const string Payment = "payment";
    public const string Image = "image";
    public const string Login = "login";
    public const string Logout = "logout";
    public static readonly string[] All = { Create, Update, Delete, Status, Payment, Image, Login, Logout };
}
