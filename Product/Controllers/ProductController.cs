using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using proyecto_venta_stock.Product.Services;
using proyecto_venta_stock.Product.DTO;


namespace proyecto_venta_stock.Controllers;

[ApiController]
[Route("[controller]")]

public class ProductController : ControllerBase
{
    private readonly IProductServices _productServices;
    public ProductController(IProductServices productServices)
    {
        _productServices = productServices;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ProductDTO product)
    {
        var result = await _productServices.Create(product);

        if (!result.IsSucces) return BadRequest(result.ErrorCode);

        return Ok(product);
    }

    [HttpPut("update")]
    public async Task<IActionResult> UpdateProduct([FromBody] ProductDTO product)
    {
        var result = await _productServices.Update(product);

        if (!result.IsSucces) return BadRequest(result.ErrorCode);

        return Ok(product);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _productServices.GetAll();
        if (!result.IsSucces) return BadRequest(result.ErrorCode);
        return Ok(result.Value);
    }
    [HttpGet("with-details")]
    public async Task<IActionResult> GetAllWithDetails([FromQuery] bool? activo = true)
    {
        var result = await _productServices.GetAllWithCategoryAndLocation(activo);
        if (!result.IsSucces) return BadRequest(result.ErrorCode);
        return Ok(result.Value);
    }

    [HttpGet("{idProducto:int}")]
    public async Task<IActionResult> GetById(int idProducto)
    {
        var result = await _productServices.GetById(idProducto);
        if (!result.IsSucces) return NotFound(result.ErrorCode);
        return Ok(result.Value);
    }
    [HttpDelete("{idProducto:int}")]
    public async Task<IActionResult> Delete(int idProducto)
    {
        var result = await _productServices.Delete(idProducto);
        if (!result.IsSucces)
        {
            if (Convert.ToString(result.ErrorCode) == "product_not_found") return NotFound();
            if (Convert.ToString(result.ErrorCode) == "product_in_use") return Conflict(result.ErrorCode);
            return BadRequest(result.ErrorCode);
        }
        return NoContent();
    }
    [HttpPatch("{idProducto:int}/toggle-estado")]
    public async Task<IActionResult> ToggleEstado(int idProducto)
    {
        var result = await _productServices.ToggleEstado(idProducto);

        if (!result.IsSucces)
            return BadRequest(result.ErrorCode);

        return Ok(new { idProducto, activo = result.Value });
    }

    [HttpGet("with-details-paged")]
    public async Task<IActionResult> GetAllWithDetailsPaged(
    [FromQuery] int pageIndex = 1,
    [FromQuery] int pageSize = 9,
    [FromQuery] bool? activo = true,
    [FromQuery] string? search = null)
    {
        var result = await _productServices.GetAllWithCategoryAndLocationPaged(pageIndex, pageSize, activo, search);
        if (!result.IsSucces) return BadRequest(result.ErrorCode);
        return Ok(result.Value);
    }

    [HttpGet("export/csv")]
    public async Task<IActionResult> ExportarCsv()
    {
        var file = await _productServices.ExportarCsvAsync();
        return File(file, "text/csv", "productos.csv");
    }

    [HttpGet("export/excel")]
    public async Task<IActionResult> ExportarExcel()
    {
        var file = await _productServices.ExportarExcelAsync();
        return File(
            file,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "productos.xlsx"
        );
    }

    [HttpGet("export/plantilla-csv")]
    public IActionResult ExportarPlantillaCsv()
    {
        var csv = _productServices.ExportarPlantillaCsv();
        return File(csv, "text/csv", "plantilla_productos.csv");
    }

    [HttpGet("export/plantilla-excel")]
    public IActionResult ExportarPlantillaExcel()
    {
        var file = _productServices.ExportarPlantillaExcel();
        return File(
            file,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "plantilla_productos.xlsx"
        );
    }


}