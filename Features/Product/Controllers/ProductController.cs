using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using proyecto_venta_stock.Product.Services;
using proyecto_venta_stock.Product.DTO;
using proyecto_venta_stock.Message;
using venta_stock_webapi.Shared.MessageProvider;


namespace proyecto_venta_stock.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class ProductController : ControllerBase
{
    private readonly IProductServices _productServices;
    public ProductController(IProductServices productServices)
    {
        _productServices = productServices;
    }

    [Authorize(Policy = "PERM:PROD_CREATE")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ProductDTO product)
    {
        var result = await _productServices.Create(product);

        if (!result.IsSuccess)
        {
            var code = (ProductErrorCode)result.ErrorCode;
            var message = MessageProvider.Get(ProductErrorDictionary.Messages, code);
            return BadRequest(message);
        }

        return Ok(product);
    }

    [Authorize(Policy = "PERM:PROD_UPDATE")]
    [HttpPut("update")]
    public async Task<IActionResult> UpdateProduct([FromBody] ProductDTO product)
    {
        var result = await _productServices.Update(product);

        if (!result.IsSuccess)
        {
            var code = (ProductErrorCode)result.ErrorCode;
            var message = MessageProvider.Get(ProductErrorDictionary.Messages, code);
            return BadRequest(message);
        }

        return Ok(product);
    }

    [Authorize(Policy = "PERM:PROD_READ")]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _productServices.GetAll();

        if (!result.IsSuccess)
        {
            var code = (ProductErrorCode)result.ErrorCode;
            var message = MessageProvider.Get(ProductErrorDictionary.Messages, code);
            return BadRequest(message);
        }
        
