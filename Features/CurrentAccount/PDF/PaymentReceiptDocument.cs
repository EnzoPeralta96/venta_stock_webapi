using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace venta_stock_webapi.CurrentAccount.PDF
{
    public class PaymentReceiptDocument : IDocument
    {
        private readonly PaymentReceiptDataSource _data;

        public PaymentReceiptDocument(PaymentReceiptDataSource data)
        {
            _data = data;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                page.Footer().Element(ComposeFooter);
            });
        }

        void ComposeHeader(IContainer container)
        {
            var headerColor = _data.ColorHeader switch
            {
                "red"  => Colors.Red.Darken2,
                "blue" => Colors.Blue.Darken2,
                _      => Colors.Green.Darken2
            };

            container.Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Column(column =>
                    {
                        column.Item().Text(_data.DatosFerreteria.Nombre)
                            .FontSize(20).Bold().FontColor(Colors.Blue.Darken2);
                        column.Item().Text(_data.DatosFerreteria.Direccion).FontSize(9);
                        column.Item().Text($"Tel: {_data.DatosFerreteria.Telefono}").FontSize(9);
                        column.Item().Text($"Email: {_data.DatosFerreteria.Email}").FontSize(9);
                        column.Item().Text($"CUIT: {_data.DatosFerreteria.Cuit}").FontSize(9).Bold();
                    });

                    row.ConstantItem(200).Border(1).Padding(10).Column(column =>
                    {
                        column.Item().AlignCenter().Text(_data.TipoComprobante)
                            .FontSize(11).Bold().FontColor(headerColor);
                        column.Item().AlignCenter().Text("CUENTA CORRIENTE")
                            .FontSize(10).FontColor(Colors.Blue.Darken2);
                        column.Item().AlignCenter().Text(_data.Fecha.ToString("dd/MM/yyyy HH:mm"))
                            .FontSize(9);
                    });
                });

                col.Item().PaddingTop(10).BorderBottom(2).BorderColor(headerColor);
            });
        }

        void ComposeContent(IContainer container)
        {
            container.PaddingVertical(20).Column(column =>
            {
                // Datos del cliente
                column.Item().Background(Colors.Grey.Lighten3).Padding(10).Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("CLIENTE").FontSize(11).Bold();
                        col.Item().Text(_data.ClienteNombre).FontSize(10);
                        col.Item().Text($"DNI/CUIT: {_data.ClienteDni}").FontSize(9);
                        col.Item().Text($"Tel: {_data.ClienteTelefono}").FontSize(9);
                    });

                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("REGISTRADO POR").FontSize(11).Bold();
                        col.Item().Text(_data.UsuarioRegistra).FontSize(10);
                    });
                });

                column.Item().PaddingVertical(15);

                // Detalle del pago
                column.Item().Border(1).BorderColor(Colors.Grey.Medium).Padding(15).Column(col =>
                {
                    col.Item().Text("DETALLE DEL PAGO").FontSize(12).Bold();
                    col.Item().PaddingTop(8);

                    col.Item().Row(row =>
                    {
                        row.ConstantItem(160).Text("Tipo de comprobante:").Bold();
                        row.RelativeItem().Text(_data.TipoComprobante);
                    });

                    col.Item().PaddingTop(4).Row(row =>
                    {
                        row.ConstantItem(160).Text("Descripción:").Bold();
                        row.RelativeItem().Text(_data.Detalle);
                    });

                    // Motivo de nota de débito (solo para tipo ND)
                    if (!string.IsNullOrEmpty(_data.MotivoNd))
                    {
                        col.Item().PaddingTop(4).Row(row =>
                        {
                            row.ConstantItem(160).Text("Motivo:").Bold();
                            row.RelativeItem().Text(_data.MotivoNd);
                        });
                    }

                    // Motivo de nota de crédito (solo para tipo NC)
                    if (!string.IsNullOrEmpty(_data.MotivoNc))
                    {
                        col.Item().PaddingTop(4).Row(row =>
                        {
                            row.ConstantItem(160).Text("Motivo:").Bold();
                            row.RelativeItem().Text(_data.MotivoNc);
                        });
                    }

                    // Venta asociada (pago_factura)
                    if (!string.IsNullOrEmpty(_data.CodigoVenta))
                    {
                        col.Item().PaddingTop(4).Row(row =>
                        {
                            row.ConstantItem(160).Text("Venta asociada:").Bold();
                            row.RelativeItem().Text(_data.CodigoVenta).FontFamily(Fonts.Courier);
                        });

                        if (_data.TotalVenta.HasValue)
                        {
                            col.Item().PaddingTop(4).Row(row =>
                            {
                                row.ConstantItem(160).Text("Total de la venta:").Bold();
                                row.RelativeItem().Text($"${_data.TotalVenta:N2}");
                            });
                        }
                    }

                    col.Item().PaddingTop(15).BorderTop(1).BorderColor(Colors.Grey.Lighten2);

                    // Importe destacado
                    var labelImporte = _data.TipoComprobante switch
                    {
                        "ANULACIÓN DE PAGO" => "IMPORTE ANULADO:",
                        "NOTA DE DÉBITO"    => "IMPORTE DEBITADO:",
                        "NOTA DE CRÉDITO"   => "IMPORTE ACREDITADO:",
                        _                   => "IMPORTE PAGADO:"
                    };

                    col.Item().PaddingTop(10).AlignRight().Column(totales =>
                    {
                        totales.Item().Row(row =>
                        {
                            row.ConstantItem(160).Text(labelImporte)
                                .FontSize(13).Bold();
                            row.ConstantItem(120).AlignRight()
                                .Text($"${_data.Importe:N2}")
                                .FontSize(13).Bold().FontColor(Colors.Green.Darken2);
                        });

                        if (!string.IsNullOrWhiteSpace(_data.ImporteEnLetras))
                        {
                            totales.Item().PaddingTop(4).AlignRight()
                                .Text(_data.ImporteEnLetras)
                                .FontSize(9).Italic().FontColor(Colors.Grey.Darken2);
                        }

                        totales.Item().PaddingTop(6).Row(row =>
                        {
                            row.ConstantItem(150).Text("Saldo resultante:").FontSize(10);
                            var saldoColor = _data.SaldoResultante > 0
                                ? Colors.Red.Darken1
                                : Colors.Green.Darken2;
                            var saldoTexto = _data.SaldoResultante > 0
                                ? $"${_data.SaldoResultante:N2} (deuda)"
                                : _data.SaldoResultante < 0
                                    ? $"${Math.Abs(_data.SaldoResultante):N2} (a favor)"
                                    : "$0,00 (sin deuda)";
                            row.ConstantItem(120).AlignRight()
                                .Text(saldoTexto).FontSize(10).FontColor(saldoColor);
                        });
                    });
                });
            });
        }

        void ComposeFooter(IContainer container)
        {
            container.AlignCenter().Column(column =>
            {
                column.Item().BorderTop(1).BorderColor(Colors.Grey.Medium)
                    .PaddingTop(10)
                    .Text("Gracias por su pago")
                    .FontSize(10).Italic();

                column.Item().Text(text =>
                {
                    text.Span("Documento generado el: ");
                    text.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")).FontSize(8);
                });
            });
        }
    }
}
