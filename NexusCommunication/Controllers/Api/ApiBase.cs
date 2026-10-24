using Microsoft.AspNetCore.Mvc;
using NexusCommunication.Services;

namespace NexusCommunication.Controllers.Api;

public abstract class ApiBase : ControllerBase
{
    protected SessionUser Session => User.ToSession();
    protected IActionResult Err(int status, string message) => StatusCode(status, new { error = message });
    protected static bool Blank(string? s) => string.IsNullOrWhiteSpace(s);
}
