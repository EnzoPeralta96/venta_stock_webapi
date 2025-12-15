using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using venta_stock_webapi.CurrentAccount.DTO.MovementDTO;
using venta_stock_webapi.CurrentAccount.Message;
using venta_stock_webapi.CurrentAccount.Services.CurrentAccountService;
using venta_stock_webapi.Shared.MessageProvider;

namespace venta_stock_webapi.CurrentAccount.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CurrentAccountController : ControllerBase
    {
        private readonly ICurrentAccountService _currentAccountService;

        public CurrentAccountController(ICurrentAccountService currentAccountService)
        {
            _currentAccountService = currentAccountService;
        }


        [Authorize(Policy = "PERM:CC_VIEW")]
        [HttpGet("movements/{clientId}")]
        public async Task<IActionResult> GetAccountMovementsByClientId(int clientId)
        {
            var result = await _currentAccountService.GetAccountMovementsByClientId(clientId);

            if (!result.IsSuccess)
            {
                var code = (CurrentAccountCode)result.ErrorCode;
                var errorMessage = MessageProvider.Get(CurrentAccountDictionary.Messages, code);
                return NotFound(errorMessage);
            }
            return Ok(result.Value);

        }

        [Authorize(Policy = "PERM:CLI_CREATE")]
        [HttpPost("create-account")]
        public async Task<IActionResult> AddCurrentAccountToClient([FromBody] CreateCurrentAccountDTO accountMovementDTO)
        {
            var result = await _currentAccountService.CreateAccountMovement(accountMovementDTO);

            if (!result.IsSuccess)
            {
                var code = (CurrentAccountCode)result.ErrorCode;
                var errorMessage = MessageProvider.Get(CurrentAccountDictionary.Messages, code);
                return NotFound(errorMessage);
            }

            return Created();
        }

        [Authorize(Policy = "PERM:CC_REGISTER_PAYMENT")]
        [HttpGet("movement-types")]
        public async Task<IActionResult> GetMovementTypes()
        {
            var result = await _currentAccountService.GetMovementTypes();

            if (!result.IsSuccess)
            {
                var code = (CurrentAccountCode)result.ErrorCode;
                var errorMessage = MessageProvider.Get(CurrentAccountDictionary.Messages, code);
                return NotFound(errorMessage);
            }

            return Ok(result.Value);
        }

        [Authorize(Policy = "PERM:CC_REGISTER_PAYMENT")]
        [HttpPost("register-movement")]
        public async Task<IActionResult> RegisterMovement([FromBody] AddMovementDTO MovementDTO)
        {
            var result = await _currentAccountService.RegisterMovement(MovementDTO);

            if (!result.IsSuccess)
            {
                var code = (CurrentAccountCode)result.ErrorCode;
                var errorMessage = MessageProvider.Get(CurrentAccountDictionary.Messages, code);
                return NotFound(errorMessage);
            }

            return Created();
        }
    }
}