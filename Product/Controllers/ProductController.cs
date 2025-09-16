using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using proyecto_venta_stock.Product.Services;
using proyecto_venta_stock.Product.DTO;
/* Agrego models para la ubicacion y data */
using proyecto_venta_stock.Models;
using proyecto_venta_stock.Data;

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
   

}