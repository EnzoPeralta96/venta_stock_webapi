using proyecto_venta_stock.Data;
using proyecto_venta_stock.Models;
using proyecto_venta_stock.Shared.ResultPattern;
using venta_stock_webapi.Client.Repository;
using venta_stock_webapi.Sale.DTO;
using venta_stock_webapi.Sale.Message;
using venta_stock_webapi.Sale.Services;

namespace venta_stock_webapi.Sale.Strategies
{
    /// <summary>
    /// Estrategia para procesar ventas en CUENTA CORRIENTE
    /// </summary>
    /// <remarks>
    /// Flujo:
    /// 1. Obtiene información de crédito del cliente (ClientRepository)
    /// 2. Valida si tiene cuenta corriente y límite configurado
    /// 3. Calcula nuevo saldo después de la venta
    /// 4. Si EXCEDE límite → Crea venta PENDIENTE (requiere autorización)
    /// 5. Si NO excede límite → Procesa venta normal (crear movimiento CC)
    /// 
    /// La lógica común (guardar venta, detalles, stock) está en SaleService
    /// </remarks>
    public class CreditSaleStrategy : ISaleStrategy
    {
        private readonly VentaStockContext _context;
        private readonly IClientRepository _clientRepository;
        private readonly IPendingSaleService _pendingSaleService;
        private readonly ILogger<CreditSaleStrategy> _logger;

        public CreditSaleStrategy(
            VentaStockContext context,
            IClientRepository clientRepository,
            IPendingSaleService pendingSaleService,
            ILogger<CreditSaleStrategy> logger)
        {
            _context = context;
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

                var montoVenta = venta.Total ?? 0;

                // ===== 1. OBTENER INFORMACIÓN DE CRÉDITO =====
                // Repository solo provee DATOS (no valida)
                
                var creditInfo = await _clientRepository.ObtenerInfoCreditoAsync(saleDTO.idCliente);

                // ===== 2. VALIDAR DATOS OBTENIDOS =====
                
                // Cliente sin cuenta corriente inicializada
                if (creditInfo == null)
                {
                    _logger.LogWarning(
                        "Cliente {id} no tiene cuenta corriente inicializada",
                        saleDTO.idCliente
                    );
                    return Result<Ventum>.Failure(SaleErrorCode.client_no_credit_account);
                }


                // ===== 3. CALCULAR NUEVO SALDO =====
                
                var nuevoSaldo = creditInfo.SaldoActual + montoVenta;

                _logger.LogDebug(
                    "Cálculo de crédito - Cliente: {id}, Saldo actual: {saldo}, " +
                    "Monto venta: {monto}, Nuevo saldo: {nuevo}, Límite: {limite}",
                    saleDTO.idCliente, creditInfo.SaldoActual, montoVenta, 
                    nuevoSaldo, creditInfo.LimiteCuenta
                );

                // ===== 4. VERIFICAR SI EXCEDE LÍMITE =====
                
                if (nuevoSaldo > creditInfo.LimiteCuenta)
                {
                    // ===== EXCEDE LÍMITE → CREAR VENTA PENDIENTE =====
                    
                    var excedente = nuevoSaldo - creditInfo.LimiteCuenta;
                    var porcentajeExcedente = (excedente / creditInfo.LimiteCuenta) * 100;

                    _logger.LogWarning(
                        "Venta EXCEDE límite de crédito - Cliente: {id}, " +
                        "Excedente: ${excedente} ({porcentaje}%). " +
                        "Creando venta PENDIENTE de autorización",
                        saleDTO.idCliente, excedente, Math.Round(porcentajeExcedente, 2)
                    );

                    // Crear venta pendiente (NO venta definitiva)
                    var pendingResult = await _pendingSaleService.CreatePendingSaleAsync(
                        saleDTO,
                        creditInfo.SaldoActual,
                        creditInfo.LimiteCuenta
                    );

                    if (!pendingResult.IsSuccess)
                    {
                        _logger.LogError(
                            "Error creando venta pendiente para cliente {id}",
                            saleDTO.idCliente
                        );
                        return Result<Ventum>.Failure(pendingResult.ErrorCode);
                    }

                    _logger.LogInformation(
                        "Venta pendiente creada exitosamente: {codigo}, " +
                        "ID pendiente: {id}, Excedente: ${excedente}",
                        pendingResult.Value.CodigoVenta,
                        pendingResult.Value.IdVentaPendiente,
                        excedente
                    );

                    // Retornar error especial indicando que quedó pendiente
                    // El SaleService debe manejar este caso y retornar la info de venta pendiente
                    return Result<Ventum>.Failure(
                        SaleErrorCode.credit_limit_exceeded // Pasar la venta pendiente como data adicional
                    );
                }

                // ===== 5. NO EXCEDE LÍMITE → PROCESAR VENTA NORMAL =====

                _logger.LogInformation(
                    "Límite verificado OK - Cliente: {id}, " +
                    "Nuevo saldo: {nuevo}/{limite} ({porcentaje}%), " +
                    "Disponible restante: ${disponible}",
                    saleDTO.idCliente,
                    nuevoSaldo,
                    creditInfo.LimiteCuenta,
                    Math.Round((nuevoSaldo / creditInfo.LimiteCuenta) * 100, 2),
                    creditInfo.LimiteCuenta - nuevoSaldo
                );

                // ===== 6. RETORNAR ÉXITO =====
                // El SaleService se encarga de:
                // - Guardar la venta y detalles
                // - CREAR el movimiento CC (ahora que la venta tiene ID)
                // - Actualizar el stock
                // - Commitear la transacción
                //
                // NOTA: El movimiento CC se crea en SaleService DESPUÉS de guardar
                // la venta para poder usar venta.IdVenta correctamente

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