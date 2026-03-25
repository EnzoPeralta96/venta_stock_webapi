using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace proyecto_venta_stock.CompraProveedor.PDF;

public class CompraProveedorReportDocument : IDocument
{
    private static readonly CultureInfo ReportCulture = CultureInfo.GetCultureInfo("es-AR");

    private readonly IReadOnlyList<Models.CompraProveedor> _compras;
    private readonly string _filtroTexto;
    private readonly DateTime _generatedAt;
    private readonly decimal _totalGeneral;
    private readonly int _comprasActivas;
    private readonly int _comprasAnuladas;

    public CompraProveedorReportDocument(
        IReadOnlyList<Models.CompraProveedor> compras,
        string filtroTexto,
        DateTime generatedAt)
    {
        _compras = compras;
        _filtroTexto = filtroTexto;
        _generatedAt = generatedAt;
        _totalGeneral = compras.Sum(c => c.Total);
        _comprasActivas = compras.Count(c => c.Activo);
        _comprasAnuladas = compras.Count - _comprasActivas;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(24);
            page.DefaultTextStyle(x => x.FontSize(9.5f));

            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeContent);
            page.Footer().Element(ComposeFooter);
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(left =>
                {
                    left.Item().Text("Reporte de compras a proveedores")
                        .FontSize(18)
                        .Bold()
                        .FontColor(Colors.Blue.Darken2);

                    left.Item().PaddingTop(2).Text("Resumen general y detalle legible por compra")
                        .FontSize(9)
                        .FontColor(Colors.Grey.Darken2);
                });

