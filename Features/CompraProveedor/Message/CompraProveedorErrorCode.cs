namespace proyecto_venta_stock.CompraProveedor.Message;

public enum CompraProveedorErrorCode
{
    compra_not_found,
    proveedor_not_found,
    usuario_not_found,
    producto_not_found,
    compra_ya_inactiva,
    compra_ya_activa,
    stock_insuficiente_para_revertir,
    numero_comprobante_duplicado,
    sin_detalles,
    error_inesperado
}

public static class CompraProveedorErrorDictionary
{
    public static readonly Dictionary<CompraProveedorErrorCode, string> Messages = new()
    {
        { CompraProveedorErrorCode.compra_not_found, "La compra indicada no existe." },
        { CompraProveedorErrorCode.proveedor_not_found, "El proveedor indicado no existe o está inactivo." },
        { CompraProveedorErrorCode.usuario_not_found, "El usuario indicado no existe." },
        { CompraProveedorErrorCode.producto_not_found, "Uno o más productos del detalle no existen o están inactivos." },
        { CompraProveedorErrorCode.compra_ya_inactiva, "La compra ya se encuentra inactiva." },
        { CompraProveedorErrorCode.compra_ya_activa, "La compra ya se encuentra activa." },
        { CompraProveedorErrorCode.stock_insuficiente_para_revertir, "No se puede revertir la compra porque algún producto no tiene suficiente stock." },
        { CompraProveedorErrorCode.numero_comprobante_duplicado, "Ya existe una compra con ese número de comprobante para el mismo proveedor." },
        { CompraProveedorErrorCode.sin_detalles, "La compra debe tener al menos un detalle." },
        { CompraProveedorErrorCode.error_inesperado, "Ocurrió un error inesperado. Por favor, intente nuevamente." }
    };
}
