using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using proyecto_venta_stock.Message;
using proyecto_venta_stock.Proveedor.DTO;
using proyecto_venta_stock.Proveedor.Services;
using venta_stock_webapi.Shared.MessageProvider;

namespace proyecto_venta_stock.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class ProveedorController : ControllerBase
{
    private readonly IProveedorServices _proveedorServices;

    public ProveedorController(IProveedorServices proveedorServices)
    {
        _proveedorServices = proveedorServices;
    }

    [Authorize(Policy = "PERM:PROV_CREATE")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProveedorDTO proveedor)
    {
        var result = await _proveedorServices.Create(proveedor);

        if (!result.IsSuccess)
        {
            var code = (ProveedorErrorCode)result.ErrorCode;
            var message = MessageProvider.Get(ProveedorErrorDictionary.Messages, code);
            return BadRequest(message);
        }

        return Ok(proveedor);
    }

    [Authorize(Policy = "PERM:PROV_UPDATE")]
    [HttpPut("update")]
    public async Task<IActionResult> Update([FromBody] UpdateProveedorDTO proveedor)
    {
        var result = await _proveedorServices.Update(proveedor);

        if (!result.IsSuccess)
        {
            var code = (ProveedorErrorCode)result.ErrorCode;
            var message = MessageProvider.Get(ProveedorErrorDictionary.Messages, code);
            return BadRequest(message);
        }

        return Ok(proveedor);
    }

    [Authorize(Policy = "PERM:PROV_READ")]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _proveedorServices.GetAll();

        if (!result.IsSuccess)
        {
            var code = (ProveedorErrorCode)result.ErrorCode;
            var message = MessageProvider.Get(ProveedorErrorDictionary.Messages, code);
            return BadRequest(message);
        }

        return Ok(result.Value);
    }

    [Authorize(Policy = "PERM:PROV_READ")]
    [HttpGet("search")]
    public async Task<IActionResult> Search(
        int pageIndex = 1,
        string searchTerm = "",
        string estado = "activos")
    {
        int pageSize = 10;

        var result = await _proveedorServices.ProveedoresPagedAsync(pageIndex, pageSize, searchTerm, estado);

        if (!result.IsSuccess)
        {
            var code = (ProveedorErrorCode)result.ErrorCode;
            var message = MessageProvider.Get(ProveedorErrorDictionary.Messages, code);
            return BadRequest(message);
        }

        return Ok(result.Value);
    }

    [Authorize(Policy = "PERM:PROV_READ")]
    [HttpGet("{idProveedor:int}")]
    public async Task<IActionResult> GetById(int idProveedor)
    {
        var result = await _proveedorServices.GetById(idProveedor);

        if (!result.IsSuccess)
        {
            var code = (ProveedorErrorCode)result.ErrorCode;
            var message = MessageProvider.Get(ProveedorErrorDictionary.Messages, code);
            if (code == ProveedorErrorCode.proveedor_not_found) return NotFound(message);
            return BadRequest(message);
        }

        return Ok(result.Value);
    }

    [Authorize(Policy = "PERM:PROV_DELETE")]
    [HttpDelete("{idProveedor:int}")]
    public async Task<IActionResult> Delete(int idProveedor)
    {
        var result = await _proveedorServices.Delete(idProveedor);

        if (!result.IsSuccess)
        {
            var code = (ProveedorErrorCode)result.ErrorCode;
            var message = MessageProvider.Get(ProveedorErrorDictionary.Messages, code);
            if (code == ProveedorErrorCode.proveedor_not_found) return NotFound(message);
            return BadRequest(message);
        }

        return NoContent();
    }

    [Authorize(Policy = "PERM:PROV_DELETE")]
    [HttpPatch("{idProveedor:int}/toggle-estado")]
    public async Task<IActionResult> ToggleEstado(int idProveedor)
    {
        var result = await _proveedorServices.ToggleEstado(idProveedor);

        if (!result.IsSuccess)
        {
            var code = (ProveedorErrorCode)result.ErrorCode;
            var message = MessageProvider.Get(ProveedorErrorDictionary.Messages, code);
            if (code == ProveedorErrorCode.proveedor_not_found) return NotFound(message);
            return BadRequest(message);
        }

        return Ok();
    }

    [Authorize(Policy = "PERM:PROV_READ")]
    [HttpGet("export/excel")]
    public async Task<IActionResult> ExportarExcel()
    {
        var result = await _proveedorServices.ExportarExcelAsync();

        if (!result.IsSuccess)
        {
            var code = (ProveedorErrorCode)result.ErrorCode;
            var message = MessageProvider.Get(ProveedorErrorDictionary.Messages, code);
            return BadRequest(message);
        }

        return File(result.Value,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"proveedores_{DateTime.Today:yyyyMMdd}.xlsx");
    }

    [Authorize(Policy = "PERM:PROV_READ")]
    [HttpGet("export/pdf")]
    public async Task<IActionResult> ExportarPdf()
    {
        var result = await _proveedorServices.ExportarPdfAsync();

        if (!result.IsSuccess)
        {
            var code = (ProveedorErrorCode)result.ErrorCode;
            var message = MessageProvider.Get(ProveedorErrorDictionary.Messages, code);
            return BadRequest(message);
        }

        return File(result.Value,
            "application/pdf",
            $"proveedores_{DateTime.Today:yyyyMMdd}.pdf");
    }
}