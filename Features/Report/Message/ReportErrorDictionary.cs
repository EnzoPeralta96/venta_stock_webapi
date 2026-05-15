namespace proyecto_venta_stock.Report.Message
{
    public static class ReportErrorDictionary
    {
        public static readonly Dictionary<ReportErrorCode, string> Messages = new()
        {
            { ReportErrorCode.fecha_invalida,       "La fecha de inicio no puede ser posterior a la fecha de fin." },
            { ReportErrorCode.agrupacion_invalida,  "El valor de agrupación no es válido. Use: dia, semana o mes." },
            { ReportErrorCode.sin_datos,            "No se encontró el artículo más vendido en el período indicado." },
            { ReportErrorCode.unexpected_error,     "Ocurrió un error inesperado al generar el reporte." }
        };
    }
}
