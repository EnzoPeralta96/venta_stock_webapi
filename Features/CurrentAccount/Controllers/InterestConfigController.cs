using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using venta_stock_webapi.CurrentAccount.DTO.InterestConfigDTO;
using venta_stock_webapi.CurrentAccount.Message;
using venta_stock_webapi.CurrentAccount.Services.InterestConfigService;
using venta_stock_webapi.Shared.MessageProvider;

namespace venta_stock_webapi.CurrentAccount.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InterestConfigController : ControllerBase
{
    private readonly IInterestConfigService _interestConfigService;

    public InterestConfigController(IInterestConfigService interestConfigService)
    {
        _interestConfigService = interestConfigService;
    }

    [Authorize(Policy = "PERM:CC_VIEW")]
    [HttpGet("interest-configs")]
    public async Task<IActionResult> GetAll()
    {
        var result = await _interestConfigService.GetAll();

        if(!result.IsSuccess)
        {
            var code = (InterestConfigCode)result.ErrorCode;
            var errorMessage = MessageProvider.Get(InterestConfigDictionary.Messages, code);
            return NotFound(errorMessage);          
        }
        return Ok(result.Value);
    }

    [Authorize(Policy = "PERM:CC_VIEW")]
    [HttpGet("interest-configs/current")]
    public async Task<IActionResult> GetCurrent()
    {
        var result = await _interestConfigService.GetCurrent();

        if (!result.IsSuccess)
        {
            var code = (InterestConfigCode)result.ErrorCode;

            var errorMessage = MessageProvider.Get(InterestConfigDictionary.Messages, code);
            return NotFound(errorMessage);
        }

        return Ok(result.Value);
    }

    [Authorize(Policy = "PERM:CC_VIEW")]
    [HttpGet("interest-configs/{idConfig}")]
    public async Task<IActionResult> GetById(int idConfig)
    {
        var result = await _interestConfigService.GetById(idConfig);

        if (!result.IsSuccess)
        {
            var code = (InterestConfigCode)result.ErrorCode;
            var errorMessage = MessageProvider.Get(InterestConfigDictionary.Messages, code);
            return NotFound(errorMessage);
        }

        return Ok(result.Value);
    }

    [Authorize(Policy = "PERM:CC_MANAGE")]
    [HttpPost("interest-configs")]
    public async Task<IActionResult> Create([FromBody] CreateInterestConfigDTO dto)
    {
        var result = await _interestConfigService.Create(dto);

        if (!result.IsSuccess)
        {
            var code = (InterestConfigCode)result.ErrorCode;
            var errorMessage = MessageProvider.Get(InterestConfigDictionary.Messages, code);
            return BadRequest(errorMessage);
        }

        return Created();
    }

    [Authorize(Policy = "PERM:CC_MANAGE")]
    [HttpPut("interest-configs")]
    public async Task<IActionResult> Update([FromBody] UpdateInterestConfigDTO dto)
    {
        var result = await _interestConfigService.Update(dto);

        if (!result.IsSuccess)
        {
            var code = (InterestConfigCode)result.ErrorCode;
            var errorMessage = MessageProvider.Get(InterestConfigDictionary.Messages, code);
            return BadRequest(errorMessage);
        }
        return Ok();
    }

    [Authorize(Policy = "PERM:CC_MANAGE")]
    [HttpPut("interest-configs/set-current/{idConfig}")]
    public async Task<IActionResult> SetAsCurrent(int idConfig)
    {
        var result = await _interestConfigService.SetAsCurrent(idConfig);
        
        if (!result.IsSuccess)
        {
            var code = (InterestConfigCode)result.ErrorCode;
            var errorMessage = MessageProvider.Get(InterestConfigDictionary.Messages, code);
            return BadRequest(errorMessage);
        }

        return Ok();
    }
}
