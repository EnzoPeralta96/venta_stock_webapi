namespace venta_stock_webapi.CurrentAccount.Services.CurrentAccountService.StrategyCurrentAccount
{
    public class MovementStrategyFactory
    {
        public IMovementStrategy GetStrategy(TypeMovement typeMovement)
        {
            return typeMovement switch
            {
                //TypeMovement.MOVIMIENTO_CC => new SaleStrategy(),
                TypeMovement.PAGO_GLOBAL => new PaymentStrategy(),
                //TypeMovement.INTEREST_ACCOUNT_GRAL => new InterestStrategy(),
                //TypeMovement.NOTA_DEBITO => new DebitNoteStrategy(),
                //TypeMovement.NOTA_CREDITO => new CreditNoteStrategy(),
                _ => throw new NotSupportedException($"The movement type {typeMovement} is not supported.")
            };
        }
    }
}