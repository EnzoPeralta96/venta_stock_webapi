using Microsoft.AspNetCore.Mvc;
using proyecto_venta_stock.Proveedor.DTO;
using proyecto_venta_stock.Proveedor.Services;

namespace proyecto_venta_stock.Controllers;

[ApiController]
[Route("[controller]")]
public class ProveedorController : ControllerBase
{
    private readonly IProveedorServices _proveedorServices;

    public ProveedorController(IProveedorServices proveedorServices)
    {
        _proveedorServices = proveedorServices;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ProveedorDTO proveedor)
    {
        var result = await _proveedorServices.Create(proveedor);
        if (!result.IsSuccess) return BadRequest(result.ErrorCode);
        return Ok(proveedor);
    }

    [HttpPut("update")]
    public async Task<IActionResult> Update([FromBody] ProveedorDTO proveedor)
    {
        var result = await _proveedorServices.Update(proveedor);
        if (!result.IsSuccess) return BadRequest(result.ErrorCode);
        return Ok(proveedor);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _proveedorServices.GetAll();
        if (!result.IsSuccess) return BadRequest(result.ErrorCode);
        return Ok(result.Value);
    }

    [HttpGet("{idProveedor:int}")]
    public async Task<IActionResult> GetById(int idProveedor)
    {
        var result = await _proveedorServices.GetById(idProveedor);
        if (!result.IsSuccess) return NotFound(result.ErrorCode);
        return Ok(result.Value);
    }

    [HttpDelete("{idProveedor:int}")]
    public async Task<IActionResult> Delete(int idProveedor)
    {
        var result = await _proveedorServices.Delete(idProveedor);
        if (!result.IsSuccess)
        {
            if (Convert.ToString(result.ErrorCode) == "proveedor_not_found") return NotFound();
            return BadRequest(result.ErrorCode);
        }
        return NoContent();
    }

    [HttpPatch("{idProveedor:int}/toggle-estado")]
    public async Task<IActionResult> ToggleEstado(int idProveedor)
    {
        var result = await _proveedorServices.ToggleEstado(idProveedor);
        if (!result.IsSuccess) return BadRequest(result.ErrorCode);
        return Ok();
    }
}