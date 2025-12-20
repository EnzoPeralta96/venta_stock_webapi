using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using proyecto_venta_stock.Message;
using venta_stock_webapi.Features.Audit.Services;
using venta_stock_webapi.Shared.MessageProvider;

namespace venta_stock_webapi.Features.Audit.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "PERM:HIS_VIEW")]
    public class AuditController : ControllerBase
    {
        private readonly IAuditService _auditService;
        public AuditController(IAuditService auditService)
        {
            _auditService = auditService;
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search(
            int pageIndex = 1,
            int pageSize = 10,
            string? searchTerm = "",
            string? accion = null,
            string? entidadTipo = null,
            string? entidadId = null,
            int? idUsuario = null,
            DateTimeOffset? from = null,
            DateTimeOffset? to = null
        )
        {
            var result = await _auditService.SearchAsync(pageIndex, pageSize, searchTerm, accion, entidadTipo, entidadId, idUsuario, from, to);

            if (!result.IsSuccess)
            {
                var code = (AuditErrorCode)result.ErrorCode;
                var errorMessage = MessageProvider.Get(AuditErrorDictionary.Messages, code);
                return NotFound(errorMessage);
            }

            return Ok(result.Value);
        }
    }
}