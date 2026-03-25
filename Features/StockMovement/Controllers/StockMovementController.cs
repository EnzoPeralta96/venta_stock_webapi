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
    /// El tipo debe ser activo y no de sistema. La cantidad es siempre positiva;
    /// el backend aplica el signo según EsPositivo del tipo seleccionado.
    /// </summary>
    [HttpPost("ajuste-manual")]
    [Authorize(Policy = "PERM:PROD_UPDATE")]
    public async Task<IActionResult> RegistrarAjuste([FromBody] AjusteStockDTO dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int idUsuario))
            return Unauthorized();

        var tipo = await _stockMovementService.GetTipoByIdAsync(dto.IdTipoMovimiento);

        if (tipo == null)
        {
            var code = StockMovementErrorCode.tipo_not_found;
            var message = MessageProvider.Get(StockMovementErrorDictionary.Messages, code);
            return NotFound(message);
        }

        if (tipo.EsSistema)
        {
            var code = StockMovementErrorCode.tipo_sistema_protegido;
            var message = MessageProvider.Get(StockMovementErrorDictionary.Messages, code);
            return BadRequest(message);
        }

        if (!tipo.Activo)
        {
            var code = StockMovementErrorCode.tipo_movimiento_invalido;
            var message = MessageProvider.Get(StockMovementErrorDictionary.Messages, code);
            return BadRequest(message);
        }

        var cantidadFinal = dto.Cantidad * (tipo.EsPositivo ? 1 : -1);

        var result = await _stockMovementService.RegistrarMovimientoAsync(
            dto.IdProducto,
            (TipoMovimientoStockEnum)dto.IdTipoMovimiento,
            cantidadFinal,
            dto.Motivo,
            idUsuario: idUsuario);

        if (!result.IsSuccess)
        {
            var code = (StockMovementErrorCode)result.ErrorCode;
            var message = MessageProvider.Get(StockMovementErrorDictionary.Messages, code);

            if (code is StockMovementErrorCode.producto_not_found) 
                return NotFound(message);

            return BadRequest(message);
        }

        return Ok(new { mensaje = "Ajuste de stock registrado correctamente." });
    }

    /// <summary>
    /// Retorna la lista de tipos de movimiento activos para uso en filtros/combos del frontend.
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
    /// Retorna todos los tipos de movimiento (incluye inactivos). Para uso administrativo.
    /// </summary>
    [HttpGet("tipos/admin")]
    [Authorize(Policy = "PERM:PROD_UPDATE")]
    public async Task<IActionResult> GetTiposAdmin()
    {
        var result = await _stockMovementService.GetTiposAdminAsync();

        if (!result.IsSuccess)
        {
            var code = (StockMovementErrorCode)result.ErrorCode;
            var message = MessageProvider.Get(StockMovementErrorDictionary.Messages, code);
            return BadRequest(message);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Crea un nuevo tipo de movimiento de stock.
    /// </summary>
    [HttpPost("tipos")]
    [Authorize(Policy = "PERM:PROD_UPDATE")]
    public async Task<IActionResult> CreateTipo([FromBody] CreateTipoMovimientoDTO dto)
    {
        var result = await _stockMovementService.CreateTipoMovimientoAsync(dto);

        if (!result.IsSuccess)
        {
            var code = (StockMovementErrorCode)result.ErrorCode;
            var message = MessageProvider.Get(StockMovementErrorDictionary.Messages, code);
            return Conflict(message);
        }

        return CreatedAtAction(nameof(GetTiposAdmin), result.Value);
    }

    /// <summary>
    /// Actualiza nombre y descripción de un tipo de movimiento. No aplica a tipos del sistema (IDs 1-4).
    /// </summary>
    [HttpPut("tipos/{id:int}")]
    [Authorize(Policy = "PERM:PROD_UPDATE")]
    public async Task<IActionResult> UpdateTipo(int id, [FromBody] UpdateTipoMovimientoDTO dto)
    {
        var result = await _stockMovementService.UpdateTipoMovimientoAsync(id, dto);

        if (!result.IsSuccess)
        {
            var code = (StockMovementErrorCode)result.ErrorCode;
            var message = MessageProvider.Get(StockMovementErrorDictionary.Messages, code);

            return code switch
            {
                StockMovementErrorCode.tipo_not_found      => NotFound(message),
                StockMovementErrorCode.tipo_sistema_protegido => Forbid(),
                _                                          => Conflict(message)
            };
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Activa o desactiva un tipo de movimiento. No aplica a tipos del sistema (IDs 1-4).
    /// </summary>
    [HttpPatch("tipos/{id:int}/toggle")]
    [Authorize(Policy = "PERM:PROD_UPDATE")]
    public async Task<IActionResult> ToggleTipo(int id)
    {
        var result = await _stockMovementService.ToggleTipoMovimientoAsync(id);

        if (!result.IsSuccess)
        {
            var code = (StockMovementErrorCode)result.ErrorCode;
            var message = MessageProvider.Get(StockMovementErrorDictionary.Messages, code);

            return code switch
            {
                StockMovementErrorCode.tipo_not_found         => NotFound(message),
                StockMovementErrorCode.tipo_sistema_protegido => Forbid(),
                _                                             => BadRequest(message)
            };
        }

        return Ok(new { activo = result.Value });
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

            if (code is StockMovementErrorCode.producto_not_found) 
                return NotFound(message);

            return BadRequest(message);
        }

        return Ok(result.Value);
    }
}
