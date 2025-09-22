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

        if (!result.IsSucces) return BadRequest(result.ErrosMessage);

        return Ok(product);
    }

    [HttpPut("update")]
    public async Task<IActionResult> UpdateProduct([FromBody] ProductDTO product)
    {
        var result = await _productServices.Update(product);

        if (!result.IsSucces) return BadRequest(result.ErrosMessage);

        return Ok(product);
    }   
   
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _productServices.GetAll();
        if (!result.IsSucces) return BadRequest(result.ErrosMessage);
        return Ok(result.Value);
    }

    [HttpGet("{idProducto:int}")]
    public async Task<IActionResult> GetById(int idProducto)
    {
        var result = await _productServices.GetById(idProducto);
        if (!result.IsSucces) return NotFound(result.ErrosMessage);
        return Ok(result.Value);
    }

}