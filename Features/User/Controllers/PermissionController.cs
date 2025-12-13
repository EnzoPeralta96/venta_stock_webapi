using Microsoft.AspNetCore.Mvc;
using venta_stock_webapi.Shared.MessageProvider;
using venta_stock_webapi.User.Message;
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

        [HttpGet("permissions")]
        public async Task<IActionResult> GetPermissions([FromQuery] int? id_permissionCategory)
        {
            var result = await _permissionService.GetPermissions(id_permissionCategory);

            if (!result.IsSuccess)
            {
                var code = (PermissionErrorCode)result.ErrorCode;
                var errorMessage = MessageProvider.Get(PermissionErrorDictionary.Messages, code);
                return BadRequest(errorMessage);
            }

            return Ok(result.Value);
        }



    }
}