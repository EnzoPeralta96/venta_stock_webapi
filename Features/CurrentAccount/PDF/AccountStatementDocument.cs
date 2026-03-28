using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using proyecto_venta_stock.Models;

namespace venta_stock_webapi.CurrentAccount.PDF
{
    // ───── Data source ─────────────────────────────────────────────────────────
    public class AccountStatementDataSource
    {
        public Ferreteria DatosFerreteria { get; set; } = new();
        public string ClienteNombre { get; set; } = "";
        public string ClienteDni { get; set; } = "";
        public DateTime FechaGeneracion { get; set; } = DateTime.Now;
        public string? FiltroFechaDesde { get; set; }
        public string? FiltroFechaHasta { get; set; }
        public decimal SaldoActual { get; set; }
        public decimal LimiteCredito { get; set; }
        public List<AccountStatementItemPdf> Movimientos { get; set; } = new();
    }

    public class AccountStatementItemPdf
    {
        public int Numero { get; set; }
        public string Fecha { get; set; } = "";
        public string TipoMovimiento { get; set; } = "";
        public string Detalle { get; set; } = "";
        public decimal? Debe { get; set; }   // Cargos: venta CC, ND, interés, anulación de pago
        public decimal? Haber { get; set; }  // Abonos: pagos, NC
        public decimal? Saldo { get; set; }  // = SaldoActual precalculado
    }

    // ───── Document ────────────────────────────────────────────────────────────
    public class AccountStatementDocument : IDocument
    {
        private readonly AccountStatementDataSource _data;

