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

    [Authorize(Policy = "PERM:COMP_CREATE")]
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

    [Authorize(Policy = "PERM:COMP_READ")]
    [HttpGet]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? activo = "true",
        [FromQuery] string? fechaDesde = null,
        [FromQuery] string? fechaHasta = null)
    {
        bool? activoFilter = activo?.ToLower() switch
        {
            "true" => true,
            "false" => false,
            _ => null
        };

        DateOnly? desdeParsed = DateOnly.TryParse(fechaDesde, out var d1) ? d1 : null;
        DateOnly? hastaParsed = DateOnly.TryParse(fechaHasta, out var h1) ? h1 : null;

        var result = await _compraProveedorServices.GetPaged(pageIndex, pageSize, search, activoFilter, desdeParsed, hastaParsed);

        if (!result.IsSuccess)
        {
            var code = (CompraProveedorErrorCode)result.ErrorCode;
            var mensaje = MessageProvider.Get(CompraProveedorErrorDictionary.Messages, code);
            return BadRequest(mensaje);
        }

        return Ok(result.Value);
    }

    [Authorize(Policy = "PERM:COMP_READ")]
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

    [Authorize(Policy = "PERM:COMP_READ")]
    [HttpGet("proveedor/{idProveedor:int}")]
    public async Task<IActionResult> GetByProveedor(
        int idProveedor,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? activo = "true",
        [FromQuery] string? fechaDesde = null,
        [FromQuery] string? fechaHasta = null)
    {
        bool? activoFilter = activo?.ToLower() switch
        {
            "true" => true,
            "false" => false,
            _ => null
        };

        DateOnly? desdeParsed = DateOnly.TryParse(fechaDesde, out var d2) ? d2 : null;
        DateOnly? hastaParsed = DateOnly.TryParse(fechaHasta, out var h2) ? h2 : null;

        var result = await _compraProveedorServices.GetPagedByProveedor(idProveedor, pageIndex, pageSize, activoFilter, desdeParsed, hastaParsed);

        if (!result.IsSuccess)
        {
            var code = (CompraProveedorErrorCode)result.ErrorCode;
            var mensaje = MessageProvider.Get(CompraProveedorErrorDictionary.Messages, code);
            return BadRequest(mensaje);
        }

        return Ok(result.Value);
    }

    [Authorize(Policy = "PERM:COMP_DELETE")]
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

    [Authorize(Policy = "PERM:COMP_READ")]
    [HttpGet("{idCompraProveedor:int}/export/excel")]
    public async Task<IActionResult> ExportarCompraExcel(int idCompraProveedor)
    {
        var result = await _compraProveedorServices.ExportarCompraExcelAsync(idCompraProveedor);

        if (!result.IsSuccess)
        {
            var code = (CompraProveedorErrorCode)result.ErrorCode;
            var mensaje = MessageProvider.Get(CompraProveedorErrorDictionary.Messages, code);

            return code == CompraProveedorErrorCode.compra_not_found ? NotFound(mensaje) : BadRequest(mensaje);
        }

        return File(result.Value,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"compra_{idCompraProveedor}_{DateTime.Today:yyyyMMdd}.xlsx");
    }

    [Authorize(Policy = "PERM:COMP_READ")]
    [HttpGet("{idCompraProveedor:int}/export/pdf")]
    public async Task<IActionResult> ExportarCompraPdf(int idCompraProveedor)
    {
        var result = await _compraProveedorServices.ExportarCompraPdfAsync(idCompraProveedor);

        if (!result.IsSuccess)
        {
            var code = (CompraProveedorErrorCode)result.ErrorCode;
            var mensaje = MessageProvider.Get(CompraProveedorErrorDictionary.Messages, code);

            return code == CompraProveedorErrorCode.compra_not_found ? NotFound(mensaje) : BadRequest(mensaje);
        }

        return File(result.Value, "application/pdf", $"compra_{idCompraProveedor}_{DateTime.Today:yyyyMMdd}.pdf");
    }

    [Authorize(Policy = "PERM:COMP_READ")]
    [HttpGet("proveedor/{idProveedor:int}/export/excel")]
    public async Task<IActionResult> ExportarComprasPorProveedorExcel(
        int idProveedor,
        [FromQuery] string? fechaDesde = null,
        [FromQuery] string? fechaHasta = null)
    {
        DateOnly? desdeParsed = DateOnly.TryParse(fechaDesde, out var d1) ? d1 : null;
        DateOnly? hastaParsed = DateOnly.TryParse(fechaHasta, out var h1) ? h1 : null;

        var result = await _compraProveedorServices.ExportarComprasPorProveedorExcelAsync(idProveedor, desdeParsed, hastaParsed);

        if (!result.IsSuccess)
        {
            var code = (CompraProveedorErrorCode)result.ErrorCode;
            var mensaje = MessageProvider.Get(CompraProveedorErrorDictionary.Messages, code);
            return BadRequest(mensaje);
        }

        return File(result.Value,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"compras_proveedor_{idProveedor}_{DateTime.Today:yyyyMMdd}.xlsx");
    }

    [Authorize(Policy = "PERM:COMP_READ")]
    [HttpGet("proveedor/{idProveedor:int}/export/pdf")]
    public async Task<IActionResult> ExportarComprasPorProveedorPdf(
        int idProveedor,
        [FromQuery] string? fechaDesde = null,
        [FromQuery] string? fechaHasta = null)
    {
        DateOnly? desdeParsed = DateOnly.TryParse(fechaDesde, out var d2) ? d2 : null;
        DateOnly? hastaParsed = DateOnly.TryParse(fechaHasta, out var h2) ? h2 : null;

        var result = await _compraProveedorServices.ExportarComprasPorProveedorPdfAsync(idProveedor, desdeParsed, hastaParsed);

        if (!result.IsSuccess)
        {
            var code = (CompraProveedorErrorCode)result.ErrorCode;
            var mensaje = MessageProvider.Get(CompraProveedorErrorDictionary.Messages, code);
            return BadRequest(mensaje);
        }
        
        return File(result.Value, "application/pdf", $"compras_proveedor_{idProveedor}_{DateTime.Today:yyyyMMdd}.pdf");
    }

    [Authorize(Policy = "PERM:COMP_READ")]
    [HttpGet("export/excel")]
    public async Task<IActionResult> ExportarExcel(
        [FromQuery] string? fechaDesde = null,
        [FromQuery] string? fechaHasta = null)
    {
        DateOnly? desdeParsed = DateOnly.TryParse(fechaDesde, out var d3) ? d3 : null;
        DateOnly? hastaParsed = DateOnly.TryParse(fechaHasta, out var h3) ? h3 : null;

        var result = await _compraProveedorServices.ExportarExcelAsync(desdeParsed, hastaParsed);

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

    [Authorize(Policy = "PERM:COMP_READ")]
    [HttpGet("export/pdf")]
    public async Task<IActionResult> ExportarPdf(
        [FromQuery] string? fechaDesde = null,
        [FromQuery] string? fechaHasta = null)
    {
        DateOnly? desdeParsed = DateOnly.TryParse(fechaDesde, out var d4) ? d4 : null;
        DateOnly? hastaParsed = DateOnly.TryParse(fechaHasta, out var h4) ? h4 : null;

        var result = await _compraProveedorServices.ExportarPdfAsync(desdeParsed, hastaParsed);

        if (!result.IsSuccess)
        {
            var code = (CompraProveedorErrorCode)result.ErrorCode;
            var mensaje = MessageProvider.Get(CompraProveedorErrorDictionary.Messages, code);
            return BadRequest(mensaje);
        }

        return File(result.Value, "application/pdf", $"compras_{DateTime.Today:yyyyMMdd}.pdf");
    }
}
