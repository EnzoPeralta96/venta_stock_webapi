using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace venta_stock_webapi.Sale.Strategies
{
    /// <summary>
    /// Interface del Factory que crea estrategias de venta
    /// </summary>
    public interface ISaleStrategyFactory
    {
        /// <summary>
        /// Obtiene la estrategia correspondiente al medio de pago
        /// </summary>
        /// <param name="idMedioPago">ID del medio de pago</param>
        /// <returns>Estrategia de procesamiento de venta</returns>
        /// <exception cref="ArgumentException">Si el medio de pago no está soportado</exception>
        ISaleStrategy GetStrategy(int idMedioPago);
    }

    /// <summary>
    /// Factory que crea instancias de estrategias de venta según el medio de pago
    /// </summary>
    /// <remarks>
    /// Mapeo de medios de pago:
    /// 1 = Efectivo → CashSaleStrategy
    /// 2 = Cuenta Corriente → CreditSaleStrategy
    /// 
    /// Para agregar un nuevo medio de pago:
    /// 1. Crear la clase XxxSaleStrategy : ISaleStrategy
    /// 2. Registrarla en Program.cs como Scoped
    /// 3. Agregarla al switch en GetStrategy()
    /// </remarks>
    public class SaleStrategyFactory : ISaleStrategyFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SaleStrategyFactory> _logger;

        public SaleStrategyFactory(
            IServiceProvider serviceProvider,
            ILogger<SaleStrategyFactory> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public ISaleStrategy GetStrategy(int idMedioPago)
        {
            _logger.LogDebug("Obteniendo estrategia para medio de pago: {id}", idMedioPago);

            ISaleStrategy strategy = idMedioPago switch
            {
                1 => _serviceProvider.GetRequiredService<CashSaleStrategy>(),
                2 => _serviceProvider.GetRequiredService<CreditSaleStrategy>(),
                _ => throw new ArgumentException(
                    $"Medio de pago {idMedioPago} no soportado. " +
                    $"Medios válidos: 1=Efectivo, 2=Cuenta Corriente",
                    nameof(idMedioPago)
                )
            };

            _logger.LogDebug("Estrategia seleccionada: {strategy}", strategy.GetType().Name);

            return strategy;
        }
    }
}