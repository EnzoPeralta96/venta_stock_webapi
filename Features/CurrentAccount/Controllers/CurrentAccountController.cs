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
        [HttpGet("summary/{clientId}")]
        public async Task<IActionResult> GetAccountSummary(int clientId)
        {
            var result = await _currentAccountService.GetAccountSummaryAsync(clientId);

            if (!result.IsSuccess)
            {
                var code = (CurrentAccountCode)result.ErrorCode;
                var errorMessage = MessageProvider.Get(CurrentAccountDictionary.Messages, code);
                return NotFound(errorMessage);
            }

            return Ok(result.Value);
        }

        [Authorize(Policy = "PERM:CC_VIEW")]
        [HttpGet("movements/{clientId}")]
        public async Task<IActionResult> GetAccountMovementsPaged(
            int clientId,
            [FromQuery] int pageIndex = 1,
            [FromQuery] string searchTerm = "",
            [FromQuery] DateTime? fechaDesde = null,
            [FromQuery] DateTime? fechaHasta = null,
            [FromQuery] int? idTipoMovimiento = null)
        {
            int pageSize = 10;

            var result = await _currentAccountService.GetAccountMovementsPagedAsync(
                clientId, pageIndex, pageSize, searchTerm, fechaDesde, fechaHasta, idTipoMovimiento);

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

        [Authorize(Policy = "PERM:CC_VIEW")]
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

            // Devuelve el IdMovimiento para que el front pueda solicitar el PDF del comprobante.
            return Ok(new { idMovimiento = result.Value });
        }

        [Authorize(Policy = "PERM:CC_VIEW")]
        [HttpGet("pending-sales/{clientId}")]
        public async Task<IActionResult> GetPendingSalesForPayment(int clientId)
        {
            var result = await _currentAccountService.GetPendingSalesForPayment(clientId);

            if (!result.IsSuccess)
            {
                var code = (CurrentAccountCode)result.ErrorCode;
                var errorMessage = MessageProvider.Get(CurrentAccountDictionary.Messages, code);
                return NotFound(errorMessage);
            }

            return Ok(result.Value);
        }

        [Authorize(Policy = "PERM:CC_MANAGE")]
        [HttpPost("annul-payment")]
        public async Task<IActionResult> AnnulPayment([FromBody] AnnulPaymentDTO dto)
        {
            var result = await _currentAccountService.AnnulPayment(dto);

            if (!result.IsSuccess)
            {
                var code = (CurrentAccountCode)result.ErrorCode;
                var errorMessage = MessageProvider.Get(CurrentAccountDictionary.Messages, code);
                return BadRequest(errorMessage);
            }

            return Ok(new { idMovimiento = result.Value });
        }

        [Authorize(Policy = "PERM:CC_VIEW")]
        [HttpGet("payment-receipt/{idMovimiento}")]
        public async Task<IActionResult> GetPaymentReceipt(int idMovimiento)
        {
            var result = await _currentAccountService.GeneratePaymentReceiptAsync(idMovimiento);

            if (!result.IsSuccess)
            {
                var code = (CurrentAccountCode)result.ErrorCode;
                var errorMessage = MessageProvider.Get(CurrentAccountDictionary.Messages, code);
                return NotFound(errorMessage);
            }

            return File(result.Value, "application/pdf", $"comprobante-pago-{idMovimiento}.pdf");
        }

        [Authorize(Policy = "PERM:CC_MANAGE")]
        [HttpPost("register-debit-note")]
        public async Task<IActionResult> RegisterDebitNote([FromBody] RegisterDebitNoteDTO dto)
        {
            var result = await _currentAccountService.RegisterDebitNote(dto);

            if (!result.IsSuccess)
            {
                var code = (CurrentAccountCode)result.ErrorCode;
                var errorMessage = MessageProvider.Get(CurrentAccountDictionary.Messages, code);
                return BadRequest(errorMessage);
            }

            return Ok(new { idMovimiento = result.Value });
        }

        [Authorize(Policy = "PERM:CC_VIEW")]
        [HttpGet("overdue-clients")]
        public async Task<IActionResult> GetOverdueClients()
        {
            var result = await _currentAccountService.GetOverdueClients();

            if (!result.IsSuccess)
            {
                var code = (CurrentAccountCode)result.ErrorCode;
                var errorMessage = MessageProvider.Get(CurrentAccountDictionary.Messages, code);
                return NotFound(errorMessage);
            }

            return Ok(result.Value);
        }

        [Authorize(Policy = "PERM:CC_MANAGE")]
        [HttpPost("apply-interest/{clientId}")]
        public async Task<IActionResult> ApplyInterestToClient(int clientId, [FromBody] ApplyInterestDTO dto)
        {
            var result = await _currentAccountService.ApplyInterestToClient(clientId, dto.IdUsuarioRegistra);
            if (!result.IsSuccess)
            {
                var code = (CurrentAccountCode)result.ErrorCode;
                var errorMessage = MessageProvider.Get(CurrentAccountDictionary.Messages, code);
                return BadRequest(errorMessage);
            }
            return Ok(result.Value);
        }

        [Authorize(Policy = "PERM:CC_MANAGE")]
        [HttpPut("update-limit")]
        public async Task<IActionResult> UpdateAccountLimit([FromBody] UpdateAccountLimitDTO dto)
        {
            var result = await _currentAccountService.UpdateAccountLimitAsync(dto);

            if (!result.IsSuccess)
            {
                var code = (CurrentAccountCode)result.ErrorCode;
                var errorMessage = MessageProvider.Get(CurrentAccountDictionary.Messages, code);
                return BadRequest(errorMessage);
            }

            return Ok(new { idMovimiento = result.Value });
        }

        [Authorize(Policy = "PERM:CC_MANAGE")]
        [HttpPost("apply-interest/bulk")]
        public async Task<IActionResult> ApplyInterestToAll([FromBody] ApplyInterestDTO dto)
        {
            var result = await _currentAccountService.ApplyInterestToAll(dto.IdUsuarioRegistra);

            if (!result.IsSuccess)
            {
                var code = (CurrentAccountCode)result.ErrorCode;
                var errorMessage = MessageProvider.Get(CurrentAccountDictionary.Messages, code);
                return BadRequest(errorMessage);
            }
            
            return Ok(result.Value);
        }
    }
}