        return Ok(result.Value);
    }
    [Authorize(Policy = "PERM:PROD_READ")]
    [HttpGet("with-details")]
    public async Task<IActionResult> GetAllWithDetails([FromQuery] bool? activo = true)
    {
        var result = await _productServices.GetAllWithCategoryAndLocation(activo);

        if (!result.IsSuccess)
        {
            var code = (ProductErrorCode)result.ErrorCode;
            var message = MessageProvider.Get(ProductErrorDictionary.Messages, code);
            return BadRequest(message);
        }

        return Ok(result.Value);
    }

    [Authorize(Policy = "PERM:PROD_READ")]
    [HttpGet("{idProducto:int}")]
    public async Task<IActionResult> GetById(int idProducto)
    {
        var result = await _productServices.GetById(idProducto);

        if (!result.IsSuccess)
        {
            var code = (ProductErrorCode)result.ErrorCode;
            var message = MessageProvider.Get(ProductErrorDictionary.Messages, code);
            return NotFound(message);
        }

        return Ok(result.Value);
    }
    [Authorize(Policy = "PERM:PROD_DELETE")]
    [HttpDelete("{idProducto:int}")]
    public async Task<IActionResult> Delete(int idProducto)
    {
        var result = await _productServices.Delete(idProducto);

        if (!result.IsSuccess)
        {
            var code = (ProductErrorCode)result.ErrorCode;
            var message = MessageProvider.Get(ProductErrorDictionary.Messages, code);

            return code switch
            {
                ProductErrorCode.product_not_found => NotFound(message),
                ProductErrorCode.product_in_use => Conflict(message),
                _ => BadRequest(message)
            };
        }

        return NoContent();
    }
    [Authorize(Policy = "PERM:PROD_DELETE")]
    [HttpPatch("{idProducto:int}/toggle-estado")]
    public async Task<IActionResult> ToggleEstado(int idProducto)
    {
        var result = await _productServices.ToggleEstado(idProducto);

        if (!result.IsSuccess)
        {
            var code = (ProductErrorCode)result.ErrorCode;
            var message = MessageProvider.Get(ProductErrorDictionary.Messages, code);
            return BadRequest(message);
        }

        return Ok(new { idProducto, activo = result.Value });
    }

    [Authorize(Policy = "PERM:PROD_READ")]
    [HttpGet("with-details-paged")]
    public async Task<IActionResult> GetAllWithDetailsPaged(
    [FromQuery] int pageIndex = 1,
    [FromQuery] int pageSize = 9,
    [FromQuery] bool? activo = true,
    [FromQuery] string? search = null,
    [FromQuery] int? idCategoria = null)
    {
        var result = await _productServices.GetAllWithCategoryAndLocationPaged(pageIndex, pageSize, activo, search, idCategoria);

        if (!result.IsSuccess)
        {
            var code = (ProductErrorCode)result.ErrorCode;
            var message = MessageProvider.Get(ProductErrorDictionary.Messages, code);
            return BadRequest(message);
        }

        return Ok(result.Value);
    }

    [Authorize(Policy = "PERM:PROD_READ")]
    [HttpGet("export/csv")]
    public async Task<IActionResult> ExportarCsv()
    {
        var file = await _productServices.ExportarCsvAsync();
        return File(file, "text/csv", "productos.csv");
    }

    [Authorize(Policy = "PERM:PROD_READ")]
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

    [Authorize(Policy = "PERM:PROD_READ")]
    [HttpGet("export/plantilla-csv")]
    public IActionResult ExportarPlantillaCsv()
    {
        var csv = _productServices.ExportarPlantillaCsv();

        return File(csv, "text/csv", "plantilla_productos.csv");
    }

    [Authorize(Policy = "PERM:PROD_READ")]
    [HttpGet("export/plantilla-excel")]
    public async Task<IActionResult> ExportarPlantillaExcel()
    {
        var file = await _productServices.ExportarPlantillaExcel();

        return File(
            file,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "plantilla_productos.xlsx"
        );
    }

    [HttpPost("importar")]
    [Authorize(Policy = "PERM:PROD_UPDATE")]
    public async Task<IActionResult> ImportarProductos(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Debe subir un archivo CSV o Excel válido.");

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int idUsuario))
            return Unauthorized();

        var result = await _productServices.ImportarProductosAsync(file, idUsuario);

        if (!result.IsSuccess)
        {
            var code = (ProductErrorCode)result.ErrorCode;
            var message = MessageProvider.Get(ProductErrorDictionary.Messages, code);
            return BadRequest(message);
        }

        return Ok(result.Value);
    }

    [Authorize(Policy = "PERM:PROD_UPDATE")]
    [HttpGet("plantilla-precios")]
    public async Task<IActionResult> DescargarPlantillaPrecios()
    {
        var result = await _productServices.DescargarPlantillaPreciosAsync();

        if (!result.IsSuccess)
        {
            var code = (ProductErrorCode)result.ErrorCode;
            var message = MessageProvider.Get(ProductErrorDictionary.Messages, code);
            return BadRequest(message);
        }

        return File(
            result.Value,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"plantilla_precios_{DateTime.Today:yyyyMMdd}.xlsx");
    }

    [Authorize(Policy = "PERM:PROD_UPDATE")]
    [HttpPost("actualizar-masivo/manual")]
    public async Task<IActionResult> ActualizarMasivoManual([FromBody] ActualizarMasivoManualDTO dto)
    {
        var result = await _productServices.ActualizarMasivoManualAsync(dto);

        if (!result.IsSuccess)
        {
            var code = (ProductErrorCode)result.ErrorCode;
            var message = MessageProvider.Get(ProductErrorDictionary.Messages, code);
            return BadRequest(message);
        }

        return Ok(result.Value);
    }

    [Authorize(Policy = "PERM:PROD_UPDATE")]
    [HttpPost("actualizar-masivo/excel")]
    public async Task<IActionResult> ActualizarMasivoExcel(IFormFile file, [FromForm] decimal ivaDefecto = 21)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Debe subir un archivo Excel o CSV válido.");

        var result = await _productServices.ActualizarMasivoExcelAsync(file, ivaDefecto);

        if (!result.IsSuccess)
        {
            var code = (ProductErrorCode)result.ErrorCode;
            var message = MessageProvider.Get(ProductErrorDictionary.Messages, code);
            return BadRequest(message);
        }

        return Ok(result.Value);
    }


}