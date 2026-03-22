using proyecto_venta_stock.Models;
using proyecto_venta_stock.Shared.ResultPattern;
using venta_stock_webapi.Client.Repository;
using venta_stock_webapi.Sale.DTO;
using venta_stock_webapi.Sale.Message;
using venta_stock_webapi.Sale.Services;

namespace venta_stock_webapi.Sale.Strategies
{
    /// <summary>
    /// Estrategia de venta para el medio de pago "Cuenta Corriente".
    /// </summary>
    /// <remarks>
    /// Responsabilidad:
    /// - Validar que el cliente tenga cuenta corriente inicializada.
    /// - Validar que el monto de la venta no exceda el crédito disponible actual.
    /// - Si excede el disponible, generar una venta pendiente de autorización (Pending Sale) y cortar el flujo.
    /// - Si NO excede, permitir que la venta continúe su flujo normal.
    ///
    /// Importante:
    /// - Esta estrategia NO persiste la venta ni crea movimientos de cuenta corriente.
    ///   La persistencia de la venta, actualización de stock y la creación del movimiento de CC
    ///   se realizan en el servicio/orquestador (SaleService) una vez que la venta tiene IdVenta.
    /// - La validación de "disponible" asume que el repositorio de cliente devuelve:
    ///   - LimiteCuenta = crédito disponible actual,
    ///   - SaldoActual = saldo/deuda actual.
    /// </remarks>
    /// <example>
    /// Si disponibleActual = 10.000 y montoVenta = 12.000:
    /// - Se crea Pending Sale y se retorna error de límite excedido.
    /// </example>
    public class CreditSaleStrategy : ISaleStrategy
    {
        private readonly IClientRepository _clientRepository;
        private readonly IPendingSaleService _pendingSaleService;
        private readonly ILogger<CreditSaleStrategy> _logger;

        public CreditSaleStrategy(
            IClientRepository clientRepository,
            IPendingSaleService pendingSaleService,
            ILogger<CreditSaleStrategy> logger)
        {
            _clientRepository = clientRepository;
            _pendingSaleService = pendingSaleService;
            _logger = logger;
        }

        public async Task<Result<Ventum>> ProcessSaleAsync(
            CreateSaleDTO saleDTO,
            Ventum venta,
            List<DetalleVentum> detalles)
        {
            try
            {
                _logger.LogInformation("Procesando venta en CUENTA CORRIENTE: {codigo}", venta.CodigoVenta);

                var montoVenta = venta.Total ?? 0m;

                if (montoVenta <= 0m)
                    return Result<Ventum>.Failure(SaleErrorCode.invalid_sale_total);

                var creditInfo = await _clientRepository.ObtenerInfoCreditoAsync(saleDTO.idCliente);

                if (creditInfo is null)
                {
                    _logger.LogWarning("Cliente {id} no tiene cuenta corriente inicializada", saleDTO.idCliente);
                    return Result<Ventum>.Failure(SaleErrorCode.client_no_credit_account);
                }

                // Interpretación: LimiteCuenta = crédito disponible actual
                var disponibleActual = creditInfo.LimiteCuenta;

                _logger.LogDebug(
                    "Crédito disponible - Cliente: {id}, Disponible: {disp}, Monto venta: {monto}",
                    saleDTO.idCliente, disponibleActual, montoVenta
                );

                // Si no hay disponible, también excede
                if (disponibleActual <= 0m || montoVenta > disponibleActual)
                {
                    var excedente = montoVenta - Math.Max(disponibleActual, 0m);
                    var porcentajeExcedente = disponibleActual > 0m
                        ? (excedente / disponibleActual) * 100m
                        : 100m;

                    _logger.LogWarning(
                        "Venta EXCEDE crédito disponible - Cliente: {id}, Excedente: ${excedente} ({porcentaje}%). " +
                        "Creando venta PENDIENTE de autorización",
                        saleDTO.idCliente, excedente, Math.Round(porcentajeExcedente, 2)
                    );

                    var pendingResult = await _pendingSaleService.CreatePendingSaleAsync(
                        saleDTO,
                        creditInfo.SaldoActual,
                        creditInfo.LimiteCuenta
                    );

                    if (!pendingResult.IsSuccess)
                        return Result<Ventum>.Failure(pendingResult.ErrorCode);

                    return Result<Ventum>.Failure(SaleErrorCode.credit_limit_exceeded);
                }

                // No excede: SaleService continúa, guarda venta y luego registra movimiento CC.
                return Result<Ventum>.Success(venta);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando venta en cuenta corriente");
                return Result<Ventum>.Failure(SaleErrorCode.unexpected_error);
            }
        }
    }
}