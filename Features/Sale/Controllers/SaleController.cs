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
        private readonly IPdfService _pdfService;
        private readonly ILogger<SaleController> _logger;

        public SaleController(ISaleServices saleService, IPdfService pdfService, ILogger<SaleController> logger)
        {
            _saleService = saleService;
            _pdfService = pdfService;
            _logger = logger;
        }

        /// <summary>
        /// Obtener listado de ventas con paginación y filtros opcionales
        /// </summary>
        /// <param name="pageNumber">Número de página (por defecto 1)</param>
        /// <param name="pageSize">Tamaño de página (por defecto 10)</param>
        /// <param name="clienteFilter">Filtro por nombre o razón social del cliente</param>
        /// <param name="fechaDesde">Filtro por fecha desde (opcional)</param>
        /// <param name="fechaHasta">Filtro por fecha hasta (opcional)</param>
        /// <returns>Lista paginada de ventas</returns>
        [Authorize(Policy = "PERM:VEN_READ")]
        [HttpGet]
        public async Task<IActionResult> GetSales(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? clienteFilter = null,
            [FromQuery] DateTime? fechaDesde = null,
            [FromQuery] DateTime? fechaHasta = null,
            [FromQuery] string? estado = null)
        {
            _logger.LogInformation(
                "Listando ventas - Página: {PageNumber}, Tamaño: {PageSize}, Cliente: {ClienteFilter}, Desde: {FechaDesde}, Hasta: {FechaHasta}, Estado: {Estado}",
                pageNumber, pageSize, clienteFilter, fechaDesde, fechaHasta, estado
            );

            var result = await _saleService.GetSalesPagedAsync(
                pageNumber,
                pageSize,
                clienteFilter,
                fechaDesde,
                fechaHasta,
                estado
            );

            if (!result.IsSuccess)
            {
                var code = (SaleErrorCode)result.ErrorCode;
                var errorMessage = MessageProvider.Get(SaleErrorDictionary.Messages, code);
                return BadRequest(errorMessage);
            }

            _logger.LogInformation(
                "Ventas obtenidas exitosamente - Total: {Total}, Página: {Page}",
                result.Value.TotalCount, result.Value.PagedIndex
            );

            return Ok(result.Value);
        }

        /// <summary>
        /// Obtener detalle completo de una venta por ID
        /// </summary>
        /// <param name="id">ID de la venta</param>
        /// <returns>Detalle completo de la venta incluyendo items</returns>
        [Authorize(Policy = "PERM:VEN_READ")]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetSaleById(int id)
        {
            _logger.LogInformation("Obteniendo detalle de venta con ID: {Id}", id);

            var result = await _saleService.GetSaleByIdAsync(id);

            if (!result.IsSuccess)
            {
                var code = (SaleErrorCode)result.ErrorCode;
                var errorMessage = MessageProvider.Get(SaleErrorDictionary.Messages, code);

                _logger.LogWarning("Venta {Id} no encontrada", id);
                return NotFound(errorMessage);
            }

            _logger.LogInformation("Detalle de venta {Id} obtenido exitosamente", id);

            return Ok(result.Value);
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

            // Venta pendiente de autorización → 202 Accepted
            if (result.Value.IdVenta == 0)
                return Accepted(result.Value);

            // Venta completada → 201 Created
            return StatusCode(201, result.Value);
        }

        [Authorize(Policy = "PERM:CC_NOTE_CREDIT")]
        [HttpPost("{idVenta:int}/annul")]
        public async Task<IActionResult> AnnulSale(int idVenta, [FromBody] AnnulSaleDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _saleService.AnnulSaleAsync(idVenta, dto);

            if (!result.IsSuccess)
            {
                var code = (SaleErrorCode)result.ErrorCode;
                var errorMessage = MessageProvider.Get(SaleErrorDictionary.Messages, code);
                return BadRequest(errorMessage);
            }

            return Ok(result.Value);
        }

        /// <summary>
        /// Genera el PDF de Nota de Crédito para una venta anulada.
        /// Funciona para ventas al contado y para ventas CC.
        /// </summary>
        [Authorize(Policy = "PERM:VEN_READ")]
        [HttpGet("{idVenta:int}/credit-note-pdf")]
        public async Task<IActionResult> GetCreditNotePdf(int idVenta)
        {
            try
            {
                var pdfBytes = await _pdfService.GenerateAnnulReceiptAsync(idVenta);
                return File(
                    pdfBytes,
                    "application/pdf",
                    $"NotaCredito_{idVenta}_{DateTime.Now:yyyyMMdd}.pdf"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando PDF de nota de crédito para venta {idVenta}", idVenta);
                return StatusCode(500, new { message = "Error generando el PDF de nota de crédito." });
            }
        }

        [HttpGet("{idVenta:int}/pdf")]
        [Authorize(Policy = "PERM:VEN_READ")]
        public async Task<IActionResult> DownloadSalePdf(int idVenta)
        {
            try
            {
                var pdfBytes = await _pdfService.GenerateSalePdfAsync(idVenta);

                return File(
                    pdfBytes,
                    "application/pdf",
                    $"Venta_{idVenta}_{DateTime.Now:yyyyMMdd}.pdf"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando PDF de venta {id}", idVenta);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error generando el PDF"
                });
            }
        }
    }
}
