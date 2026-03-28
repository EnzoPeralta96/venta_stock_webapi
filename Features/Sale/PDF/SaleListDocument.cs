using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using proyecto_venta_stock.Models;

namespace venta_stock_webapi.Sale.PDF
{
    // ───── Data source ─────────────────────────────────────────────────────────
    public class SaleListDataSource
    {
        public Ferreteria DatosFerreteria { get; set; } = new();
        public string Titulo { get; set; } = "LISTADO DE VENTAS";
        public DateTime FechaGeneracion { get; set; } = DateTime.Now;
        public string? FiltroCliente { get; set; }
        public string? FiltroFechaDesde { get; set; }
        public string? FiltroFechaHasta { get; set; }
        public string? FiltroEstado { get; set; }
        public int TotalVentas { get; set; }
        public decimal TotalRecaudado { get; set; }
        public List<SaleListItemPdf> Ventas { get; set; } = new();
    }

    public class SaleListItemPdf
    {
        public int Numero { get; set; }
        public string CodigoVenta { get; set; } = "";
        public string Cliente { get; set; } = "";
        public string Fecha { get; set; } = "";
        public string MedioPago { get; set; } = "";
        public string Estado { get; set; } = "";
        public string Vendedor { get; set; } = "";
        public decimal Total { get; set; }
    }

    // ───── Document ────────────────────────────────────────────────────────────
    public class SaleListDocument : IDocument
    {
        private readonly SaleListDataSource _data;

        public SaleListDocument(SaleListDataSource data) => _data = data;

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(30);
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

                    // Título y filtros
                    row.ConstantItem(270).Column(c =>
                    {
                        c.Item().AlignRight().Text(_data.Titulo)
                            .FontSize(14).Bold().FontColor(Colors.Blue.Darken2);
                        c.Item().AlignRight()
                            .Text($"Generado: {_data.FechaGeneracion:dd/MM/yyyy HH:mm}")
                            .FontSize(8).FontColor(Colors.Grey.Darken1);

                        if (!string.IsNullOrWhiteSpace(_data.FiltroFechaDesde) || !string.IsNullOrWhiteSpace(_data.FiltroFechaHasta))
                        {
                            var rango = $"Período: {_data.FiltroFechaDesde ?? "–"} al {_data.FiltroFechaHasta ?? "–"}";
                            c.Item().AlignRight().Text(rango).FontSize(8).FontColor(Colors.Grey.Darken1);
                        }
                        if (!string.IsNullOrWhiteSpace(_data.FiltroCliente))
                            c.Item().AlignRight().Text($"Cliente: {_data.FiltroCliente}").FontSize(8).FontColor(Colors.Grey.Darken1);
                        if (!string.IsNullOrWhiteSpace(_data.FiltroEstado))
                            c.Item().AlignRight().Text($"Estado: {_data.FiltroEstado}").FontSize(8).FontColor(Colors.Grey.Darken1);
                    });
                });

                col.Item().PaddingTop(6).BorderBottom(2).BorderColor(Colors.Blue.Darken2);

                // KPIs
                col.Item().PaddingTop(6).Row(row =>
                {
                    KpiCard(row.RelativeItem(), "Total Ventas", _data.TotalVentas.ToString(), Colors.Blue.Lighten4);
                    row.ConstantItem(10);
                    KpiCard(row.RelativeItem(), "Total Recaudado", $"${_data.TotalRecaudado:N2}", Colors.Green.Lighten4);
                });

                col.Item().PaddingTop(6);
            });
        }

        static void KpiCard(IContainer container, string label, string value, string bgColor)
        {
            container.Background(bgColor).Padding(8).Column(c =>
            {
                c.Item().Text(label).FontSize(8).FontColor(Colors.Grey.Darken2);
                c.Item().Text(value).FontSize(13).Bold();
            });
        }

        void ComposeContent(IContainer container)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.ConstantColumn(25);   // #
                    cols.RelativeColumn(2);    // Código
                    cols.RelativeColumn(3);    // Cliente
                    cols.ConstantColumn(70);   // Fecha
                    cols.RelativeColumn(2);    // Vendedor
                    cols.RelativeColumn(1.5f); // Medio Pago
                    cols.ConstantColumn(60);   // Estado
                    cols.ConstantColumn(80);   // Total
                });

                // Header
                table.Header(header =>
                {
                    IContainer H(IContainer c) => c
                        .Background(Colors.Blue.Darken2).Padding(5)
                        .DefaultTextStyle(x => x.FontColor(Colors.White).Bold());

                    header.Cell().Element(H).Text("#");
                    header.Cell().Element(H).Text("Código");
                    header.Cell().Element(H).Text("Cliente");
                    header.Cell().Element(H).Text("Fecha");
                    header.Cell().Element(H).Text("Vendedor");
                    header.Cell().Element(H).Text("Medio Pago");
                    header.Cell().Element(H).Text("Estado");
                    header.Cell().Element(H).AlignRight().Text("Total");
                });

                // Rows
                foreach (var v in _data.Ventas)
                {
                    bool even = v.Numero % 2 == 0;

                    IContainer R(IContainer c) => c
                        .Background(even ? Colors.Grey.Lighten4 : Colors.White)
                        .BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(4);

                    table.Cell().Element(R).Text(v.Numero.ToString()).FontColor(Colors.Grey.Darken1);
                    table.Cell().Element(R).Text(v.CodigoVenta);
                    table.Cell().Element(R).Text(v.Cliente);
                    table.Cell().Element(R).Text(v.Fecha);
                    table.Cell().Element(R).Text(v.Vendedor);
                    table.Cell().Element(R).Text(v.MedioPago);
                    table.Cell().Element(R).Element(c => EstadoBadge(c, v.Estado));
                    table.Cell().Element(R).AlignRight().Text($"${v.Total:N2}").Bold();
                }

                // Total row
                IContainer Tot(IContainer c) => c
                    .Background(Colors.Blue.Lighten4).Padding(5);

                table.Cell().ColumnSpan(7).Element(Tot).AlignRight().Text("TOTAL:").Bold();
                table.Cell().Element(Tot).AlignRight()
                    .Text($"${_data.TotalRecaudado:N2}").Bold().FontColor(Colors.Blue.Darken2);
            });
        }

        static void EstadoBadge(IContainer container, string estado)
        {
            var color = estado.ToLower() switch
            {
                "aprobado" => Colors.Green.Lighten3,
                "anulada"  => Colors.Red.Lighten3,
                "pendiente" => Colors.Yellow.Lighten3,
                "rechazado" => Colors.Orange.Lighten3,
                _          => Colors.Grey.Lighten3
            };
            container.Background(color).Padding(3)
                .Text(estado).FontSize(8).Bold();
        }

        void ComposeFooter(IContainer container)
        {
            container.AlignCenter().BorderTop(1).BorderColor(Colors.Grey.Lighten2)
                .PaddingTop(5).Row(row =>
                {
                    row.RelativeItem().Text($"Documento generado el {_data.FechaGeneracion:dd/MM/yyyy HH:mm:ss}").FontSize(7).FontColor(Colors.Grey.Darken1);
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
