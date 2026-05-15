using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace venta_stock_webapi.Shared.Controllers;

public abstract class BaseController : ControllerBase
{
    protected bool TryGetUserId(out int userId)
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return int.TryParse(claim?.Value, out userId);
    }
}
