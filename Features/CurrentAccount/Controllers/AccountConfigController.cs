using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using venta_stock_webapi.CurrentAccount.DTO;
using venta_stock_webapi.CurrentAccount.Message;
using venta_stock_webapi.CurrentAccount.Services.AccountConfigService;
using venta_stock_webapi.Shared.MessageProvider;

namespace venta_stock_webapi.CurrentAccount.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AccountConfigController : ControllerBase
    {
        private readonly IAccountConfigService _accountConfigService;

        public AccountConfigController(IAccountConfigService accountConfigService)
        {
            _accountConfigService = accountConfigService;
        }

        [Authorize(Policy = "PERM:CC_VIEW")]
        [HttpGet("account-configs/{configId}")]
        public async Task<IActionResult> GetAccountConfigById(int configId)
        {
            var result = await _accountConfigService.GetAccountConfigById(configId);

            if (!result.IsSuccess)
            {
                var code = (AccountConfigCode)result.ErrorCode;
                var errorMessage = MessageProvider.Get(AccountConfigDictionary.Messages, code);
                return NotFound(errorMessage);
            }

            return Ok(result.Value);
        }

        [Authorize(Policy = "PERM:CC_VIEW")]
        [HttpGet("account-configs")]
        public async Task<IActionResult> GetAccountConfigs([FromQuery] bool? activo = null)
        {
            var result = await _accountConfigService.GetAccountConfigs(activo);

            if (!result.IsSuccess)
            {
                var code = (AccountConfigCode)result.ErrorCode;
                var errorMessage = MessageProvider.Get(AccountConfigDictionary.Messages, code);
                return NotFound(errorMessage);
            }

            return Ok(result.Value);
        }

        [Authorize(Policy = "PERM:CC_MANAGE")]
        [HttpPost("create-account-configs")]
        public async Task<IActionResult> CreateAccountConfig([FromBody] CreateAccountConfigDTO accountConfigDTO)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _accountConfigService.CreateAccountConfig(accountConfigDTO);

            if (!result.IsSuccess)
            {
                var code = (AccountConfigCode)result.ErrorCode;
                var errorMessage = MessageProvider.Get(AccountConfigDictionary.Messages, code);
                return BadRequest(errorMessage);
            }

            return Ok(result.Value);
        }

        [Authorize(Policy = "PERM:CC_MANAGE")]
        [HttpPut("update-account-configs")]
        public async Task<IActionResult> UpdateAccountConfig([FromBody] UpdateAccountConfigDTO accountConfigDTO)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _accountConfigService.UpdateAccountConfig(accountConfigDTO);

            if (!result.IsSuccess)
            {
                var code = (AccountConfigCode)result.ErrorCode;
                var errorMessage = MessageProvider.Get(AccountConfigDictionary.Messages, code);
                return BadRequest(errorMessage);
            }

            return Ok(result.Value);
        }

        [Authorize(Policy = "PERM:CC_MANAGE")]
        [HttpDelete("toggle-state/{configId}/{active}")]
        public async Task<IActionResult> ToggleAccountConfigState(int configId, bool active)
        {
            var result = await _accountConfigService.ToggleStateAccountConfig(configId, active);

            if (!result.IsSuccess)
            {
                var code = (AccountConfigCode)result.ErrorCode;
                var errorMessage = MessageProvider.Get(AccountConfigDictionary.Messages, code);
                return BadRequest(errorMessage);
            }

            return Ok(result.Value);
        }
    }
}