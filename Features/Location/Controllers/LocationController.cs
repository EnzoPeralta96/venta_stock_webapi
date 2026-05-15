using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using proyecto_venta_stock.Location.Services;
using proyecto_venta_stock.Location.DTO;
using proyecto_venta_stock.Message;
using venta_stock_webapi.Shared.MessageProvider;
using Microsoft.AspNetCore.Authorization;

namespace proyecto_venta_stock.Controllers;


[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LocationController : ControllerBase
{
    private readonly ILocationService _service;
    private readonly ILogger<LocationController> _logger;

    public LocationController(ILocationService service, ILogger<LocationController> logger)
    {
        _service = service;
        _logger = logger;
    }

    //GET api/Location/search?pageIndex=1&pageSize=10&searchTerm=a&activos=true
    [Authorize(Policy = "PERM:PROD_READ")]
    [HttpGet("search")]
    public async Task<IActionResult> Search(
        int pageIndex = 1,
        int pageSize = 10,
        string searchTerm = "",
        bool activos = true)
    {
        var result = await _service.SearchAsync(pageIndex, pageSize, searchTerm, activos);

        if (!result.IsSuccess)
        {
            var code = (LocationErrorCode)result.ErrorCode;
            var msg = MessageProvider.Get(LocationErrorDictionary.Messages, code);
            return BadRequest(msg);
        }

        return Ok(result.Value);
    }

    //GET api/Location/{id}
    [Authorize(Policy = "PERM:PROD_READ")]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);

        if (!result.IsSuccess)
        {
            var code = (LocationErrorCode)result.ErrorCode;
            var msg = MessageProvider.Get(LocationErrorDictionary.Messages, code);
            return NotFound(msg);
        }

        return Ok(result.Value);
    }

    //POST api/Location/create
    [Authorize(Policy = "PERM:PROD_CREATE")]
    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] LocationCreateUpdateDTO dto)
    {
        var result = await _service.CreateAsync(dto);

        if (!result.IsSuccess)
        {
            var code = (LocationErrorCode)result.ErrorCode;
            var msg = MessageProvider.Get(LocationErrorDictionary.Messages, code);
            
            if (code == LocationErrorCode.duplicate_location)
                return Conflict(msg);

            return BadRequest(msg);
        }

        return Ok(result.Value);
    }

    //PUT api/Location/update/{id}
    [Authorize(Policy = "PERM:PROD_UPDATE")]
    [HttpPut("update/{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] LocationCreateUpdateDTO dto)
    {

        var result = await _service.UpdateAsync(id, dto);

        if (!result.IsSuccess)
        {
            var code = (LocationErrorCode)result.ErrorCode;
            var msg = MessageProvider.Get(LocationErrorDictionary.Messages, code);

            return code switch
            {
                LocationErrorCode.location_not_found => NotFound(msg),
                LocationErrorCode.duplicate_location => Conflict(msg),
                LocationErrorCode.location_in_use => Conflict(msg),
                _ => BadRequest(msg)
            };
        }

        return Ok(result.Value);
    }

    // 🗑️ DELETE api/Location/delete/{id}
    [Authorize(Policy = "PERM:PROD_DELETE")]
    [HttpDelete("delete/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _service.DeleteAsync(id);

        if (!result.IsSuccess)
        {
            var code = (LocationErrorCode)result.ErrorCode;
            var msg = MessageProvider.Get(LocationErrorDictionary.Messages, code);
            return code switch
            {
                LocationErrorCode.location_not_found => NotFound(msg),
                LocationErrorCode.location_in_use => Conflict(msg),
                _ => BadRequest(msg)
            };
        }

        return Ok(new { message = "Ubicación eliminada correctamente" });
    }

    //PATCH api/Location/toggle/{id}
    [Authorize(Policy = "PERM:PROD_UPDATE")]
    [HttpPatch("toggle/{id:int}")]
    public async Task<IActionResult> ToggleActivo(int id)
    {
        var result = await _service.ToggleActivoAsync(id);

        if (!result.IsSuccess)
        {
            var code = (LocationErrorCode)result.ErrorCode;
            var msg = MessageProvider.Get(LocationErrorDictionary.Messages, code);
            return BadRequest(msg);
        }

        return Ok(new { message = "Estado de la ubicación actualizado" });
    }
}
