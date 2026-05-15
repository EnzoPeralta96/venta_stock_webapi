namespace venta_stock_webapi.CurrentAccount.Message;

public enum DebitNoteReasonCode
{
    reason_not_found,
    reason_name_exists,
    unexpected_error
}

public static class DebitNoteReasonDictionary
{
    public static readonly Dictionary<DebitNoteReasonCode, string> Messages = new()
    {
        { DebitNoteReasonCode.reason_not_found,
          "El motivo de nota de débito indicado no existe." },
        { DebitNoteReasonCode.reason_name_exists,
          "Ya existe un motivo de nota de débito con ese nombre." },
        { DebitNoteReasonCode.unexpected_error,
          "Ocurrió un error inesperado, por favor intente nuevamente." }
    };
}
