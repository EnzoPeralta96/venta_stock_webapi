namespace venta_stock_webapi.CurrentAccount.Message
{
    public enum CurrentAccountCode
    {
        account_not_found,
        account_already_active,
        unexpected_error,
        movement_not_found,
        payment_already_annulled,
        debit_note_reason_not_found,
        debit_note_reason_inactive,
        sale_not_found,
        sale_does_not_belong_to_client,
        no_active_config,
        interest_already_applied_this_month,
        client_has_no_debt,
        configuracion_cc_not_found,
        limit_already_set,
        global_payment_must_cover_full_debt
    }

    public static class CurrentAccountDictionary
    {
        public static readonly Dictionary<CurrentAccountCode, string> Messages = new()
        {
            { CurrentAccountCode.account_not_found, "La cuenta indicada no existe." },
            { CurrentAccountCode.account_already_active, "La cuenta corriente ya está activa." },
            { CurrentAccountCode.unexpected_error, "Ocurrió un error inesperado, por favor intente nuevamente." },
            { CurrentAccountCode.movement_not_found, "El movimiento indicado no existe o no es un pago." },
            { CurrentAccountCode.payment_already_annulled, "El pago indicado ya fue anulado previamente." },
            { CurrentAccountCode.debit_note_reason_not_found, "El motivo de nota de débito indicado no existe." },
            { CurrentAccountCode.debit_note_reason_inactive, "El motivo de nota de débito indicado no está activo." },
            { CurrentAccountCode.sale_not_found, "La venta indicada no existe." },
            { CurrentAccountCode.sale_does_not_belong_to_client, "La venta indicada no pertenece al cliente." },
            { CurrentAccountCode.no_active_config, "No hay ninguna configuración de interés activa. Configure una antes de continuar." },
            { CurrentAccountCode.interest_already_applied_this_month, "El interés por mora ya fue aplicado a este cliente en el mes en curso." },
            { CurrentAccountCode.client_has_no_debt, "El cliente no tiene deuda pendiente." },
            { CurrentAccountCode.configuracion_cc_not_found, "La configuración de límite indicada no existe o no está activa." },
            { CurrentAccountCode.limit_already_set, "El nuevo límite indicado es igual al límite actual." },
            { CurrentAccountCode.global_payment_must_cover_full_debt, "El pago global debe cubrir el total de la deuda. Para pagos parciales use el tipo 'Pago Parcial'." }
        };
    }
}
