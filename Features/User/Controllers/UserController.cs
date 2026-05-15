using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using proyecto_venta_stock.Message;
using proyecto_venta_stock.Services;
using proyecto_venta_stock.User.DTO;
using venta_stock_webapi.Features.User.DTO.UserDTO;
using venta_stock_webapi.Shared.MessageProvider;

namespace proyecto_venta_stock.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    [Authorize(Policy = "PERM:USR_READ")]
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] int? id)
    {
        var result = await _userService.GetUsersAsync(id);

        if (!result.IsSuccess)
        {
            var code = (UserErrorCode)result.ErrorCode;
            var errorMessage = MessageProvider.Get(UserErrorDictionary.Messages, code);
            return NotFound(errorMessage);
        }

        return Ok(result.Value);
    }

    [Authorize(Policy = "PERM:USR_READ")]
    [HttpGet("search")]
    public async Task<IActionResult> SearchUsers(
    int pageIndex = 1,
    string searchTerm = "",
    string estado = "activos")
    {
        int sizePage = 10;
        var result = await _userService.UsersPagedAsync(pageIndex, sizePage, searchTerm, estado);

        if (!result.IsSuccess)
        {
            var code = (UserErrorCode)result.ErrorCode;
            var errorMessage = MessageProvider.Get(UserErrorDictionary.Messages, code);
            return NotFound(errorMessage);
        }

        return Ok(result.Value);
    }

    [Authorize(Policy = "PERM:USR_CREATE")]
    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] UserCreateDTO user)
    {

        var result = await _userService.CreateAsync(user);

        if (!result.IsSuccess)
        {
            var code = (UserErrorCode)result.ErrorCode;
            var errorMessage = MessageProvider.Get(UserErrorDictionary.Messages, code);
            return NotFound(errorMessage);
        }

        return Ok(user);
    }

    [Authorize(Policy = "PERM:USR_UPDATE")]
    [HttpPut("update")]
    public async Task<IActionResult> Update([FromBody] UserUpdateDTO user)
    {

        var result = await _userService.UpdateAsync(user);

        if (!result.IsSuccess)
        {
            var code = (UserErrorCode)result.ErrorCode;
            var errorMessage = MessageProvider.Get(UserErrorDictionary.Messages, code);
            return BadRequest(errorMessage);
        }

        return NoContent();
    }

    [Authorize(Policy = "PERM:USR_PASSWORD_UPDATE")]
    [HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] UserChangePasswordDTO dto)
    {

        var result = await _userService.ChangePasswordAsync(dto);

        if (!result.IsSuccess)
        {
            var code = (UserErrorCode)result.ErrorCode;
            var errorMessage = MessageProvider.Get(UserErrorDictionary.Messages, code);
            return BadRequest(errorMessage);
        }

        return NoContent();
    }

    [Authorize(Policy = "PERM:USR_DELETE")]
    [HttpDelete("delete/{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _userService.DeleteAsync(id);

        if (!result.IsSuccess)
        {
            var code = (UserErrorCode)result.ErrorCode;
            var errorMessage = MessageProvider.Get(UserErrorDictionary.Messages, code);
            return NotFound(errorMessage);
        }

        return NoContent();
    }

    /// <summary>
    /// Activa un usuario previamente desactivado. Si el usuario ya se encuentra activo, 
    /// se devuelve un error indicando que el usuario ya está activo.
    /// Se utiliza el mismo permiso que para eliminar usuarios, 
    /// ya que conceptualmente se considera una "reversión" de la eliminación, 
    /// más que una acción completamente distinta.
    /// </summary>

    [Authorize(Policy = "PERM:USR_DELETE")]
    [HttpPut("activate/{id}")]
    public async Task<IActionResult> Activate(int id)
    {
        var result = await _userService.ActivateAsync(id);

        if (!result.IsSuccess)
        {
            var code = (UserErrorCode)result.ErrorCode;
            var errorMessage = MessageProvider.Get(UserErrorDictionary.Messages, code);
            return BadRequest(errorMessage);
        }

        return NoContent();
    }
}
