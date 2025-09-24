using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using proyecto_venta_stock.Location.Services;
using proyecto_venta_stock.Location.DTO;

namespace proyecto_venta_stock.Controllers;

[ApiController]
[Route("[controller]")]

public class LocationController : ControllerBase
{
    private readonly ILocationServices _locationServices;
    public LocationController(ILocationServices locationServices)
    {
        _locationServices = locationServices;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] LocationDTO location)
    {
        var result = await _locationServices.Create(location);

        if (!result.IsSucces) return BadRequest(result.ErrosMessage);

        return Ok(location);
    }

    [HttpPut("update")]
    public async Task<IActionResult> UpdateLocation([FromBody] LocationDTO location)
    {
        var result = await _locationServices.Update(location);

        if (!result.IsSucces) return BadRequest(result.ErrosMessage);

        return Ok(location);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _locationServices.GetAll();
        if (!result.IsSucces) return BadRequest(result.ErrosMessage);
        return Ok(result.Value);
    }

    [HttpGet("{idUbicacion:int}")]
    public async Task<IActionResult> GetById(int idUbicacion)
    {
        var result = await _locationServices.GetById(idUbicacion);
        if (!result.IsSucces) return NotFound(result.ErrosMessage);
        return Ok(result.Value);
    }
    [HttpDelete("{idUbicacion:int}")]
    public async Task<IActionResult> Delete(int idUbicacion)
    {
        var result = await _locationServices.Delete(idUbicacion);
        if (!result.IsSucces) return BadRequest(result.ErrosMessage);
        return Ok(result.Value);
    }
}