        public AccountStatementDocument(AccountStatementDataSource data) => _data = data;

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(35);
                page.DefaultTextStyle(x => x.FontSize(9));
                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                page.Footer().Element(ComposeFooter);
            });
        }

        void ComposeHeader(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().Row(row =>
                {
                    // Datos ferretería
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text(_data.DatosFerreteria.Nombre)
                            .FontSize(16).Bold().FontColor(Colors.Blue.Darken2);
                        c.Item().Text(_data.DatosFerreteria.Direccion).FontSize(8);
                        c.Item().Text($"Tel: {_data.DatosFerreteria.Telefono}").FontSize(8);
                        c.Item().Text($"CUIT: {_data.DatosFerreteria.Cuit}").FontSize(8).Bold();
                    });

                    // Título
                    row.ConstantItem(220).Column(c =>
                    {
                        c.Item().AlignRight().Text("ESTADO DE CUENTA CORRIENTE")
                            .FontSize(13).Bold().FontColor(Colors.Blue.Darken2);
                        c.Item().AlignRight()
                            .Text($"Generado: {_data.FechaGeneracion:dd/MM/yyyy HH:mm}")
                            .FontSize(8).FontColor(Colors.Grey.Darken1);
                        if (!string.IsNullOrWhiteSpace(_data.FiltroFechaDesde) || !string.IsNullOrWhiteSpace(_data.FiltroFechaHasta))
                        {
                            var rango = $"Período: {_data.FiltroFechaDesde ?? "–"} al {_data.FiltroFechaHasta ?? "–"}";
                            c.Item().AlignRight().Text(rango).FontSize(8).FontColor(Colors.Grey.Darken1);
                        }
                    });
                });

                col.Item().PaddingTop(6).BorderBottom(2).BorderColor(Colors.Blue.Darken2);

                // Info cliente + KPIs
                col.Item().PaddingTop(6).Row(row =>
                {
                    // Cliente
                    row.RelativeItem().Background(Colors.Grey.Lighten3).Padding(8).Column(c =>
                    {
                        c.Item().Text("CLIENTE").FontSize(8).Bold().FontColor(Colors.Grey.Darken2);
                        c.Item().Text(_data.ClienteNombre).FontSize(11).Bold();
                        c.Item().Text($"DNI/CUIT: {_data.ClienteDni}").FontSize(8);
                    });

                    row.ConstantItem(10);

                    // Saldo
                    KpiCard(row.RelativeItem(), "Saldo Deudor", $"${_data.SaldoActual:N2}",
                        _data.SaldoActual > 0 ? Colors.Red.Lighten4 : Colors.Green.Lighten4);
                    row.ConstantItem(8);

                    // Límite
                    KpiCard(row.RelativeItem(), "Límite de Crédito", $"${_data.LimiteCredito:N2}", Colors.Blue.Lighten4);
                    row.ConstantItem(8);

                    var disponible = _data.LimiteCredito - _data.SaldoActual;
                    KpiCard(row.RelativeItem(), "Disponible", $"${disponible:N2}",
                        disponible >= 0 ? Colors.Green.Lighten4 : Colors.Orange.Lighten4);
                });

                col.Item().PaddingTop(8);
            });
        }

        static void KpiCard(IContainer container, string label, string value, string bgColor)
        {
            container.Background(bgColor).Padding(8).Column(c =>
            {
                c.Item().Text(label).FontSize(7).FontColor(Colors.Grey.Darken2);
                c.Item().Text(value).FontSize(11).Bold();
            });
        }

        void ComposeContent(IContainer container)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.ConstantColumn(22);   // #
                    cols.ConstantColumn(62);   // Fecha
                    cols.RelativeColumn(2);    // Tipo
                    cols.RelativeColumn(3);    // Detalle
                    cols.ConstantColumn(80);   // Debe
                    cols.ConstantColumn(80);   // Haber
                    cols.ConstantColumn(85);   // Saldo
                });

                // Header
                table.Header(header =>
                {
                    IContainer H(IContainer c) => c
                        .Background(Colors.Blue.Darken2).Padding(5)
                        .DefaultTextStyle(x => x.FontColor(Colors.White).Bold());

                    header.Cell().Element(H).Text("#");
                    header.Cell().Element(H).Text("Fecha");
                    header.Cell().Element(H).Text("Tipo");
                    header.Cell().Element(H).Text("Detalle / Comprobante");
                    header.Cell().Element(H).AlignRight().Text("Debe");
                    header.Cell().Element(H).AlignRight().Text("Haber");
                    header.Cell().Element(H).AlignRight().Text("Saldo");
                });

                // Rows
                foreach (var m in _data.Movimientos)
                {
                    bool even = m.Numero % 2 == 0;

                    IContainer R(IContainer c) => c
                        .Background(even ? Colors.Grey.Lighten4 : Colors.White)
                        .BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(4);

                    table.Cell().Element(R).Text(m.Numero.ToString()).FontColor(Colors.Grey.Darken1);
                    table.Cell().Element(R).Text(m.Fecha);
                    table.Cell().Element(R).Text(m.TipoMovimiento);
                    table.Cell().Element(R).Text(m.Detalle);

                    // Debe
                    table.Cell().Element(R).AlignRight()
                        .Text(m.Debe.HasValue ? $"${m.Debe.Value:N2}" : "-")
                        .FontColor(m.Debe.HasValue ? Colors.Red.Darken1 : Colors.Grey.Medium);

                    // Haber
                    table.Cell().Element(R).AlignRight()
                        .Text(m.Haber.HasValue ? $"${m.Haber.Value:N2}" : "-")
                        .FontColor(m.Haber.HasValue ? Colors.Green.Darken1 : Colors.Grey.Medium);

                    // Saldo
                    table.Cell().Element(R).AlignRight()
                        .Text(m.Saldo.HasValue ? $"${m.Saldo.Value:N2}" : "-")
                        .Bold()
                        .FontColor(m.Saldo.GetValueOrDefault() > 0 ? Colors.Red.Darken1 : Colors.Green.Darken1);
                }

                // Fila resumen final
                IContainer Tot(IContainer c) => c
                    .Background(Colors.Blue.Lighten4).Padding(5);

                table.Cell().ColumnSpan(6).Element(Tot)
                    .AlignRight().Text("SALDO FINAL:").Bold();
                table.Cell().Element(Tot).AlignRight()
                    .Text($"${_data.SaldoActual:N2}").Bold()
                    .FontColor(_data.SaldoActual > 0 ? Colors.Red.Darken2 : Colors.Green.Darken2);
            });
        }

        void ComposeFooter(IContainer container)
        {
            container.AlignCenter().BorderTop(1).BorderColor(Colors.Grey.Lighten2)
                .PaddingTop(5).Row(row =>
                {
                    row.RelativeItem().Text("El saldo refleja la posición al momento de generación del documento.").FontSize(7).FontColor(Colors.Grey.Darken1).Italic();
                    row.RelativeItem().AlignRight().Text(text =>
                    {
                        text.Span("Página ").FontSize(7);
                        text.CurrentPageNumber().FontSize(7);
                        text.Span(" de ").FontSize(7);
                        text.TotalPages().FontSize(7);
                    });
                });
        }
    }
}
