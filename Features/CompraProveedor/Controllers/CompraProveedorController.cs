using Microsoft.AspNetCore.Mvc;
using proyecto_venta_stock.CompraProveedor.DTO;
using proyecto_venta_stock.CompraProveedor.Message;
using proyecto_venta_stock.CompraProveedor.Services;
using venta_stock_webapi.Shared.MessageProvider;

namespace proyecto_venta_stock.CompraProveedor.Controllers;

[ApiController]
[Route("[controller]")]
public class CompraProveedorController : ControllerBase
{
    private readonly ICompraProveedorServices _compraProveedorServices;

    public CompraProveedorController(ICompraProveedorServices compraProveedorServices)
    {
        _compraProveedorServices = compraProveedorServices;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CompraProveedorCreateDTO dto)
    {
        var result = await _compraProveedorServices.Create(dto);

        if (!result.IsSuccess)
        {
            var code = (CompraProveedorErrorCode)result.ErrorCode;
            var mensaje = MessageProvider.Get(CompraProveedorErrorDictionary.Messages, code);
            return BadRequest(mensaje);
        }

        return Ok(result.Value);
    }

    [HttpPut("update")]
    public async Task<IActionResult> Update([FromBody] CompraProveedorUpdateDTO dto)
    {
        var result = await _compraProveedorServices.Update(dto);

        if (!result.IsSuccess)
        {
            var code = (CompraProveedorErrorCode)result.ErrorCode;
            var mensaje = MessageProvider.Get(CompraProveedorErrorDictionary.Messages, code);
            if (Convert.ToString(result.ErrorCode) == "compra_not_found") return NotFound(mensaje);
            return BadRequest(mensaje);
        }

        return Ok(result.Value);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _compraProveedorServices.GetAll();

        if (!result.IsSuccess)
        {
            var code = (CompraProveedorErrorCode)result.ErrorCode;
            var mensaje = MessageProvider.Get(CompraProveedorErrorDictionary.Messages, code);
            return BadRequest(mensaje);
        }

        return Ok(result.Value);
    }

    [HttpGet("with-details")]
    public async Task<IActionResult> GetAllWithDetails()
    {
        var result = await _compraProveedorServices.GetAllWithDetails();

        if (!result.IsSuccess)
        {
            var code = (CompraProveedorErrorCode)result.ErrorCode;
            var mensaje = MessageProvider.Get(CompraProveedorErrorDictionary.Messages, code);
            return BadRequest(mensaje);
        }

        return Ok(result.Value);
    }

    [HttpGet("{idCompraProveedor:int}")]
    public async Task<IActionResult> GetById(int idCompraProveedor)
    {
        var result = await _compraProveedorServices.GetById(idCompraProveedor);

        if (!result.IsSuccess)
        {
            var code = (CompraProveedorErrorCode)result.ErrorCode;
            var mensaje = MessageProvider.Get(CompraProveedorErrorDictionary.Messages, code);
            return NotFound(mensaje);
        }

        return Ok(result.Value);
    }

    [HttpGet("proveedor/{idProveedor:int}")]
    public async Task<IActionResult> GetByProveedor(int idProveedor)
    {
        var result = await _compraProveedorServices.GetByProveedor(idProveedor);

        if (!result.IsSuccess)
        {
            var code = (CompraProveedorErrorCode)result.ErrorCode;
            var mensaje = MessageProvider.Get(CompraProveedorErrorDictionary.Messages, code);
            return BadRequest(mensaje);
        }

        return Ok(result.Value);
    }

    [HttpDelete("{idCompraProveedor:int}")]
    public async Task<IActionResult> Delete(int idCompraProveedor)
    {
        var result = await _compraProveedorServices.Delete(idCompraProveedor);

        if (!result.IsSuccess)
        {
            var code = (CompraProveedorErrorCode)result.ErrorCode;
            var mensaje = MessageProvider.Get(CompraProveedorErrorDictionary.Messages, code);
            if (Convert.ToString(result.ErrorCode) == "compra_not_found") return NotFound(mensaje);
            if (Convert.ToString(result.ErrorCode) == "compra_ya_inactiva") return Conflict(mensaje);
            return BadRequest(mensaje);
        }

        return NoContent();
    }

    [HttpPatch("{idCompraProveedor:int}/toggle-estado")]
    public async Task<IActionResult> ToggleEstado(int idCompraProveedor)
    {
        var result = await _compraProveedorServices.ToggleEstado(idCompraProveedor);

        if (!result.IsSuccess)
        {
            var code = (CompraProveedorErrorCode)result.ErrorCode;
            var mensaje = MessageProvider.Get(CompraProveedorErrorDictionary.Messages, code);
            if (Convert.ToString(result.ErrorCode) == "compra_not_found") return NotFound(mensaje);
            return BadRequest(mensaje);
        }

        return Ok();
    }
}
