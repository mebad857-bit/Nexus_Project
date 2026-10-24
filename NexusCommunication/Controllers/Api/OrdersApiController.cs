using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusCommunication.Data;
using NexusCommunication.Models;
using NexusCommunication.Services;

namespace NexusCommunication.Controllers.Api;

[ApiController]
[Authorize]
[Route("api/orders")]
public class OrdersApiController : ApiBase
{
    private readonly AppDbContext _db;
    private readonly IdGenerator _ids;
    private readonly NotificationService _notify;
    private readonly AuditService _audit;
    public OrdersApiController(AppDbContext db, IdGenerator ids, NotificationService notify, AuditService audit)
    { _db = db; _ids = ids; _notify = notify; _audit = audit; }

    /// <summary>Place a new order - retail staff (own outlet), customers (self) or admin</summary>
    [HttpPost]
    public async Task<IActionResult> Place([FromBody] PlaceOrderRequest req)
    {
        var s = Session;
        if (s.Role is not (AppRoles.Retail or AppRoles.Customer or AppRoles.Admin))
            return Err(403, "Only retail outlet staff or customers can place orders");

        var type = req.ConnectionType ?? "";
        if (req.PlanId is int pid && pid > 0)
        {
            var plan = await _db.Plans.FindAsync(pid);
            if (plan == null || !plan.Active) return Err(400, "The selected plan is not available");
            type = plan.ConnectionType; // the plan decides the connection type
        }
        if (!ConnTypes.All.Contains(type)) return Err(400, "A valid connection type is required");

        var qty = Math.Clamp(req.Quantity ?? 1, 1, 10000);

        /* resolve the customer */
        int? customerId = null;
        Customer? newCustomer = null;
        if (s.Role == AppRoles.Customer)
        {
            var cust = await _db.Customers.FirstOrDefaultAsync(c => c.UserId == s.Id);
            if (cust == null) return Err(400, "Customer profile not found");
            customerId = cust.Id;
        }
        else if (req.CustomerId is int cid && cid > 0)
        {
            if (!await _db.Customers.AnyAsync(c => c.Id == cid)) return Err(404, "Customer not found");
            customerId = cid;
        }
        else
        {
            var c = req.Customer;
            if (c == null || Blank(c.Name) || Blank(c.Phone) || Blank(c.Address) || Blank(c.City))
                return Err(400, "Customer name, phone, address and city are required");
            var cityCode = Fmt.Cities.FirstOrDefault(x => x.Name == c.City).Code ?? c.CityCode ?? "101";
            newCustomer = new Customer
            {
                Name = c.Name!.Trim(), Phone = c.Phone!.Trim(), Address = c.Address!.Trim(), City = c.City!.Trim(),
                CityCode = cityCode,
                Email = Blank(c.Email) ? null : c.Email!.Trim(),
                IdProof = Blank(c.IdProof) ? null : c.IdProof!.Trim(),
                AltPhone = Blank(c.AltPhone) ? null : c.AltPhone!.Trim(),
            };
        }

        int? shopId = null;
        if (s.Role == AppRoles.Retail)
            shopId = (await _db.Employees.FirstOrDefaultAsync(e => e.UserId == s.Id))?.ShopId;

        var corporate = qty >= 10 || req.Corporate == true;
        var order = new Order
        {
            OrderNo = await _ids.NextOrderNoAsync(type),
            ConnectionType = type,
            PlanId = req.PlanId is > 0 ? req.PlanId : null,
            Quantity = qty,
            Corporate = corporate,
            DiscountPct = corporate ? Fmt.BulkDiscount(qty) : 0,
            Status = OrderStatus.Pending,
            ShopId = shopId,
            CreatedBy = s.Name,
        };
        if (newCustomer != null) order.Customer = newCustomer; else order.CustomerId = customerId!.Value;

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        var custName = newCustomer?.Name ?? (await _db.Customers.AsNoTracking().Where(c => c.Id == order.CustomerId).Select(c => c.Name).FirstOrDefaultAsync()) ?? "customer";
        await _audit.LogAsync(AuditAction.Create, "Order", order.OrderNo,
            $"Order {order.OrderNo} placed for {custName} ({Fmt.TypeLabel(order.ConnectionType)}, {order.Quantity} line{(order.Quantity == 1 ? "" : "s")})");
        await _notify.OrderPlacedAsync(order, custName);
        return Ok(new { ok = true, order = new { id = order.Id, orderId = order.OrderNo, status = order.Status } });
    }

    /// <summary>Feasibility decision - technical team / admin</summary>
    [HttpPatch]
    public async Task<IActionResult> UpdateStatus([FromBody] OrderStatusRequest req)
    {
        // customers may withdraw their own application while it is still waiting for the feasibility survey
        if (Session.Role == AppRoles.Customer)
        {
            if (req.Status != OrderStatus.Cancelled) return Err(403, "Customers can only cancel a pending request");
            var own = await _db.Orders.Include(o => o.Customer).FirstOrDefaultAsync(o => o.Id == req.Id);
            if (own == null || own.Customer.UserId != Session.Id) return Err(404, "Order not found");
            if (own.Status != OrderStatus.Pending) return Err(409, "Only requests that are still awaiting the survey can be cancelled");
            own.Status = OrderStatus.Cancelled;
            own.Remark = "Withdrawn by the customer";
            await _db.SaveChangesAsync();
            await _audit.LogAsync(AuditAction.Status, "Order", own.OrderNo, $"Order {own.OrderNo}: status {Fmt.OrderStatus(OrderStatus.Pending)} → {Fmt.OrderStatus(own.Status)} (withdrawn by the customer)");
            await _notify.OrderStatusAsync(own, AppRoles.Customer);
            return Ok(new { ok = true, order = new { id = own.Id, orderId = own.OrderNo, status = own.Status } });
        }

        if (Session.Role is not (AppRoles.Technical or AppRoles.Admin))
            return Err(403, "Only the technical team can update feasibility");

        var status = req.Status ?? "";
        if (req.Id <= 0 || status is not (OrderStatus.Feasible or OrderStatus.Infeasible or OrderStatus.Cancelled or OrderStatus.Pending))
            return Err(400, "Invalid update");

        var order = await _db.Orders.FindAsync(req.Id);
        if (order == null) return Err(404, "Order not found");
        if (order.Status is OrderStatus.Connected or OrderStatus.Cancelled)
            return Err(409, $"Order is already {order.Status} and cannot be changed");

        var oldStatus = order.Status;
        order.Status = status;
        if (req.Remark != null) order.Remark = req.Remark.Length > 1000 ? req.Remark[..1000] : req.Remark;
        await _db.SaveChangesAsync();
        if (oldStatus != status)
        {
            await _audit.LogAsync(AuditAction.Status, "Order", order.OrderNo,
                $"Order {order.OrderNo}: status {Fmt.OrderStatus(oldStatus)} → {Fmt.OrderStatus(status)}{(Blank(req.Remark) ? "" : $" — {req.Remark!.Trim()}")}");
            await _notify.OrderStatusAsync(order, Session.Role);
        }
        return Ok(new { ok = true, order = new { id = order.Id, orderId = order.OrderNo, status = order.Status } });
    }
}
