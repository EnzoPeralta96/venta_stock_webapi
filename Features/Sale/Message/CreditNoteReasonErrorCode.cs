namespace venta_stock_webapi.Sale.Message;

public enum CreditNoteReasonCode
{
    reason_not_found,
    reason_name_exists,
    unexpected_error
}

public static class CreditNoteReasonDictionary
{
    public static readonly Dictionary<CreditNoteReasonCode, string> Messages = new()
    {
        { CreditNoteReasonCode.reason_not_found,  "El motivo de nota de crédito indicado no existe." },
        { CreditNoteReasonCode.reason_name_exists, "Ya existe un motivo de nota de crédito con ese nombre." },
        { CreditNoteReasonCode.unexpected_error,   "Ocurrió un error inesperado, por favor intente nuevamente." }
    };
}
