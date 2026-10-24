using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexusCommunication.Data;

namespace NexusCommunication.Controllers.Api;

[ApiController]
[AllowAnonymous]
public class HealthApiController : ControllerBase
{
    private readonly AppDbContext _db;
    public HealthApiController(AppDbContext db) => _db = db;

    [HttpGet("api/health")]
    public async Task<IActionResult> Get() =>
        await _db.Database.CanConnectAsync() ? Ok(new { ok = true }) : StatusCode(500, new { ok = false });
}
