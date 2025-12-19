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
        private readonly ILogger<SaleController> _logger;

        public SaleController(ISaleServices saleService, ILogger<SaleController> logger)
        {
            _saleService = saleService;
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
            [FromQuery] DateTime? fechaHasta = null)
        {
            _logger.LogInformation(
                "Listando ventas - Página: {PageNumber}, Tamaño: {PageSize}, Cliente: {ClienteFilter}, Desde: {FechaDesde}, Hasta: {FechaHasta}",
                pageNumber, pageSize, clienteFilter, fechaDesde, fechaHasta
            );

            var result = await _saleService.GetSalesPagedAsync(
                pageNumber,
                pageSize,
                clienteFilter,
                fechaDesde,
                fechaHasta
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

            return Ok(result.Value);
        }
    }
}
