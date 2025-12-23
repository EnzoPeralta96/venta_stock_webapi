// Features/Sale/PDF/SaleDocument.cs

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace venta_stock_webapi.Sale.PDF
{
    public class SaleDocument : IDocument
    {
        private readonly SaleDocumentDataSource _data;

        public SaleDocument(SaleDocumentDataSource data)
        {
            _data = data;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container
                .Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Element(ComposeHeader);
                    page.Content().Element(ComposeContent);
                    page.Footer().Element(ComposeFooter);
                });
        }

        // ===== HEADER =====
        void ComposeHeader(IContainer container)
        {
            // Un contenedor solo admite un hijo; usamos Column para agrupar fila y separador
            container.Column(col =>
            {
                col.Item().Row(row =>
                {
                    // Logo y datos empresa (izquierda)
                    row.RelativeItem().Column(column =>
                    {
                        column.Item().Text(_data.DatosFerreteria.Nombre)
                            .FontSize(20)
                            .Bold()
                            .FontColor(Colors.Blue.Darken2);

                        column.Item().Text(_data.DatosFerreteria.Direccion)
                            .FontSize(9);

                        column.Item().Text($"Tel: {_data.DatosFerreteria.Telefono}")
                            .FontSize(9);

                        column.Item().Text($"Email: {_data.DatosFerreteria.Email}")
                            .FontSize(9);

                        column.Item().Text($"CUIT: {_data.DatosFerreteria.Cuit}")
                            .FontSize(9)
                            .Bold();
                    });

                    // Tipo de comprobante y número (derecha)
                    row.ConstantItem(200).Border(1).Padding(10).Column(column =>
                    {
                        column.Item().AlignCenter().Text(_data.TipoComprobante)
                            .FontSize(12)
                            .Bold();

                        column.Item().AlignCenter().Text($"Nº {_data.CodigoVenta}")
                            .FontSize(14)
                            .Bold()
                            .FontColor(Colors.Red.Darken1);

                        column.Item().AlignCenter().Text(_data.Fecha.ToString("dd/MM/yyyy HH:mm"))
                            .FontSize(9);
                    });
                });

                // Línea separadora
                col.Item().PaddingTop(10).BorderBottom(2).BorderColor(Colors.Blue.Darken2);
            });
        }

        // ===== CONTENIDO =====
        void ComposeContent(IContainer container)
        {
            container.PaddingVertical(20).Column(column =>
            {
                // Datos del cliente
                column.Item().Element(ComposeClientInfo);

                column.Item().PaddingVertical(10);

                // Tabla de items
                column.Item().Element(ComposeTable);

                column.Item().PaddingVertical(10);

                // Totales
                column.Item().Element(ComposeTotals);

                // Observaciones
                if (!string.IsNullOrWhiteSpace(_data.Observaciones))
                {
                    column.Item().PaddingTop(15).Element(ComposeObservations);
                }

                // Alerta si es venta pendiente
                if (_data.EsVentaPendiente)
                {
                    column.Item().PaddingTop(15).Element(ComposePendingAlert);
                }
            });
        }

        // Info del cliente
        void ComposeClientInfo(IContainer container)
        {
            container.Background(Colors.Grey.Lighten3)
                .Padding(10)
                .Column(column =>
                {
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("CLIENTE")
                                .FontSize(11)
                                .Bold();

                            col.Item().Text(_data.ClienteNombre)
                                .FontSize(10);
                            col.Item().Text($"DNI/CUIT: {_data.ClienteDni}")
                                .FontSize(9);
                            col.Item().Text($"TELÉFONO: {_data.ClienteTelefono}")
                                .FontSize(9);
                        });

                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("VENDEDOR")
                                .FontSize(11)
                                .Bold();

                            col.Item().Text(_data.VendedorNombre)
                                .FontSize(10);
                        });

                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("FORMA DE PAGO")
                                .FontSize(11)
                                .Bold();

                            col.Item().Text(_data.MedioPago)
                                .FontSize(10);
                        });
                    });
                });
        }

        // Tabla de productos
        void ComposeTable(IContainer container)
        {
            container.Table(table =>
            {
                // Definir columnas
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(30);     // #
                    columns.RelativeColumn(3);       // Producto
                    columns.RelativeColumn(2);       // Marca
                    columns.ConstantColumn(60);      // Cantidad
                    columns.ConstantColumn(80);      // P. Unitario
                    columns.ConstantColumn(100);     // Subtotal
                });

                // Header
                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).Text("#").Bold();
                    header.Cell().Element(CellStyle).Text("Producto").Bold();
                    header.Cell().Element(CellStyle).Text("Marca").Bold();
                    header.Cell().Element(CellStyle).AlignCenter().Text("Cant.").Bold();
                    header.Cell().Element(CellStyle).AlignRight().Text("P. Unit.").Bold();
                    header.Cell().Element(CellStyle).AlignRight().Text("Subtotal").Bold();

                    // Style para header
                    static IContainer CellStyle(IContainer container)
                    {
                        return container
                            .Background(Colors.Blue.Darken2)
                            .Padding(5)
                            .DefaultTextStyle(x => x.FontColor(Colors.White));
                    }
                });

                // Filas de items
                foreach (var item in _data.Items)
                {
                    var isEven = item.Numero % 2 == 0;

                    table.Cell().Element(e => RowStyle(e, isEven))
                        .AlignCenter()
                        .Text(item.Numero);

                    table.Cell().Element(e => RowStyle(e, isEven))
                        .Text(item.Producto);

                    table.Cell().Element(e => RowStyle(e, isEven))
                        .Text(item.Marca ?? "-");

                    table.Cell().Element(e => RowStyle(e, isEven))
                        .AlignCenter()
                        .Text(item.Cantidad);

                    table.Cell().Element(e => RowStyle(e, isEven))
                        .AlignRight()
                        .Text($"${item.PrecioUnitario:N2}");

                    table.Cell().Element(e => RowStyle(e, isEven))
                        .AlignRight()
                        .Text($"${item.Subtotal:N2}");
                }

                // Style para filas
                static IContainer RowStyle(IContainer container, bool isEven)
                {
                    return container
                        .Background(isEven ? Colors.Grey.Lighten4 : Colors.White)
                        .BorderBottom(1)
                        .BorderColor(Colors.Grey.Lighten2)
                        .Padding(5);
                }
            });
        }

        // Totales
        void ComposeTotals(IContainer container)
        {
            container.AlignRight().Column(column =>
            {
                column.Item().Row(row =>
                {
                    row.ConstantItem(150).Text("Subtotal:");
                    row.ConstantItem(100).AlignRight().Text($"${_data.Subtotal:N2}");
                });

                if (_data.Descuento > 0)
                {
                    column.Item().Row(row =>
                    {
                        row.ConstantItem(150).Text("Descuento:")
                            .FontColor(Colors.Red.Medium);
                        row.ConstantItem(100).AlignRight().Text($"-${_data.Descuento:N2}")
                            .FontColor(Colors.Red.Medium);
                    });
                }

                column.Item().PaddingTop(5).BorderTop(2).Row(row =>
                {
                    row.ConstantItem(150).Text("TOTAL:")
                        .FontSize(14)
                        .Bold();
                    row.ConstantItem(100).AlignRight().Text($"${_data.Total:N2}")
                        .FontSize(14)
                        .Bold()
                        .FontColor(Colors.Green.Darken2);
                });

                // Total en texto
                if (!string.IsNullOrWhiteSpace(_data.TotalEnTexto))
                {
                    column.Item().PaddingTop(5).Row(row =>
                    {
                        row.RelativeItem().AlignRight().Text(_data.TotalEnTexto)
                            .FontSize(9)
                            .Italic()
                            .FontColor(Colors.Grey.Darken1);
                    });
                }
            });
        }

        // Observaciones
        void ComposeObservations(IContainer container)
        {
            container.Border(1).BorderColor(Colors.Grey.Medium)
                .Padding(10)
                .Column(column =>
                {
                    column.Item().Text("Observaciones:")
                        .FontSize(10)
                        .Bold();

                    column.Item().Text(_data.Observaciones)
                        .FontSize(9);
                });
        }

        // Alerta de venta pendiente
        void ComposePendingAlert(IContainer container)
        {
            container.Background(Colors.Yellow.Lighten3)
                .Border(2)
                .BorderColor(Colors.Orange.Darken1)
                .Padding(10)
                .Column(column =>
                {
                    column.Item().Text("VENTA PENDIENTE DE AUTORIZACIÓN")
                        .FontSize(12)
                        .Bold()
                        .FontColor(Colors.Orange.Darken2);

                    column.Item().Text(
                        $"Esta venta excede el límite de crédito en ${_data.Excedente:N2}. " +
                        "Requiere aprobación del administrador antes de ser procesada."
                    ).FontSize(9);
                });
        }

        // ===== FOOTER =====
        void ComposeFooter(IContainer container)
        {
            container.AlignCenter().Column(column =>
            {
                column.Item().BorderTop(1).BorderColor(Colors.Grey.Medium)
                    .PaddingTop(10)
                    .Text("Gracias por su compra")
                    .FontSize(10)
                    .Italic();

                column.Item().Text(text =>
                {
                    text.Span("Documento generado el: ");
                    text.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"))
                        .FontSize(8);
                });
            });
        }
    }
}
