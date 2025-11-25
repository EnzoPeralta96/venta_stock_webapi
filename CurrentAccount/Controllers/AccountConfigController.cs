using Microsoft.AspNetCore.Mvc;
using venta_stock_webapi.CurrentAccount.DTO;
using venta_stock_webapi.CurrentAccount.Message;
using venta_stock_webapi.CurrentAccount.Services.AccountConfigService;
using venta_stock_webapi.Shared.MessageProvider;

namespace venta_stock_webapi.CurrentAccount.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountConfigController : ControllerBase
    {
        private readonly IAccountConfigService _accountConfigService;

        public AccountConfigController(IAccountConfigService accountConfigService)
        {
            _accountConfigService = accountConfigService;
        }

        [HttpGet("account-configs/{configId}")]
        public async Task<IActionResult> GetAccountConfigById(int configId)
        {
            var result = await _accountConfigService.GetAccountConfigById(configId);

            if (!result.IsSucces)
            {
                var code = (AccountConfigCode)result.ErrorCode;
                var errorMessage = MessageProvider.Get(AccountConfigDictionary.Messages, code);
                return NotFound(errorMessage);
            }

            return Ok(result.Value);
        }

        [HttpGet("account-configs")]
        public async Task<IActionResult> GetAccountConfigs()
        {
            var result = await _accountConfigService.GetAccountConfigs();

            if (!result.IsSucces)
            {
                var code = (AccountConfigCode)result.ErrorCode;
                var errorMessage = MessageProvider.Get(AccountConfigDictionary.Messages, code);
                return NotFound(errorMessage);
            }

            return Ok(result.Value);
        }

        [HttpPost("create-account-configs")]
        public async Task<IActionResult> CreateAccountConfig([FromBody] CreateAccountConfigDTO accountConfigDTO)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _accountConfigService.CreateAccountConfig(accountConfigDTO);

            if (!result.IsSucces)
            {
                var code = (AccountConfigCode)result.ErrorCode;
                var errorMessage = MessageProvider.Get(AccountConfigDictionary.Messages, code);
                return BadRequest(errorMessage);
            }

            return Ok(result.Value);
        }

        [HttpPut("update-account-configs")]
        public async Task<IActionResult> UpdateAccountConfig([FromBody] UpdateAccountConfigDTO accountConfigDTO)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _accountConfigService.UpdateAccountConfig(accountConfigDTO);

            if (!result.IsSucces)
            {
                var code = (AccountConfigCode)result.ErrorCode;
                var errorMessage = MessageProvider.Get(AccountConfigDictionary.Messages, code);
                return BadRequest(errorMessage);
            }

            return Ok(result.Value);
        }

        [HttpDelete("toggle-state/{configId}/{active}")]
        public async Task<IActionResult> ToggleAccountConfigState(int configId, bool active)
        {
            var result = await _accountConfigService.ToggleStateAccountConfig(configId, active);

            if (!result.IsSucces)
            {
                var code = (AccountConfigCode)result.ErrorCode;
                var errorMessage = MessageProvider.Get(AccountConfigDictionary.Messages, code);
                return BadRequest(errorMessage);
            }

            return Ok(result.Value);
        }
    }
}