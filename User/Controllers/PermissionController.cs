
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using venta_stock_webapi.User.Services;

namespace venta_stock_webapi.User.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PermissionController : ControllerBase
    {
        private readonly ILogger<PermissionController> _logger;
        private readonly IPermissionService _permissionService;

        public PermissionController(ILogger<PermissionController> logger, IPermissionService permissionService)
        {
            _logger = logger;
            _permissionService = permissionService;
        }

        [HttpGet("permissions/{id_categoria_permission}")]
        public async Task<IActionResult> GetPermissions(int id_categoria_permission)
        {
            var result = await _permissionService.GetPermissions(id_categoria_permission);

            if (!result.IsSucces) return BadRequest(result.ErrosMessage);

            return Ok(result.Value);
        }



    }
}