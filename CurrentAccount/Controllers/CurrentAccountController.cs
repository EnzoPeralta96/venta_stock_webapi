using Microsoft.AspNetCore.Mvc;
using venta_stock_webapi.CurrentAccount.DTO.MovementDTO;
using venta_stock_webapi.CurrentAccount.Message;
using venta_stock_webapi.CurrentAccount.Services.CurrentAccountService;
using venta_stock_webapi.Shared.MessageProvider;

namespace venta_stock_webapi.CurrentAccount.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CurrentAccountController : ControllerBase
    {
        private readonly ICurrentAccountService _currentAccountService;

        public CurrentAccountController(ICurrentAccountService currentAccountService)
        {
            _currentAccountService = currentAccountService;
        }

        [HttpGet("movements/{clientId}")]
        public async Task<IActionResult> GetAccountMovementsByClientId(int clientId)
        {
            var result = await _currentAccountService.GetAccountMovementsByClientId(clientId);

            if (!result.IsSucces)
            {
                var code = (CurrentAccountCode)result.ErrorCode;
                var errorMessage = MessageProvider.Get(CurrentAccountDictionary.Messages, code);
                return NotFound(errorMessage);
            }
            
            return Ok(result.Value);
            
        }

        [HttpPost("create-movement")]
        public async Task<IActionResult> AddAccountMovement([FromBody] CreateCurrentAccountDTO accountMovementDTO)
        {
            var result = await _currentAccountService.CreateAccountMovement(accountMovementDTO);

            if (!result.IsSucces)
            {
                var code = (CurrentAccountCode)result.ErrorCode;
                var errorMessage = MessageProvider.Get(CurrentAccountDictionary.Messages, code);
                return NotFound(errorMessage);
            }

            return Created();
        }

        [HttpGet("movement-types")]
        public async Task<IActionResult> GetMovementTypes()
        {
            var result = await _currentAccountService.GetMovementTypes();

            if (!result.IsSucces)
            {
                var code = (CurrentAccountCode)result.ErrorCode;
                var errorMessage = MessageProvider.Get(CurrentAccountDictionary.Messages, code);
                return NotFound(errorMessage);
            }

            return Ok(result.Value);
        }

        [HttpPost("register-movement")]
        public async Task<IActionResult> RegisterMovement([FromBody] AddMovementDTO MovementDTO)
        {
            var result = await _currentAccountService.RegisterMovement(MovementDTO);

            if (!result.IsSucces)
            {
                var code = (CurrentAccountCode)result.ErrorCode;
                var errorMessage = MessageProvider.Get(CurrentAccountDictionary.Messages, code);
                return NotFound(errorMessage);
            }

            return Created();
        }
    }
}