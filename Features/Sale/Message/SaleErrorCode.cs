namespace venta_stock_webapi.Sale.Message
{
    public enum SaleErrorCode
    {
        client_not_found,
        product_not_found,
        product_inactive,
        insufficient_stock,
        sale_not_found,
        invalid_payment_method,
        empty_cart,
        client_inactive,
        unexpected_error
    }

    public static class SaleErrorDictionary
    {
        public static readonly Dictionary<SaleErrorCode, string> Messages = new()
        {
            { SaleErrorCode.client_not_found, "El cliente indicado no existe." },
            { SaleErrorCode.product_not_found, "Uno o más productos no existen." },
            { SaleErrorCode.product_inactive, "No se puede vender un producto inactivo." },
            { SaleErrorCode.insufficient_stock, "Stock insuficiente para uno o más productos." },
            { SaleErrorCode.sale_not_found, "La venta indicada no existe." },
            { SaleErrorCode.invalid_payment_method, "Medio de pago inválido." },
            { SaleErrorCode.empty_cart, "La venta debe contener al menos un producto." },
            { SaleErrorCode.client_inactive, "No se puede realizar una venta a un cliente inactivo." },
            { SaleErrorCode.unexpected_error, "Ocurrió un error inesperado, por favor intente nuevamente." }
        };
    }
}