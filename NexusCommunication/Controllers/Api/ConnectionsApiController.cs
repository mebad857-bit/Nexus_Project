using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusCommunication.Data;
using NexusCommunication.Models;
using NexusCommunication.Services;

namespace NexusCommunication.Controllers.Api;

[ApiController]
[Authorize]
[Route("api/connections")]
public class ConnectionsApiController : ApiBase
{
    private readonly AppDbContext _db;
    private readonly IdGenerator _ids;
    private readonly NotificationService _notify;
    private readonly AuditService _audit;
    public ConnectionsApiController(AppDbContext db, IdGenerator ids, NotificationService notify, AuditService audit)
    { _db = db; _ids = ids; _notify = notify; _audit = audit; }

    /// <summary>
    /// Provision connections from a feasible order (issues the 16-digit account id(s), issues equipment).
    /// A bulk / corporate order for N lines creates N separate connections - every line needs its own account id.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Provision([FromBody] ProvisionRequest req)
    {
        if (Session.Role is not (AppRoles.Technical or AppRoles.Admin))
            return Err(403, "Only the technical team can provision connections");
        if (req.OrderId <= 0) return Err(400, "orderId is required");

        await using var tx = await _db.Database.BeginTransactionAsync();

        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == req.OrderId);
        if (order == null) return Err(404, "Order not found");
        if (order.Status == OrderStatus.Connected) return Err(409, "Order already provisioned");
        if (order.Status != OrderStatus.Feasible) return Err(409, "Order must pass the feasibility check first");

        var cust = await _db.Customers.FirstOrDefaultAsync(c => c.Id == order.CustomerId);
        if (cust == null) return Err(404, "Customer record missing");

        var lines = Math.Max(1, order.Quantity);

        int? equipmentId = req.EquipmentId is > 0 ? req.EquipmentId : null;
        if (equipmentId != null)
        {
            var eqp = await _db.Equipment.FirstOrDefaultAsync(e => e.Id == equipmentId);
            if (eqp == null) return Err(404, "Equipment not found");
            if (eqp.Stock < lines)
                return Err(409, $"Insufficient stock — only {eqp.Stock} unit(s) available");

            // atomic, race-safe stock decrement
            var affected = await _db.Equipment.Where(e => e.Id == equipmentId && e.Stock >= lines)
                .ExecuteUpdateAsync(set => set.SetProperty(e => e.Stock, e => e.Stock - lines));
            if (affected == 0) return Err(409, "Insufficient stock — please refresh and try again");
        }

        var accountIds = await _ids.NextAccountIdsAsync(order.ConnectionType, cust.CityCode, lines);
        foreach (var accountId in accountIds)
        {
            _db.Connections.Add(new Connection
            {
                AccountId = accountId,
                OrderId = order.Id,
                CustomerId = cust.Id,
                PlanId = order.PlanId,
                ConnectionType = order.ConnectionType,
                City = cust.City,
                CityCode = cust.CityCode,
                EquipmentId = equipmentId,
                Status = ConnStatus.Active,
            });
        }
        order.Status = OrderStatus.Connected;
        order.Remark = lines == 1
            ? $"Connection {accountIds[0]} activated"
            : $"{lines} connections activated ({accountIds[0]} … {accountIds[^1]})";

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        await _audit.LogAsync(AuditAction.Create, "Connection", accountIds[0],
            lines == 1 ? $"Connection {accountIds[0]} provisioned from order {order.OrderNo}"
                       : $"{lines} connections provisioned from order {order.OrderNo} ({accountIds[0]} … {accountIds[^1]})");
        await _audit.LogAsync(AuditAction.Status, "Order", order.OrderNo, $"Order {order.OrderNo}: status {Fmt.OrderStatus(OrderStatus.Feasible)} → {Fmt.OrderStatus(OrderStatus.Connected)}");
        await _notify.OrderStatusAsync(order, Session.Role, lines == 1 ? $"Account ID {accountIds[0]}" : $"{lines} accounts activated");
        return Ok(new
        {
            ok = true,
            connection = new { accountId = accountIds[0], status = ConnStatus.Active },
            accounts = accountIds,
        });
    }

    /// <summary>Activate / put on hold / retire a connection, and/or move it to another plan of the same type</summary>
    [HttpPatch]
    public async Task<IActionResult> SetStatus([FromBody] ConnectionStatusRequest req)
    {
        if (Session.Role is not (AppRoles.Technical or AppRoles.Admin))
            return Err(403, "Only the technical team can change connection state");

        var hasStatus = !string.IsNullOrEmpty(req.Status);
        var hasPlan = req.PlanId is > 0;
        if (req.Id <= 0 || (!hasStatus && !hasPlan)) return Err(400, "Invalid update");
        if (hasStatus && !ConnStatus.All.Contains(req.Status!)) return Err(400, "Invalid connection status");

        var conn = await _db.Connections.FindAsync(req.Id);
        if (conn == null) return Err(404, "Connection not found");

        if (hasPlan)
        {
            var plan = await _db.Plans.FindAsync(req.PlanId!.Value);
            if (plan == null || !plan.Active) return Err(400, "The selected plan is not available");
            if (plan.ConnectionType != conn.ConnectionType)
                return Err(400, "A plan change must stay within the same connection type");
            if (conn.Status == ConnStatus.PermanentlyInactive)
                return Err(409, "A permanently inactive connection cannot change plan");
            conn.PlanId = plan.Id;
        }
        var oldConnStatus = conn.Status;
        if (hasStatus) conn.Status = req.Status!;

        await _db.SaveChangesAsync();
        var bits = new List<string>();
        if (hasStatus && oldConnStatus != conn.Status) bits.Add($"status {Fmt.ConnStatus(oldConnStatus)} → {Fmt.ConnStatus(conn.Status)}");
        if (hasPlan) bits.Add($"plan changed (plan #{req.PlanId})");
        if (bits.Count > 0)
            await _audit.LogAsync(AuditAction.Update, "Connection", conn.AccountId, $"Connection {conn.AccountId}: {string.Join("; ", bits)}");
        return Ok(new { ok = true, connection = new { id = conn.Id, accountId = conn.AccountId, status = conn.Status } });
    }
}
