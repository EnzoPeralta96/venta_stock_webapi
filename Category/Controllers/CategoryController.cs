using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using proyecto_venta_stock.Category.Services;
using proyecto_venta_stock.Category.DTO;

namespace proyecto_venta_stock.Controllers;

[ApiController]
[Route("[controller]")]

public class CategoryController : ControllerBase
{
    private readonly ICategoryServices _categoryServices;
    public CategoryController(ICategoryServices categoryServices)
    {
        _categoryServices = categoryServices;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CategoryDetailDTO category)
    {
        var result = await _categoryServices.Create(category);

        if (!result.IsSucces) return BadRequest(result.ErrosMessage);

        return Ok(category);
    }

    [HttpPut("update")]
    public async Task<IActionResult> UpdateCategory([FromBody] CategoryDetailDTO category)
    {
        var result = await _categoryServices.Update(category);

        if (!result.IsSucces) return BadRequest(result.ErrosMessage);

        return Ok(category);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _categoryServices.GetAll();
        if (!result.IsSucces) return BadRequest(result.ErrosMessage);
        return Ok(result.Value);
    }

    [HttpGet("{idCategoria:int}")]
    public async Task<IActionResult> GetById(int idCategoria)
    {
        var result = await _categoryServices.GetById(idCategoria);
        if (!result.IsSucces) return NotFound(result.ErrosMessage);
        return Ok(result.Value);
    }

    [HttpDelete("{idCategoria:int}")]
    public async Task<IActionResult> Delete(int idCategoria)
    {
        var result = await _categoryServices.Delete(idCategoria);
        if (!result.IsSucces)
        {
            if (result.ErrosMessage == "category_not_found") return NotFound();
            if (result.ErrosMessage == "category_in_use") return Conflict(result.ErrosMessage);
            return BadRequest(result.ErrosMessage);
        }
        return NoContent(); // <- no devolver false/true al front
    }
}
