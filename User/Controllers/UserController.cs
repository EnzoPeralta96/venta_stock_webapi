using Microsoft.AspNetCore.Mvc;
using proyecto_venta_stock.Message;
using proyecto_venta_stock.Services;
using proyecto_venta_stock.User.DTO;
using venta_stock_webapi.Shared.MessageProvider;

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
    public async Task<IActionResult> GetUsers([FromQuery] int? id)
    {
        var result = await _userService.GetUsersAsync(id);

        if (!result.IsSucces)
        {
            var code = (UserErrorCode)result.ErrorCode;
            var errorMessage = MessageProvider.Get(UserErrorDictionary.Messages, code);
            return NotFound(errorMessage);
        }

        return Ok(result.Value);
    }

    [HttpGet("search")]
public async Task<IActionResult> SearchUsers(
    int pageIndex = 1,
    string searchTerm = "",
    string estado = "activos") 
{
    int sizePage = 10;
    var result = await _userService.UsersPagedAsync(pageIndex, sizePage, searchTerm, estado);

    if (!result.IsSucces)
    {
        var code = (UserErrorCode)result.ErrorCode;
        var errorMessage = MessageProvider.Get(UserErrorDictionary.Messages, code);
        return NotFound(errorMessage);
    }

    return Ok(result.Value);
}


    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] UserCreateDTO user)
    {
        if (!ModelState.IsValid) return BadRequest();

        var result = await _userService.CreateAsync(user);

        if (!result.IsSucces)
        {
            var code = (UserErrorCode)result.ErrorCode;
            var errorMessage = MessageProvider.Get(UserErrorDictionary.Messages, code);
            return NotFound(errorMessage);
        }

        return Ok(user);
    }

    [HttpPut("update")]
    public async Task<IActionResult> Update([FromBody] UserUpdateDTO user)
    {
        if (!ModelState.IsValid) return BadRequest();

        var result = await _userService.UpdateAsync(user);

        if (!result.IsSucces)
        {
            var code = (UserErrorCode)result.ErrorCode;
            var errorMessage = MessageProvider.Get(UserErrorDictionary.Messages, code);
            return BadRequest(errorMessage);
        }

        return NoContent();
    }

    [HttpDelete("delete/{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _userService.DeleteAsync(id);

        if (!result.IsSucces)
        {
            var code = (UserErrorCode)result.ErrorCode;
            var errorMessage = MessageProvider.Get(UserErrorDictionary.Messages, code);
            return NotFound(errorMessage);
        }

        return NoContent();
    }
}
