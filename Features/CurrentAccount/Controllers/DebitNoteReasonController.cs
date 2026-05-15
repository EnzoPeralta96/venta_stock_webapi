using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using venta_stock_webapi.CurrentAccount.DTO.DebitNoteReasonDTO;
using venta_stock_webapi.CurrentAccount.Message;
using venta_stock_webapi.CurrentAccount.Services.DebitNoteReasonService;
using venta_stock_webapi.Shared.MessageProvider;

namespace venta_stock_webapi.CurrentAccount.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DebitNoteReasonController : ControllerBase
    {
        private readonly IDebitNoteReasonService _debitNoteReasonService;

        public DebitNoteReasonController(IDebitNoteReasonService debitNoteReasonService)
        {
            _debitNoteReasonService = debitNoteReasonService;
        }

        [Authorize(Policy = "PERM:CC_VIEW")]
        [HttpGet("reasons/{idMotivo}")]
        public async Task<IActionResult> GetById(int idMotivo)
        {
            var result = await _debitNoteReasonService.GetById(idMotivo);

            if (!result.IsSuccess)
            {
                var code = (DebitNoteReasonCode)result.ErrorCode;
                var errorMessage = MessageProvider.Get(DebitNoteReasonDictionary.Messages, code);
                return NotFound(errorMessage);
            }

            return Ok(result.Value);
        }

        [Authorize(Policy = "PERM:CC_VIEW")]
        [HttpGet("reasons")]
        public async Task<IActionResult> GetAll([FromQuery] bool? activo = null, [FromQuery] string? categoria = null)
        {
            var result = await _debitNoteReasonService.GetAll(activo, categoria);

            if (!result.IsSuccess)
            {
                var code = (DebitNoteReasonCode)result.ErrorCode;
                var errorMessage = MessageProvider.Get(DebitNoteReasonDictionary.Messages, code);
                return NotFound(errorMessage);
            }

            return Ok(result.Value);
        }

        [Authorize(Policy = "PERM:CC_MANAGE")]
        [HttpPost("reasons")]
        public async Task<IActionResult> Create([FromBody] CreateDebitNoteReasonDTO dto)
        {
         
            var result = await _debitNoteReasonService.Create(dto);

            if (!result.IsSuccess)
            {
                var code = (DebitNoteReasonCode)result.ErrorCode;
                var errorMessage = MessageProvider.Get(DebitNoteReasonDictionary.Messages, code);
                return BadRequest(errorMessage);
            }

            return Ok(result.Value);
        }

        [Authorize(Policy = "PERM:CC_MANAGE")]
        [HttpPut("reasons")]
        public async Task<IActionResult> Update([FromBody] UpdateDebitNoteReasonDTO dto)
        {

            var result = await _debitNoteReasonService.Update(dto);

            if (!result.IsSuccess)
            {
                var code = (DebitNoteReasonCode)result.ErrorCode;
                var errorMessage = MessageProvider.Get(DebitNoteReasonDictionary.Messages, code);
                return BadRequest(errorMessage);
            }

            return Ok(result.Value);
        }

        [Authorize(Policy = "PERM:CC_MANAGE")]
        [HttpDelete("toggle-state/{idMotivo}/{activo}")]
        public async Task<IActionResult> ToggleState(int idMotivo, bool activo)
        {
            var result = await _debitNoteReasonService.ToggleState(idMotivo, activo);

            if (!result.IsSuccess)
            {
                var code = (DebitNoteReasonCode)result.ErrorCode;
                var errorMessage = MessageProvider.Get(DebitNoteReasonDictionary.Messages, code);
                return BadRequest(errorMessage);
            }

            return Ok(result.Value);
        }
    }
}