                row.ConstantItem(280).Background(Colors.Blue.Lighten4)
                    .Border(1).BorderColor(Colors.Blue.Lighten3)
                    .Padding(10)
                    .Column(right =>
                    {
                        right.Item().Text("Filtros aplicados")
                            .FontSize(8)
                            .Bold()
                            .FontColor(Colors.Blue.Darken2);

                        right.Item().PaddingTop(2).Text(_filtroTexto)
                            .FontSize(9)
                            .Bold();

                        right.Item().PaddingTop(4).Text($"Emitido: {_generatedAt:dd/MM/yyyy HH:mm}")
                            .FontSize(8)
                            .FontColor(Colors.Grey.Darken2);
                    });
            });

            column.Item().PaddingTop(10).Row(row =>
            {
                row.Spacing(8);

                row.RelativeItem().Element(c =>
                    ComposeKpiCard(c, "Compras", _compras.Count.ToString(), Colors.Blue.Darken2, Colors.Blue.Lighten4));

                row.RelativeItem().Element(c =>
                    ComposeKpiCard(c, "Activas", _comprasActivas.ToString(), Colors.Green.Darken2, Colors.Grey.Lighten3));

                row.RelativeItem().Element(c =>
                    ComposeKpiCard(c, "Anuladas", _comprasAnuladas.ToString(), Colors.Red.Darken2, Colors.Red.Lighten4));

                row.RelativeItem().Element(c =>
                    ComposeKpiCard(c, "Total general", FormatCurrency(_totalGeneral), Colors.Blue.Darken2, Colors.Grey.Lighten3));
            });

            column.Item().PaddingTop(12).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingTop(12).Column(column =>
        {
            if (_compras.Count == 0)
            {
                column.Item().Border(1).BorderColor(Colors.Grey.Lighten2)
                    .Background(Colors.Grey.Lighten4)
                    .Padding(24)
                    .AlignCenter()
                    .Column(empty =>
                    {
                        empty.Item().Text("No hay compras para exportar")
                            .FontSize(14)
                            .Bold()
                            .FontColor(Colors.Blue.Darken2);

                        empty.Item().PaddingTop(6).Text("Ajusta los filtros o amplia el periodo del reporte.")
                            .FontSize(9)
                            .FontColor(Colors.Grey.Darken2);
                    });
                return;
            }

            column.Item().Text("Resumen por compra")
                .FontSize(11)
                .Bold()
                .FontColor(Colors.Blue.Darken2);

            column.Item().PaddingTop(6).Element(ComposeOverviewTable);

            column.Item().PageBreak();

            column.Item().Text("Detalle de compras")
                .FontSize(11)
                .Bold()
                .FontColor(Colors.Blue.Darken2);

            for (var index = 0; index < _compras.Count; index++)
            {
                if (index > 0)
                    column.Item().PageBreak();

                column.Item()
                    .PaddingTop(index == 0 ? 10 : 0)
                    .Element(c => ComposePurchaseCard(c, _compras[index]));
            }
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Text("Para auditoria detallada por columna conviene exportar Excel; este PDF prioriza lectura humana.")
                .FontSize(8)
                .FontColor(Colors.Grey.Darken1);

            row.ConstantItem(120).AlignRight().Text(text =>
            {
                text.Span("Pagina ").FontSize(8).FontColor(Colors.Grey.Medium);
                text.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Medium);
                text.Span(" de ").FontSize(8).FontColor(Colors.Grey.Medium);
                text.TotalPages().FontSize(8).FontColor(Colors.Grey.Medium);
            });
        });
    }

    private void ComposeOverviewTable(IContainer container)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(60);
                columns.ConstantColumn(80);
                columns.RelativeColumn(3);
                columns.RelativeColumn(2);
                columns.ConstantColumn(55);
                columns.ConstantColumn(100);
                columns.ConstantColumn(75);
            });

            table.Header(header =>
            {
                header.Cell().Element(HeaderCellStyle).Text("Compra");
                header.Cell().Element(HeaderCellStyle).Text("Fecha");
                header.Cell().Element(HeaderCellStyle).Text("Proveedor");
                header.Cell().Element(HeaderCellStyle).Text("Comprobante");
                header.Cell().Element(HeaderCellStyle).AlignCenter().Text("Items");
                header.Cell().Element(HeaderCellStyle).AlignRight().Text("Total");
                header.Cell().Element(HeaderCellStyle).AlignCenter().Text("Estado");
            });

            var rowIndex = 0;

            foreach (var compra in _compras)
            {
                rowIndex++;
                var background = rowIndex % 2 == 0 ? Colors.Grey.Lighten4 : Colors.White;
                var statusColor = compra.Activo ? Colors.Green.Darken2 : Colors.Red.Darken2;

                table.Cell().Element(c => BodyCellStyle(c, background)).Text($"#{compra.IdCompraProveedor}");
                table.Cell().Element(c => BodyCellStyle(c, background)).Text(compra.Fecha.ToString("dd/MM/yyyy"));
                table.Cell().Element(c => BodyCellStyle(c, background)).Text(GetProveedor(compra));
                table.Cell().Element(c => BodyCellStyle(c, background)).Text(GetComprobante(compra));
                table.Cell().Element(c => BodyCellStyle(c, background)).AlignCenter().Text(compra.CompraProveedorDetalles.Count.ToString());
                table.Cell().Element(c => BodyCellStyle(c, background)).AlignRight().Text(FormatCurrency(compra.Total));
                table.Cell().Element(c => BodyCellStyle(c, background)).AlignCenter().Text(text =>
                {
                    text.Span(compra.Activo ? "Activa" : "Anulada")
                        .FontColor(statusColor)
                        .Bold();
                });
            }
        });
    }

    private void ComposePurchaseCard(IContainer container, Models.CompraProveedor compra)
    {
        var accentColor = compra.Activo ? Colors.Blue.Darken2 : Colors.Red.Darken2;
        var accentBackground = compra.Activo ? Colors.Blue.Lighten4 : Colors.Red.Lighten4;

        container.Decoration(decoration =>
        {
            decoration.Before().Column(column =>
            {
                column.Item().ShowOnce().Element(c => ComposePurchaseHeader(c, compra, accentColor, accentBackground, false));
                column.Item().SkipOnce().Element(c => ComposePurchaseHeader(c, compra, accentColor, accentBackground, true));
            });

            decoration.Content().Border(1).BorderColor(Colors.Grey.Lighten2)
                .Padding(12)
                .Column(column =>
                {
                    column.Item().Row(row =>
                    {
                        row.Spacing(8);
                        row.RelativeItem().Element(c => ComposeInfoCard(c, "Fecha", compra.Fecha.ToString("dd/MM/yyyy")));
                        row.RelativeItem().Element(c => ComposeInfoCard(c, "Vencimiento", compra.FechaVencimiento?.ToString("dd/MM/yyyy") ?? "-"));
                        row.RelativeItem().Element(c => ComposeInfoCard(c, "Comprobante", GetComprobante(compra)));
                        row.RelativeItem().Element(c => ComposeInfoCard(c, "Registro", GetUsuario(compra)));
                    });

                    if (!string.IsNullOrWhiteSpace(compra.Observacion))
                    {
                        column.Item().PaddingTop(8).Background(Colors.Grey.Lighten4)
                            .Padding(8)
                            .Text(text =>
                            {
                                text.Span("Observacion: ").Bold();
                                text.Span(compra.Observacion);
                            });
                    }

                    column.Item().PaddingTop(10).Row(row =>
                    {
                        row.Spacing(8);
                        row.RelativeItem().Element(c => ComposeMetricCard(c, "Productos", compra.CompraProveedorDetalles.Count.ToString(), Colors.Grey.Lighten4, Colors.Blue.Darken2));
                        row.RelativeItem().Element(c => ComposeMetricCard(c, "Subtotal", FormatCurrency(compra.Subtotal), Colors.Grey.Lighten4, Colors.Grey.Darken2));
                        row.RelativeItem().Element(c => ComposeMetricCard(c, "Descuento", FormatCurrency(compra.DescuentoTotal), Colors.Grey.Lighten4, Colors.Grey.Darken2));
                        row.RelativeItem().Element(c => ComposeMetricCard(c, "IVA", FormatCurrency(compra.IvaTotal), Colors.Grey.Lighten4, Colors.Grey.Darken2));
                    });

                    column.Item().PaddingTop(10).Element(c => ComposeDetailTable(c, compra, accentColor));
                });
        });
    }

    private void ComposePurchaseHeader(
        IContainer container,
        Models.CompraProveedor compra,
        string accentColor,
        string accentBackground,
        bool isContinuation)
    {
        container.Border(1).BorderColor(Colors.Grey.Lighten2)
            .Padding(12)
            .Column(column =>
            {
                column.Item().Row(row =>
                {
                    row.RelativeItem().Column(left =>
                    {
                        left.Item().Text($"Compra #{compra.IdCompraProveedor}")
                            .FontSize(isContinuation ? 11 : 13)
                            .Bold()
                            .FontColor(accentColor);

                        left.Item().PaddingTop(2).Text(GetProveedor(compra))
                            .FontSize(isContinuation ? 10 : 11)
                            .Bold();

                        left.Item().PaddingTop(2).Text(
                                isContinuation
                                    ? $"Continuacion del detalle | Tipo: {GetTipoComprobante(compra)}"
                                    : $"Tipo: {GetTipoComprobante(compra)}")
                            .FontSize(9)
                            .FontColor(Colors.Grey.Darken2);
                    });

                    row.ConstantItem(isContinuation ? 180 : 140).Background(accentBackground)
                        .Border(1).BorderColor(accentColor)
                        .Padding(8)
                        .Column(total =>
                        {
                            total.Item().AlignRight().Text(compra.Activo ? "Activa" : "Anulada")
                                .FontSize(8)
                                .Bold()
                                .FontColor(accentColor);

                            total.Item().PaddingTop(4).AlignRight().Text(
                                    isContinuation ? "Total / hoja continuada" : "Total compra")
                                .FontSize(8)
                                .FontColor(Colors.Grey.Darken2);

                            total.Item().AlignRight().Text(FormatCurrency(compra.Total))
                                .FontSize(isContinuation ? 12 : 14)
                                .Bold()
                                .FontColor(accentColor);
                        });
                });
            });
    }

    private void ComposeDetailTable(IContainer container, Models.CompraProveedor compra, string accentColor)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(4);
                columns.ConstantColumn(80);
                columns.ConstantColumn(110);
                columns.ConstantColumn(130);
                columns.ConstantColumn(110);
            });

            table.Header(header =>
            {
                header.Cell().Element(c => TableHeaderCell(c, accentColor)).Text("Producto");
                header.Cell().Element(c => TableHeaderCell(c, accentColor)).AlignRight().Text("Cantidad");
                header.Cell().Element(c => TableHeaderCell(c, accentColor)).AlignRight().Text("Precio c/u");
                header.Cell().Element(c => TableHeaderCell(c, accentColor)).Text("Desc. / IVA");
                header.Cell().Element(c => TableHeaderCell(c, accentColor)).AlignRight().Text("Total linea");
            });

            var rowIndex = 0;

            foreach (var detalle in compra.CompraProveedorDetalles)
            {
                rowIndex++;
                var background = rowIndex % 2 == 0 ? Colors.Grey.Lighten4 : Colors.White;
                var unidad = detalle.IdProductoNavigation?.IdUnidadMedidaNavigation?.Abreviatura ?? "";

                table.Cell().Element(c => TableBodyCell(c, background)).Text(detalle.IdProductoNavigation?.Nombre ?? "-");

                table.Cell().Element(c => TableBodyCell(c, background)).AlignRight().Text(text =>
                {
                    text.Span(FormatNumber(detalle.Cantidad)).Bold();
                    if (!string.IsNullOrEmpty(unidad))
                        text.Span($" {unidad}").FontSize(8).FontColor(Colors.Grey.Darken1);
                });

                table.Cell().Element(c => TableBodyCell(c, background)).AlignRight().Text(text =>
                {
                    text.Span(FormatCurrency(detalle.PrecioUnitario));
                    if (!string.IsNullOrEmpty(unidad))
                        text.Span($"/{unidad}").FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                });

                table.Cell().Element(c => TableBodyCell(c, background)).Text(FormatAdjustments(detalle));
                table.Cell().Element(c => TableBodyCell(c, background)).AlignRight().Text(FormatCurrency(detalle.Total));
            }
        });
    }

    private static void ComposeKpiCard(IContainer container, string title, string value, string valueColor, string backgroundColor)
    {
        container.Background(backgroundColor)
            .Border(1).BorderColor(Colors.Grey.Lighten2)
            .Padding(10)
            .Column(column =>
            {
                column.Item().Text(title)
                    .FontSize(8)
                    .FontColor(Colors.Grey.Darken1);

                column.Item().PaddingTop(2).Text(value)
                    .FontSize(14)
                    .Bold()
                    .FontColor(valueColor);
            });
    }

    private static void ComposeInfoCard(IContainer container, string title, string value)
    {
        container.Background(Colors.Grey.Lighten4)
            .Border(1).BorderColor(Colors.Grey.Lighten2)
            .Padding(8)
            .Column(column =>
            {
                column.Item().Text(title)
                    .FontSize(8)
                    .FontColor(Colors.Grey.Darken1);

                column.Item().PaddingTop(2).Text(string.IsNullOrWhiteSpace(value) ? "-" : value)
                    .FontSize(9)
                    .Bold();
            });
    }

    private static void ComposeMetricCard(IContainer container, string title, string value, string backgroundColor, string valueColor)
    {
        container.Background(backgroundColor)
            .Border(1).BorderColor(Colors.Grey.Lighten2)
            .Padding(8)
            .Column(column =>
            {
                column.Item().Text(title)
                    .FontSize(8)
                    .FontColor(Colors.Grey.Darken1);

                column.Item().PaddingTop(2).Text(value)
                    .FontSize(10)
                    .Bold()
                    .FontColor(valueColor);
            });
    }

    private static IContainer HeaderCellStyle(IContainer container)
    {
        return container.Background(Colors.Blue.Darken2)
            .PaddingVertical(5)
            .PaddingHorizontal(6)
            .DefaultTextStyle(x => x.FontColor(Colors.White).Bold().FontSize(8));
    }

    private static IContainer BodyCellStyle(IContainer container, string background)
    {
        return container.Background(background)
            .BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
            .PaddingVertical(5)
            .PaddingHorizontal(6);
    }

    private static IContainer TableHeaderCell(IContainer container, string accentColor)
    {
        return container.Background(accentColor)
            .PaddingVertical(5)
            .PaddingHorizontal(6)
            .DefaultTextStyle(x => x.FontColor(Colors.White).Bold().FontSize(8));
    }

    private static IContainer TableBodyCell(IContainer container, string background)
    {
        return container.Background(background)
            .BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
            .PaddingVertical(5)
            .PaddingHorizontal(6);
    }

    private static string GetProveedor(Models.CompraProveedor compra) =>
        string.IsNullOrWhiteSpace(compra.IdProveedorNavigation?.Proveedor1)
            ? "Proveedor no informado"
            : compra.IdProveedorNavigation.Proveedor1;

    private static string GetTipoComprobante(Models.CompraProveedor compra) =>
        string.IsNullOrWhiteSpace(compra.TipoComprobante)
            ? "No informado"
            : compra.TipoComprobante;

    private static string GetComprobante(Models.CompraProveedor compra)
    {
        var tipo = string.IsNullOrWhiteSpace(compra.TipoComprobante) ? null : compra.TipoComprobante.Trim();
        var numero = string.IsNullOrWhiteSpace(compra.NumeroComprobante) ? null : compra.NumeroComprobante.Trim();

        return (tipo, numero) switch
        {
            (null, null) => "-",
            (not null, null) => tipo,
            (null, not null) => numero,
            _ => $"{tipo} {numero}"
        };
    }

    private static string GetUsuario(Models.CompraProveedor compra)
    {
        var nombre = compra.IdUsuarioNavigation?.Nombre?.Trim();
        var apellido = compra.IdUsuarioNavigation?.Apellido?.Trim();
        var nombreCompleto = $"{nombre} {apellido}".Trim();

        if (!string.IsNullOrWhiteSpace(nombreCompleto))
            return nombreCompleto;

        return string.IsNullOrWhiteSpace(compra.IdUsuarioNavigation?.Usuario1)
            ? "-"
            : compra.IdUsuarioNavigation.Usuario1;
    }

    private static string FormatAdjustments(Models.CompraProveedorDetalle detalle)
    {
        var parts = new List<string>();

        if (detalle.DescuentoPorcentaje > 0)
            parts.Add($"Desc. {FormatNumber(detalle.DescuentoPorcentaje)}%");

        if (detalle.IvaPorcentaje > 0)
            parts.Add($"IVA {FormatNumber(detalle.IvaPorcentaje)}%");

        return parts.Count == 0 ? "Sin ajustes" : string.Join(" | ", parts);
    }

    private static string FormatCurrency(decimal value) =>
        $"$ {value.ToString("#,##0.00", ReportCulture)}";

    private static string FormatNumber(decimal value) =>
        value.ToString("#,##0.##", ReportCulture);
}
