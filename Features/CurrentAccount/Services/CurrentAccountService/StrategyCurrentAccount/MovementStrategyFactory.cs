namespace venta_stock_webapi.CurrentAccount.Services.CurrentAccountService.StrategyCurrentAccount
{
    public class MovementStrategyFactory
    {
        public IMovementStrategy GetStrategy(TypeMovement typeMovement)
        {
            return typeMovement switch
            {
                TypeMovement.MOVIMIENTO_CC => new SaleStrategy(),
                TypeMovement.PAGO_GLOBAL => new PaymentStrategy(),
                TypeMovement.PAGO_PARCIAL => new PaymentStrategy(),
                TypeMovement.PAGO_FACTURA => new PaymentStrategy(),

                TypeMovement.NOTA_DEBITO => new DebitNoteStrategy(),
                TypeMovement.NOTA_CREDITO => new CreditNoteStrategy(),
                TypeMovement.ANULACION_PAGO => new DebitNoteStrategy(), // revertir un pago = sumar deuda de vuelta
                TypeMovement.MODIFICACION_LIMITE => new LimitModificationStrategy(),
                _ => throw new NotSupportedException($"The movement type {typeMovement} is not supported.")
            };
        }
    }
}