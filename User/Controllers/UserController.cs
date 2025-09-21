using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using proyecto_venta_stock.Services;
using proyecto_venta_stock.User.DTO;

namespace proyecto_venta_stock.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery]int? id)
    {
        var result = await _userService.GetUsersAsync(id);

        if (!result.IsSucces) return NotFound(result.ErrosMessage);

        return Ok(result.Value);
    }    
    
    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] UserCreateDTO user)
    {
        if (!ModelState.IsValid) return BadRequest();

        var result = await _userService.CreateAsync(user);

        if (!result.IsSucces) return BadRequest(result.ErrosMessage);
        
        return Ok(user);
    }
}
