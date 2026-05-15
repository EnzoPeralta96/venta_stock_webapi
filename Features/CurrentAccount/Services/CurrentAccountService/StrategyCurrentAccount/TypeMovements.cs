namespace venta_stock_webapi.CurrentAccount.Services.CurrentAccountService.StrategyCurrentAccount
{
    public enum TypeMovement
    {
        ALTA_CLIENTE = 2,
        NOTA_DEBITO = 3,
        NOTA_CREDITO = 4,
        MOVIMIENTO_CC = 5,
        PAGO_GLOBAL = 6,

        PAGO_FACTURA = 8,
        ANULACION_PAGO = 9,
        MODIFICACION_LIMITE = 10,
        PAGO_PARCIAL = 11
    }
}