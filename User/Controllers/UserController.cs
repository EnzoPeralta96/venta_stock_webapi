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

    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] UserDTO user)
    {
        var result = await _userService.Create(user);

        if (!result.IsSucces) return BadRequest(result.ErrosMessage);
        
        return Ok(user);
    }
}
