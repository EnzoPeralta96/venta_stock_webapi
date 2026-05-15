using proyecto_venta_stock.Models;
using proyecto_venta_stock.Proveedor.DTO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace proyecto_venta_stock.Proveedor.PDF
{
    public class ProveedorListDocument : IDocument
    {
        private readonly List<ProveedorDTO> _proveedores;
        private readonly Ferreteria _ferreteria;

        public ProveedorListDocument(List<ProveedorDTO> proveedores, Ferreteria ferreteria)
        {
            _proveedores = proveedores;
            _ferreteria = ferreteria;
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
            container.Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Column(column =>
                    {
                        column.Item().Text(_ferreteria.Nombre)
                            .FontSize(20).Bold().FontColor(Colors.Blue.Darken2);
                        column.Item().Text(_ferreteria.Direccion).FontSize(9);
                        column.Item().Text($"Tel: {_ferreteria.Telefono}").FontSize(9);
                        column.Item().Text($"Email: {_ferreteria.Email}").FontSize(9);
                        column.Item().Text($"CUIT: {_ferreteria.Cuit}").FontSize(9).Bold();
                    });

                    row.ConstantItem(200).Border(1).Padding(10).Column(column =>
                    {
                        column.Item().AlignCenter().Text("LISTADO DE PROVEEDORES")
                            .FontSize(11).Bold().FontColor(Colors.Blue.Darken2);
                        column.Item().AlignCenter().Text(DateTime.Now.ToString("dd/MM/yyyy"))
                            .FontSize(9);
                    });
                });

                col.Item().PaddingTop(10).BorderBottom(2).BorderColor(Colors.Blue.Darken2);
            });
        }

        void ComposeContent(IContainer container)
        {
            container.PaddingVertical(20).Column(column =>
            {
                // Encabezado de tabla
                column.Item().Background(Colors.Blue.Darken2).Padding(6).Row(row =>
                {
                    row.ConstantItem(30).Text("#").Bold().FontColor(Colors.White);
                    row.RelativeItem(3).Text("Nombre").Bold().FontColor(Colors.White);
                    row.RelativeItem(3).Text("Dirección").Bold().FontColor(Colors.White);
                    row.RelativeItem(2).Text("Teléfono").Bold().FontColor(Colors.White);
                    row.ConstantItem(55).AlignCenter().Text("Estado").Bold().FontColor(Colors.White);
                });

                // Filas
                for (int i = 0; i < _proveedores.Count; i++)
                {
                    var p = _proveedores[i];
                    var bgColor = i % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;

                    column.Item().Background(bgColor).Padding(6).Row(row =>
                    {
                        row.ConstantItem(30).Text((i + 1).ToString()).FontColor(Colors.Grey.Darken2);
                        row.RelativeItem(3).Text(p.Nombre ?? "-");
                        row.RelativeItem(3).Text(p.Direccion ?? "-").FontColor(Colors.Grey.Darken1);
                        row.RelativeItem(2).Text(p.Telefono ?? "-").FontColor(Colors.Grey.Darken1);
                        var estadoColor = p.Activo ? Colors.Green.Darken2 : Colors.Red.Darken2;
                        row.ConstantItem(55).AlignCenter().Text(p.Activo ? "Activo" : "Inactivo")
                            .FontColor(estadoColor).Bold();
                    });
                }

                column.Item().PaddingTop(10).AlignRight()
                    .Text($"Total: {_proveedores.Count} proveedor(es)")
                    .FontSize(9).Italic().FontColor(Colors.Grey.Darken1);
            });
        }

        void ComposeFooter(IContainer container)
        {
            container.AlignCenter().Column(column =>
            {
                column.Item().BorderTop(1).BorderColor(Colors.Grey.Medium)
                    .PaddingTop(10)
                    .Text(text =>
                    {
                        text.Span("Documento generado el: ");
                        text.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")).FontSize(8);
                    });
            });
        }
    }
}
