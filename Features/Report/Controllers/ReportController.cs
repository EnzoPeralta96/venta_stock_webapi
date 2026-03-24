using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using proyecto_venta_stock.Report.Message;
using proyecto_venta_stock.Report.Services;
using venta_stock_webapi.Shared.MessageProvider;

namespace proyecto_venta_stock.Report.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReportController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportController(IReportService reportService)
        {
            _reportService = reportService;
        }

        // GET api/Report/total-vendido?fechaDesde=2024-01-01&fechaHasta=2024-12-31
        [HttpGet("total-vendido")]
        [Authorize(Policy = "PERM:REP_GENERATE")]
        public async Task<IActionResult> GetTotalVendido(
            [FromQuery] DateTime fechaDesde,
            [FromQuery] DateTime fechaHasta)
        {
            var result = await _reportService.GetTotalVendidoAsync(fechaDesde, fechaHasta);

            if (!result.IsSuccess)
            {
                var message = MessageProvider.Get(ReportErrorDictionary.Messages, (ReportErrorCode)result.ErrorCode);
                return BadRequest(message);
            }

            return Ok(result.Value);
        }

        // GET api/Report/ventas-por-periodo?fechaDesde=2024-01-01&fechaHasta=2024-12-31&agrupacion=mes
        [HttpGet("ventas-por-periodo")]
        [Authorize(Policy = "PERM:REP_GENERATE")]
        public async Task<IActionResult> GetVentasPorPeriodo(
            [FromQuery] DateTime fechaDesde,
            [FromQuery] DateTime fechaHasta,
            [FromQuery] string agrupacion = "mes")
        {
            var result = await _reportService.GetVentasPorPeriodoAsync(fechaDesde, fechaHasta, agrupacion);

            if (!result.IsSuccess)
            {
                var message = MessageProvider.Get(ReportErrorDictionary.Messages, (ReportErrorCode)result.ErrorCode);
                return BadRequest(message);
            }

            return Ok(result.Value);
        }

        // GET api/Report/articulo-mas-vendido?fechaDesde=2024-01-01&fechaHasta=2024-12-31
        [HttpGet("articulo-mas-vendido")]
        [Authorize(Policy = "PERM:REP_GENERATE")]
        public async Task<IActionResult> GetArticuloMasVendido(
            [FromQuery] DateTime fechaDesde,
            [FromQuery] DateTime fechaHasta)
        {
            var result = await _reportService.GetArticuloMasVendidoAsync(fechaDesde, fechaHasta);

            if (!result.IsSuccess)
            {
                var code    = (ReportErrorCode)result.ErrorCode;
                var message = MessageProvider.Get(ReportErrorDictionary.Messages, code);
                return code == ReportErrorCode.sin_datos ? NotFound(message) : BadRequest(message);
            }

            return Ok(result.Value);
        }

        // GET api/Report/productos-mas-vendidos?fechaDesde=2024-01-01&fechaHasta=2024-12-31&topN=10&idCategoria=3
        [HttpGet("productos-mas-vendidos")]
        [Authorize(Policy = "PERM:REP_GENERATE")]
        public async Task<IActionResult> GetProductosMasVendidos(
            [FromQuery] DateTime fechaDesde,
            [FromQuery] DateTime fechaHasta,
            [FromQuery] int topN = 10,
            [FromQuery] int? idCategoria = null)
        {
            var result = await _reportService.GetProductosMasVendidosAsync(fechaDesde, fechaHasta, topN, idCategoria);

            if (!result.IsSuccess)
            {
                var message = MessageProvider.Get(ReportErrorDictionary.Messages, (ReportErrorCode)result.ErrorCode);
                return BadRequest(message);
            }

            return Ok(result.Value);
        }

        // GET api/Report/categorias-mas-vendidas?fechaDesde=2024-01-01&fechaHasta=2024-12-31
        [HttpGet("categorias-mas-vendidas")]
        [Authorize(Policy = "PERM:REP_GENERATE")]
        public async Task<IActionResult> GetCategoriasMasVendidas(
            [FromQuery] DateTime fechaDesde,
            [FromQuery] DateTime fechaHasta)
        {
            var result = await _reportService.GetCategoriasMasVendidasAsync(fechaDesde, fechaHasta);

            if (!result.IsSuccess)
            {
                var message = MessageProvider.Get(ReportErrorDictionary.Messages, (ReportErrorCode)result.ErrorCode);
                return BadRequest(message);
            }

            return Ok(result.Value);
        }

        // GET api/Report/margen-utilidad?fechaDesde=2024-01-01&fechaHasta=2024-12-31
        [HttpGet("margen-utilidad")]
        [Authorize(Policy = "PERM:REP_GENERATE")]
        public async Task<IActionResult> GetMargenUtilidad(
            [FromQuery] DateTime fechaDesde,
            [FromQuery] DateTime fechaHasta)
        {
            var result = await _reportService.GetMargenUtilidadAsync(fechaDesde, fechaHasta);

            if (!result.IsSuccess)
            {
                var message = MessageProvider.Get(ReportErrorDictionary.Messages, (ReportErrorCode)result.ErrorCode);
                return BadRequest(message);
            }

            return Ok(result.Value);
        }

        // GET api/Report/clientes-frecuentes?fechaDesde=2024-01-01&fechaHasta=2024-12-31&topN=10
        [HttpGet("clientes-frecuentes")]
        [Authorize(Policy = "PERM:REP_GENERATE")]
        public async Task<IActionResult> GetClientesFrecuentes(
            [FromQuery] DateTime fechaDesde,
            [FromQuery] DateTime fechaHasta,
            [FromQuery] int topN = 10)
        {
            var result = await _reportService.GetClientesMasFrecuentesAsync(fechaDesde, fechaHasta, topN);

            if (!result.IsSuccess)
            {
                var message = MessageProvider.Get(ReportErrorDictionary.Messages, (ReportErrorCode)result.ErrorCode);
                return BadRequest(message);
            }

            return Ok(result.Value);
        }

        // GET api/Report/tiempo-promedio-cobro?fechaDesde=2024-01-01&fechaHasta=2024-12-31
        [HttpGet("tiempo-promedio-cobro")]
        [Authorize(Policy = "PERM:REP_GENERATE")]
        public async Task<IActionResult> GetTiempoPromedioCobro(
            [FromQuery] DateTime fechaDesde,
            [FromQuery] DateTime fechaHasta)
        {
            var result = await _reportService.GetTiempoPromedioCobroAsync(fechaDesde, fechaHasta);

            if (!result.IsSuccess)
            {
                var message = MessageProvider.Get(ReportErrorDictionary.Messages, (ReportErrorCode)result.ErrorCode);
                return BadRequest(message);
            }

            return Ok(result.Value);
        }

        // GET api/Report/deuda-total
        [HttpGet("deuda-total")]
        [Authorize(Policy = "PERM:REP_GENERATE")]
        public async Task<IActionResult> GetDeudaTotal()
        {
            var result = await _reportService.GetMontoTotalAdeudadoAsync();

            if (!result.IsSuccess)
            {
                var message = MessageProvider.Get(ReportErrorDictionary.Messages, (ReportErrorCode)result.ErrorCode);
                return BadRequest(message);
            }

            return Ok(result.Value);
        }

        // GET api/Report/clientes-saldo-deudor
        [HttpGet("clientes-saldo-deudor")]
        [Authorize(Policy = "PERM:REP_GENERATE")]
        public async Task<IActionResult> GetClientesSaldoDeudor()
        {
            var result = await _reportService.GetClientesSaldoDeudorAsync();

            if (!result.IsSuccess)
            {
                var message = MessageProvider.Get(ReportErrorDictionary.Messages, (ReportErrorCode)result.ErrorCode);
                return BadRequest(message);
            }

            return Ok(result.Value);
        }
    }
}
