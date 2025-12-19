using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using venta_stock_webapi.Sale.DTO;
using venta_stock_webapi.Sale.Message;
using venta_stock_webapi.Sale.Services;
using venta_stock_webapi.Shared.MessageProvider;

namespace venta_stock_webapi.Sale.Controllers
{
    /// <summary>
    /// Controlador para gestión de ventas pendientes de autorización
    /// Exclusivo para administradores con permiso SALE_AUTHORIZE
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "PERM:SALE_AUTHORIZE")]
    public class PendingSaleController : ControllerBase
    {
        private readonly IPendingSaleService _pendingSaleService;
        private readonly ILogger<PendingSaleController> _logger;

        public PendingSaleController(
            IPendingSaleService pendingSaleService,
            ILogger<PendingSaleController> logger)
        {
            _pendingSaleService = pendingSaleService;
            _logger = logger;
        }

        /// <summary>
        /// Listar todas las ventas PENDIENTES (estado = 1)
        /// </summary>
        /// <returns>Lista de ventas pendientes sin autorizar</returns>
        [HttpGet]
        public async Task<IActionResult> GetPendingSales()
        {
            _logger.LogInformation("Listando ventas pendientes (estado=1)");

            var result = await _pendingSaleService.GetPendingSalesAsync(1);  // 1 = Pendiente

            if (!result.IsSuccess)
            {
                var code = (SaleErrorCode)result.ErrorCode;
                var errorMessage = MessageProvider.Get(SaleErrorDictionary.Messages, code);
                return BadRequest(errorMessage);
            }

            _logger.LogInformation("Ventas pendientes obtenidas: {count}", result.Value.Count);

            return Ok(result.Value);
        }

        /// <summary>
        /// Obtener detalle completo de una venta pendiente
        /// </summary>
        /// <param name="id">ID de la venta pendiente</param>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetPendingSaleById(int id)
        {
            _logger.LogInformation("Obteniendo detalle de venta pendiente: {id}", id);

            var result = await _pendingSaleService.GetPendingSaleByIdAsync(id);

            if (!result.IsSuccess)
            {
                var code = (SaleErrorCode)result.ErrorCode;
                var errorMessage = MessageProvider.Get(SaleErrorDictionary.Messages, code);

                _logger.LogWarning("Venta pendiente {id} no encontrada", id);
                return NotFound(errorMessage);
            }

            _logger.LogInformation("Detalle de venta pendiente {id} obtenido", id);

            return Ok(result.Value);
        }

        /// <summary>
        /// Aprobar una venta pendiente
        /// Crea la venta definitiva, actualiza stock y registra movimiento en CC
        /// </summary>
        /// <param name="id">ID de la venta pendiente</param>
        /// <param name="dto">Observaciones de la aprobación (opcional)</param>
        [HttpPost("{id:int}/approve")]
        public async Task<IActionResult> ApproveSale(
            int id,
            [FromBody] ApproveRejectDTO dto)
        {
            // Obtener ID del usuario desde el token JWT
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int idUsuarioAutoriza))
            {
                _logger.LogWarning("No se pudo obtener ID del usuario desde token");
                return Unauthorized("No se pudo obtener el ID del usuario autenticado");
            }

            _logger.LogInformation(
                "Usuario {userId} aprobando venta pendiente {id}",
                idUsuarioAutoriza, id
            );

            var authorizeDto = new AuthorizeSaleDTO
            {
                IdVentaPendiente = id,
                Aprobar = true,
                Observaciones = dto.Observaciones
            };

            var result = await _pendingSaleService.AuthorizeSaleAsync(authorizeDto, idUsuarioAutoriza);

            if (!result.IsSuccess)
            {
                var code = (SaleErrorCode)result.ErrorCode;
                var errorMessage = MessageProvider.Get(SaleErrorDictionary.Messages, code);

                _logger.LogError(
                    "Error al aprobar venta pendiente {id}: {error}",
                    id, result.ErrorCode
                );

                return BadRequest(errorMessage);
            }

            _logger.LogInformation(
                "Venta pendiente {id} aprobada exitosamente. Venta generada: {ventaId} ({codigo})",
                id, result.Value.IdVentaGenerada, result.Value.CodigoVentaGenerada
            );

            return Ok(result.Value);
        }

        /// <summary>
        /// Rechazar una venta pendiente
        /// </summary>
        /// <param name="id">ID de la venta pendiente</param>
        /// <param name="dto">Motivo del rechazo (obligatorio)</param>
        [HttpPost("{id:int}/reject")]
        public async Task<IActionResult> RejectSale(
            int id,
            [FromBody] ApproveRejectDTO dto)
        {
            // Validar que las observaciones no estén vacías al rechazar
            if (string.IsNullOrWhiteSpace(dto.Observaciones))
            {
                _logger.LogWarning("Intento de rechazar venta {id} sin observaciones", id);
                return BadRequest("Las observaciones son obligatorias al rechazar una venta");
            }

            // Obtener ID del usuario desde el token JWT
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int idUsuarioAutoriza))
            {
                _logger.LogWarning("No se pudo obtener ID del usuario desde token");
                return Unauthorized("No se pudo obtener el ID del usuario autenticado");
            }

            _logger.LogInformation(
                "Usuario {userId} rechazando venta pendiente {id}",
                idUsuarioAutoriza, id
            );

            var authorizeDto = new AuthorizeSaleDTO
            {
                IdVentaPendiente = id,
                Aprobar = false,
                Observaciones = dto.Observaciones
            };

            var result = await _pendingSaleService.AuthorizeSaleAsync(authorizeDto, idUsuarioAutoriza);

            if (!result.IsSuccess)
            {
                var code = (SaleErrorCode)result.ErrorCode;
                var errorMessage = MessageProvider.Get(SaleErrorDictionary.Messages, code);

                _logger.LogError(
                    "Error al rechazar venta pendiente {id}: {error}",
                    id, result.ErrorCode
                );

                return BadRequest(errorMessage);
            }

            _logger.LogInformation("Venta pendiente {id} rechazada", id);

            return Ok(result.Value);
        }

        /// <summary>
        /// Obtener estadísticas de ventas pendientes
        /// Útil para dashboards y métricas administrativas
        /// </summary>
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            _logger.LogInformation("Obteniendo estadísticas de ventas pendientes");

            // Obtener todas las ventas pendientes (todas, no solo estado=1)
            var allResult = await _pendingSaleService.GetPendingSalesAsync(null);

            if (!allResult.IsSuccess)
            {
                var code = (SaleErrorCode)allResult.ErrorCode;
                var errorMessage = MessageProvider.Get(SaleErrorDictionary.Messages, code);
                return BadRequest(errorMessage);
            }

            var all = allResult.Value;
            var pending = all.Where(x => x.Estado == "Pendiente").ToList();
            var approved = all.Where(x => x.Estado == "Aprobada").ToList();
            var rejected = all.Where(x => x.Estado == "Rechazada").ToList();

            var stats = new
            {
                // Contadores
                total = all.Count,
                pendientes = pending.Count,
                aprobadas = approved.Count,
                rechazadas = rejected.Count,

                // Montos totales
                montoTotalPendiente = pending.Sum(x => x.Total),
                montoTotalAprobado = approved.Sum(x => x.Total),
                montoTotalRechazado = rejected.Sum(x => x.Total),

                // Métricas adicionales
                antiguasPendientes = pending.Where(x => x.DiasPendiente > 3).Count(),
                excedenteTotalPendiente = pending.Sum(x => x.Excedente),

                // Tasas de aprobación (evitar división por cero)
                tasaAprobacion = all.Count > 0
                    ? Math.Round((double)approved.Count / all.Count * 100, 2)
                    : 0,
                tasaRechazo = all.Count > 0
                    ? Math.Round((double)rejected.Count / all.Count * 100, 2)
                    : 0
            };

            _logger.LogInformation(
                "Stats: {total} total, {pending} pendientes, {approved} aprobadas, {rejected} rechazadas",
                stats.total, stats.pendientes, stats.aprobadas, stats.rechazadas
            );

            return Ok(stats);
        }
    }

    /// <summary>
    /// DTO simplificado para aprobar/rechazar ventas pendientes
    /// </summary>
    public class ApproveRejectDTO
    {
        /// <summary>
        /// Observaciones/motivo de la aprobación o rechazo
        /// Obligatorio al rechazar, opcional al aprobar
        /// </summary>
        public string? Observaciones { get; set; }
    }
}
