using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using venta_stock_webapi.Sale.DTO.CreditNoteReasonDTO;
using venta_stock_webapi.Sale.Message;
using venta_stock_webapi.Sale.Services.CreditNoteReasonService;
using venta_stock_webapi.Shared.MessageProvider;

namespace venta_stock_webapi.Sale.Controllers;

[ApiController]
[Route("api/sale/credit-note")]
[Authorize]
public class CreditNoteReasonController : ControllerBase
{
    private readonly ICreditNoteReasonService _service;

    public CreditNoteReasonController(ICreditNoteReasonService service)
    {
        _service = service;
    }

    [Authorize(Policy = "PERM:CC_READ")]
    [HttpGet("reasons/{idMotivo}")]
    public async Task<IActionResult> GetById(int idMotivo)
    {
        var result = await _service.GetById(idMotivo);

        if (!result.IsSuccess)
        {
            var code = (CreditNoteReasonCode)result.ErrorCode;
            return NotFound(MessageProvider.Get(CreditNoteReasonDictionary.Messages, code));
        }

        return Ok(result.Value);
    }

    [Authorize(Policy = "PERM:VEN_READ")]
    [HttpGet("reasons")]
    public async Task<IActionResult> GetAll([FromQuery] bool? activo = null)
    {
        var result = await _service.GetAll(activo);

        if (!result.IsSuccess)
        {
            var code = (CreditNoteReasonCode)result.ErrorCode;
            return StatusCode(500, MessageProvider.Get(CreditNoteReasonDictionary.Messages, code));
        }

        return Ok(result.Value);
    }

    [Authorize(Policy = "PERM:CC_MANAGE")]
    [HttpPost("reasons")]
    public async Task<IActionResult> Create([FromBody] CreateCreditNoteReasonDTO dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _service.Create(dto);

        if (!result.IsSuccess)
        {
            var code = (CreditNoteReasonCode)result.ErrorCode;
            return BadRequest(MessageProvider.Get(CreditNoteReasonDictionary.Messages, code));
        }

        return Ok();
    }

    [Authorize(Policy = "PERM:CC_MANAGE")]
    [HttpPut("reasons")]
    public async Task<IActionResult> Update([FromBody] UpdateCreditNoteReasonDTO dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _service.Update(dto);

        if (!result.IsSuccess)
        {
            var code = (CreditNoteReasonCode)result.ErrorCode;
            var status = code == CreditNoteReasonCode.reason_not_found ? 404 : 400;
            return StatusCode(status, MessageProvider.Get(CreditNoteReasonDictionary.Messages, code));
        }

        return Ok();
    }

    [Authorize(Policy = "PERM:CC_MANAGE")]
    [HttpPatch("reasons/toggle-state/{idMotivo}/{activo}")]
    public async Task<IActionResult> ToggleState(int idMotivo, bool activo)
    {
        var result = await _service.ToggleState(idMotivo, activo);

        if (!result.IsSuccess)
        {
            var code = (CreditNoteReasonCode)result.ErrorCode;
            return NotFound(MessageProvider.Get(CreditNoteReasonDictionary.Messages, code));
        }

        return Ok();
    }
}
