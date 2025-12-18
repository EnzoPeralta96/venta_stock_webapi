using proyecto_venta_stock.Data;
using proyecto_venta_stock.Models;
using proyecto_venta_stock.Shared.ResultPattern;
using venta_stock_webapi.Client.Repository;
using venta_stock_webapi.Sale.DTO;
using venta_stock_webapi.Sale.Message;

namespace venta_stock_webapi.Sale.Strategies
{
    /// <summary>
    /// Estrategia para procesar ventas en CUENTA CORRIENTE
    /// </summary>
    /// <remarks>
    /// Lógica específica:
    /// 1. Obtener información de crédito del cliente (usa ClientRepository)
    /// 2. VALIDAR límite de crédito (responsabilidad de la estrategia)
    /// 3. Crear movimiento en cuenta corriente (tipo: movimiento_cc)
    /// 
    /// La lógica común (guardar venta, detalles, stock) está en SaleService
    /// </remarks>
    public class CreditSaleStrategy : ISaleStrategy
    {
        private readonly VentaStockContext _context;
        private readonly IClientRepository _clientRepository;
        private readonly ILogger<CreditSaleStrategy> _logger;

        public CreditSaleStrategy(
            VentaStockContext context,
            IClientRepository clientRepository,
            ILogger<CreditSaleStrategy> logger)
        {
            _context = context;
            _clientRepository = clientRepository;
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

                // Cliente sin límite de crédito configurado
                if (creditInfo.LimiteCuenta <= 0)
                {
                    _logger.LogWarning(
                        "Cliente {id} no tiene límite de crédito configurado",
                        saleDTO.idCliente
                    );
                    return Result<Ventum>.Failure(SaleErrorCode.client_no_credit_limit);
                }

                // ===== 3. VALIDAR LÍMITE DE CRÉDITO =====
                // Esta es responsabilidad de la ESTRATEGIA (lógica de negocio)
                
                var nuevoSaldo = creditInfo.SaldoActual + montoVenta;

                if (nuevoSaldo > creditInfo.LimiteCuenta)
                {
                    _logger.LogWarning(
                        "Límite de crédito excedido - Cliente: {id}, " +
                        "Saldo actual: {saldo}, Monto venta: {monto}, " +
                        "Nuevo saldo: {nuevo}, Límite: {limite}, Disponible: {disponible}",
                        saleDTO.idCliente,
                        creditInfo.SaldoActual,
                        montoVenta,
                        nuevoSaldo,
                        creditInfo.LimiteCuenta,
                        creditInfo.LimiteDisponible
                    );
                    
                    return Result<Ventum>.Failure(SaleErrorCode.credit_limit_exceeded);
                }

                _logger.LogInformation(
                    "Límite verificado OK - Cliente: {id}, Nuevo saldo: {nuevo}/{limite} ({porcentaje}%)",
                    saleDTO.idCliente,
                    nuevoSaldo,
                    creditInfo.LimiteCuenta,
                    Math.Round((nuevoSaldo / creditInfo.LimiteCuenta) * 100, 2)
                );

                // ===== 4. CREAR MOVIMIENTO EN CUENTA CORRIENTE =====
                // Tipo de movimiento: 5 = movimiento_cc (consumo por venta)
                
                var movimiento = new MovimientoCc
                {
                    IdCliente = saleDTO.idCliente,
                    IdVenta = venta.IdVenta,
                    IdTipoMovimiento = 5,  // movimiento_cc (según la imagen)
                    Importe = montoVenta,
                    Fecha = DateTime.Now,
                    Detalle = $"Venta {venta.CodigoVenta}",
                    IdEstado = 2,  // Completada (ajustar según tu tabla estado)
                    SaldoActual = nuevoSaldo,
                    LimiteCuenta = creditInfo.LimiteCuenta,
                    IdUsuarioRegistra = saleDTO.idUsuarioVendedor,
                    FechaAutorizacion = null,  // No requiere autorización para ventas simples
                    IdUsuarioAutoriza = null
                };

                await _context.MovimientoCcs.AddAsync(movimiento);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Movimiento CC creado - Venta: {codigo}, ID Movimiento: {idMov}, " +
                    "Saldo nuevo: {saldo}, Límite disponible: {disponible}",
                    venta.CodigoVenta,
                    movimiento.IdMovimiento,
                    nuevoSaldo,
                    creditInfo.LimiteCuenta - nuevoSaldo
                );

                // ===== 5. RETORNAR ÉXITO =====
                // El SaleService se encarga de:
                // - Guardar la venta y detalles
                // - Actualizar el stock
                // - Commitear la transacción

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