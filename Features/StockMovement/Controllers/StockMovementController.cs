using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using proyecto_venta_stock.Models;
using venta_stock_webapi.Features.StockMovement.DTO;
using venta_stock_webapi.Features.StockMovement.Messages;
using venta_stock_webapi.Features.StockMovement.Services;
using venta_stock_webapi.Shared.MessageProvider;

namespace venta_stock_webapi.Features.StockMovement.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StockMovementController : ControllerBase
{
    private readonly IStockMovementService _stockMovementService;

    public StockMovementController(IStockMovementService stockMovementService)
    {
        _stockMovementService = stockMovementService;
    }

    /// <summary>
    /// Registra un ajuste manual de stock.
    /// Tipos válidos: 5 (AjustePositivoManual), 6 (AjusteNegativoManual), 7 (ConsumoInternoDueno).
    /// </summary>
    [HttpPost("ajuste-manual")]
    [Authorize(Policy = "PERM:PROD_UPDATE")]
    public async Task<IActionResult> RegistrarAjuste([FromBody] AjusteStockDTO dto)
    {
        var tipoMovimiento = (TipoMovimientoStockEnum)dto.IdTipoMovimiento;

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int idUsuario))
            return Unauthorized();

        var result = await _stockMovementService.RegistrarMovimientoAsync(
            dto.IdProducto,
            tipoMovimiento,
            dto.Cantidad,
            dto.Motivo,
            idUsuario: idUsuario);

        if (!result.IsSuccess)
        {
            var code = (StockMovementErrorCode)result.ErrorCode;
            var message = MessageProvider.Get(StockMovementErrorDictionary.Messages, code);

            if (code is StockMovementErrorCode.producto_not_found) return NotFound(message);

            return BadRequest(message);
        }

        return Ok(new { mensaje = "Ajuste de stock registrado correctamente." });
    }

    /// <summary>
    /// Retorna la lista de tipos de movimiento de stock para uso en filtros/combos del frontend.
    /// </summary>
    [HttpGet("tipos")]
    [Authorize(Policy = "PERM:PROD_READ")]
    public async Task<IActionResult> GetTipos()
    {
        var result = await _stockMovementService.GetTiposMovimientoAsync();

        if (!result.IsSuccess)
        {
            var code = (StockMovementErrorCode)result.ErrorCode;
            var message = MessageProvider.Get(StockMovementErrorDictionary.Messages, code);
            return BadRequest(message);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Retorna el historial de movimientos (Kardex) paginado para un producto.
    /// </summary>
    [HttpGet("producto/{idProducto}/movimientos")]
    [Authorize(Policy = "PERM:PROD_READ")]
    public async Task<IActionResult> GetMovimientos(
        int idProducto,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int? idTipoMovimiento = null)
    {
        int pageSize = 10;
        var result = await _stockMovementService.MovimientosPagedAsync(
            idProducto, pageIndex, pageSize, idTipoMovimiento);

        if (!result.IsSuccess)
        {
            var code = (StockMovementErrorCode)result.ErrorCode;
            var message = MessageProvider.Get(StockMovementErrorDictionary.Messages, code);

            if (code is StockMovementErrorCode.producto_not_found) return NotFound(message);

            return BadRequest(message);
        }

        return Ok(result.Value);
    }
}
