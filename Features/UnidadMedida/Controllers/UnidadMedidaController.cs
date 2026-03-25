using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using venta_stock_webapi.Features.UnidadMedida.DTO;
using venta_stock_webapi.Features.UnidadMedida.Messages;
using venta_stock_webapi.Features.UnidadMedida.Services;
using venta_stock_webapi.Shared.MessageProvider;

namespace venta_stock_webapi.Features.UnidadMedida.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UnidadMedidaController : ControllerBase
{
    private readonly IUnidadMedidaService _service;

    public UnidadMedidaController(IUnidadMedidaService service)
    {
        _service = service;
    }

    /// <summary>
    /// Retorna todas las unidades (activas e inactivas). Para la grilla de administración.
    /// </summary>
    [HttpGet("admin")]
    [Authorize(Policy = "PERM:PROD_UPDATE")]
    public async Task<IActionResult> GetAll()
    {
        var result = await _service.GetAllAsync();
        if (!result.IsSuccess)
        {
            var message = MessageProvider.Get(UnidadMedidaErrorDictionary.Messages, (UnidadMedidaErrorCode)result.ErrorCode);
            return BadRequest(message);
        }
        return Ok(result.Value);
    }

    /// <summary>
    /// Retorna solo las unidades activas. Para poblar selects en formularios de productos.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "PERM:PROD_READ")]
    public async Task<IActionResult> GetActivos()
    {
        var result = await _service.GetActivosAsync();
        if (!result.IsSuccess)
        {
            var message = MessageProvider.Get(UnidadMedidaErrorDictionary.Messages, (UnidadMedidaErrorCode)result.ErrorCode);
            return BadRequest(message);
        }
        return Ok(result.Value);
    }

    /// <summary>
    /// Crea una nueva unidad de medida.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "PERM:PROD_UPDATE")]
    public async Task<IActionResult> Create([FromBody] CreateUnidadMedidaDTO dto)
    {
        var result = await _service.CreateAsync(dto);
        if (!result.IsSuccess)
        {
            var code = (UnidadMedidaErrorCode)result.ErrorCode;
            var message = MessageProvider.Get(UnidadMedidaErrorDictionary.Messages, code);
            return Conflict(message);
        }
        return CreatedAtAction(nameof(GetAll), result.Value);
    }

    /// <summary>
    /// Actualiza nombre y abreviatura de una unidad de medida.
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = "PERM:PROD_UPDATE")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUnidadMedidaDTO dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        if (!result.IsSuccess)
        {
            var code = (UnidadMedidaErrorCode)result.ErrorCode;
            var message = MessageProvider.Get(UnidadMedidaErrorDictionary.Messages, code);
            return code switch
            {
                UnidadMedidaErrorCode.unidad_not_found => NotFound(message),
                _ => Conflict(message)
            };
        }
        return Ok(result.Value);
    }

    /// <summary>
    /// Activa o desactiva una unidad de medida.
    /// No se puede desactivar si hay productos usándola.
    /// </summary>
    [HttpPatch("{id:int}/toggle")]
    [Authorize(Policy = "PERM:PROD_UPDATE")]
    public async Task<IActionResult> Toggle(int id)
    {
        var result = await _service.ToggleActivoAsync(id);
        if (!result.IsSuccess)
        {
            var code = (UnidadMedidaErrorCode)result.ErrorCode;
            var message = MessageProvider.Get(UnidadMedidaErrorDictionary.Messages, code);
            return code switch
            {
                UnidadMedidaErrorCode.unidad_not_found => NotFound(message),
                UnidadMedidaErrorCode.unidad_en_uso    => Conflict(message),
                _ => BadRequest(message)
            };
        }
        return Ok(new { activo = result.Value });
    }
}
