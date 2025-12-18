namespace venta_stock_webapi.Sale.Message
{
    public enum SaleErrorCode
    {
        // Errores de cliente
        client_not_found,
        client_inactive,
        client_no_credit_account,
        client_no_credit_limit,
        
        // Errores de producto
        product_not_found,
        product_inactive,
        insufficient_stock,
        invalid_quantity,
        
        // Errores de venta
        invalid_payment_method,
        empty_cart,
        sale_not_found,
        
        // Errores de cuenta corriente
        credit_limit_exceeded,
        
        // Errores generales
        unexpected_error
    }

    public static class SaleErrorDictionary
    {
        public static readonly Dictionary<SaleErrorCode, string> Messages = new()
        {
            // Errores de cliente
            { SaleErrorCode.client_not_found, "El cliente indicado no existe." },
            { SaleErrorCode.client_inactive, "No se puede realizar una venta a un cliente inactivo." },
            { SaleErrorCode.client_no_credit_account, "El cliente no tiene cuenta corriente inicializada." },
            { SaleErrorCode.client_no_credit_limit, "El cliente no tiene límite de crédito configurado." },
            
            // Errores de producto
            { SaleErrorCode.product_not_found, "Uno o más productos no existen." },
            { SaleErrorCode.product_inactive, "No se puede vender un producto inactivo." },
            { SaleErrorCode.insufficient_stock, "Stock insuficiente para uno o más productos." },
            { SaleErrorCode.invalid_quantity, "La cantidad debe ser mayor a cero." },
            
            // Errores de venta
            { SaleErrorCode.invalid_payment_method, "Medio de pago inválido." },
            { SaleErrorCode.empty_cart, "La venta debe contener al menos un producto." },
            { SaleErrorCode.sale_not_found, "La venta indicada no existe." },
            
            // Errores de cuenta corriente
            { SaleErrorCode.credit_limit_exceeded, "Límite de crédito excedido. No hay cupo disponible suficiente." },
            
            // Errores generales
            { SaleErrorCode.unexpected_error, "Ocurrió un error inesperado, por favor intente nuevamente." }
        };
    }
}