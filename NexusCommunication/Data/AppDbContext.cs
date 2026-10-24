using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NexusCommunication.Models;

namespace NexusCommunication.Data;

/// <summary>SQL Server returns DateTime with Kind=Unspecified; mark everything as UTC so JSON/JS get the right instant.</summary>
public class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter() : base(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc)) { }
}

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<RetailShop> RetailShops => Set<RetailShop>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<Equipment> Equipment => Set<Equipment>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Connection> Connections => Set<Connection>();
    public DbSet<Bill> Bills => Set<Bill>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Feedback> Feedbacks => Set<Feedback>();
    public DbSet<Faq> Faqs => Set<Faq>();
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<OnlinePayment> OnlinePayments => Set<OnlinePayment>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketReply> TicketReplies => Set<TicketReply>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        builder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        builder.Properties<decimal>().HavePrecision(12, 2);
    }

    protected override void OnModelCreating(ModelBuilder m)
    {
        // no cascading deletes anywhere (same behaviour as the original schema) - avoids SQL Server "multiple cascade paths" errors
        foreach (var fk in m.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            fk.DeleteBehavior = DeleteBehavior.Restrict;

        m.Entity<User>(e => { e.ToTable("Users"); e.HasKey(x => x.Id); e.HasIndex(x => x.Username).IsUnique(); });
        m.Entity<RetailShop>(e => { e.ToTable("RetailShops"); e.HasKey(x => x.Id); e.HasIndex(x => x.Code).IsUnique(); });
        m.Entity<Employee>(e => { e.ToTable("Employees"); e.HasKey(x => x.Id); e.HasIndex(x => x.EmpCode).IsUnique(); e.HasIndex(x => x.Department); });
        m.Entity<Vendor>(e => { e.ToTable("Vendors"); e.HasKey(x => x.Id); });
        m.Entity<Equipment>(e => { e.ToTable("Equipment"); e.HasKey(x => x.Id); });
        m.Entity<Plan>(e => { e.ToTable("Plans"); e.HasKey(x => x.Id); e.HasIndex(x => x.ConnectionType); });
        m.Entity<Customer>(e => { e.ToTable("Customers"); e.HasKey(x => x.Id); e.HasIndex(x => x.Phone); });
        m.Entity<Order>(e =>
        {
            e.ToTable("Orders"); e.HasKey(x => x.Id);
            e.HasIndex(x => x.OrderNo).IsUnique(); e.HasIndex(x => x.Status); e.HasIndex(x => x.CreatedAt);
        });
        m.Entity<Connection>(e =>
        {
            e.ToTable("Connections"); e.HasKey(x => x.Id);
            e.HasIndex(x => x.AccountId).IsUnique(); e.HasIndex(x => x.Status);
        });
        m.Entity<Bill>(e =>
        {
            e.ToTable("Bills"); e.HasKey(x => x.Id);
            e.HasIndex(x => x.BillNo).IsUnique(); e.HasIndex(x => x.Status);
            e.HasMany(x => x.Payments).WithOne(p => p.Bill).HasForeignKey(p => p.BillId);
        });
        m.Entity<Payment>(e => { e.ToTable("Payments"); e.HasKey(x => x.Id); });
        m.Entity<Feedback>(e => { e.ToTable("Feedback"); e.HasKey(x => x.Id); });
        m.Entity<Faq>(e => { e.ToTable("Faqs"); e.HasKey(x => x.Id); });
        m.Entity<ContactMessage>(e => { e.ToTable("ContactMessages"); e.HasKey(x => x.Id); e.HasIndex(x => x.IsRead); });
        m.Entity<Notification>(e =>
        {
            e.ToTable("Notifications"); e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.UserId, x.IsRead }); e.HasIndex(x => x.CreatedAt);
        });
        m.Entity<OnlinePayment>(e =>
        {
            e.ToTable("OnlinePayments"); e.HasKey(x => x.Id);
            e.HasIndex(x => x.TransactionId).IsUnique(); e.HasIndex(x => x.BillId); e.HasIndex(x => x.CustomerId);
            e.HasOne(x => x.Bill).WithMany().HasForeignKey(x => x.BillId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Payment).WithMany().HasForeignKey(x => x.PaymentId).OnDelete(DeleteBehavior.Restrict);
        });
        m.Entity<Ticket>(e =>
        {
            e.ToTable("Tickets"); e.HasKey(x => x.Id);
            e.HasIndex(x => x.TicketNo).IsUnique(); e.HasIndex(x => x.Status);
            e.HasIndex(x => x.CustomerId); e.HasIndex(x => x.AssignedEmployeeId);
            e.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.AssignedEmployee).WithMany().HasForeignKey(x => x.AssignedEmployeeId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Replies).WithOne(r => r.Ticket).HasForeignKey(r => r.TicketId).OnDelete(DeleteBehavior.Restrict);
        });
        m.Entity<TicketReply>(e => { e.ToTable("TicketReplies"); e.HasKey(x => x.Id); e.HasIndex(x => x.TicketId); });
        m.Entity<PasswordResetToken>(e =>
        {
            e.ToTable("PasswordResetTokens"); e.HasKey(x => x.Id);
            e.HasIndex(x => x.TokenHash).IsUnique(); e.HasIndex(x => x.UserId);
        });
        m.Entity<AuditLog>(e =>
        {
            e.ToTable("AuditLogs"); e.HasKey(x => x.Id);
            e.HasIndex(x => x.CreatedAt); e.HasIndex(x => x.UserId); e.HasIndex(x => x.Action);
        });
    }
}
