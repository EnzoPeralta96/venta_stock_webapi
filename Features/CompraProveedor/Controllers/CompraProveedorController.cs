using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using proyecto_venta_stock.CompraProveedor.DTO;
using proyecto_venta_stock.CompraProveedor.Message;
using proyecto_venta_stock.CompraProveedor.Services;
using venta_stock_webapi.Shared.MessageProvider;

namespace proyecto_venta_stock.CompraProveedor.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
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

    [HttpPost("{idCompraProveedor:int}/anular")]
    public async Task<IActionResult> Anular(int idCompraProveedor, [FromBody] AnulacionCompraDTO dto)
    {
        var result = await _compraProveedorServices.Anular(idCompraProveedor, dto);

        if (!result.IsSuccess)
        {
            var code = (CompraProveedorErrorCode)result.ErrorCode;
            var mensaje = MessageProvider.Get(CompraProveedorErrorDictionary.Messages, code);
            if (code == CompraProveedorErrorCode.compra_not_found) return NotFound(mensaje);
            if (code == CompraProveedorErrorCode.compra_ya_inactiva) return Conflict(mensaje);
            return BadRequest(mensaje);
        }

        return NoContent();
    }

    [HttpGet("export/excel")]
    public async Task<IActionResult> ExportarExcel()
    {
        var result = await _compraProveedorServices.ExportarExcelAsync();

        if (!result.IsSuccess)
        {
            var code = (CompraProveedorErrorCode)result.ErrorCode;
            var mensaje = MessageProvider.Get(CompraProveedorErrorDictionary.Messages, code);
            return BadRequest(mensaje);
        }

        return File(result.Value,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"compras_{DateTime.Today:yyyyMMdd}.xlsx");
    }
}
