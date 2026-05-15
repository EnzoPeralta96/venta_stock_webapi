using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using proyecto_venta_stock.Data;
using proyecto_venta_stock.Message;
using venta_stock_webapi.Data.Audit;
using venta_stock_webapi.Login.DTO;
using venta_stock_webapi.Login.Services;
using venta_stock_webapi.Shared.Identity;
using venta_stock_webapi.Shared.MessageProvider;

namespace venta_stock_webapi.Features.Login.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoginController : ControllerBase
    {
        public readonly ILoginService _loginService;

        public LoginController(ILoginService loginService)
        {
            _loginService = loginService;
        }

        [HttpPost]
        public async Task<IActionResult> AuthenticacionUser([FromBody] LoginRequestDTO loginRequest)
        {
            var result = await _loginService.AuthenticateAsync(loginRequest);

            if (!result.IsSuccess)
            {
                var code = (UserErrorCode)result.ErrorCode;
                var errorMessage = MessageProvider.Get(UserErrorDictionary.Messages, code);
                return BadRequest(errorMessage);
            }
            
            return Ok(result.Value);
        }
    }
}