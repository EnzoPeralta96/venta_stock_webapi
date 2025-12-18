using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using venta_stock_webapi.Sale.DTO;
using venta_stock_webapi.Sale.Message;
using venta_stock_webapi.Sale.Services;
using venta_stock_webapi.Shared.MessageProvider;

namespace venta_stock_webapi.Sale.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SaleController : ControllerBase
    {
        private readonly ISaleServices _saleService;

        public SaleController(ISaleServices saleService)
        {
            _saleService = saleService;
        }

        /// <summary>
        /// Creates a new sale transaction
        /// </summary>
        [Authorize(Policy = "PERM:VEN_CREATE")]
        [HttpPost("create")]
        public async Task<IActionResult> CreateSale([FromBody] CreateSaleDTO createSaleDTO)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _saleService.CreateSaleAsync(createSaleDTO);

            if (!result.IsSuccess)
            {
                var code = (SaleErrorCode)result.ErrorCode;
                var errorMessage = MessageProvider.Get(SaleErrorDictionary.Messages, code);
                return BadRequest(errorMessage);
            }

            return Ok(result.Value);
        }
    }
}
