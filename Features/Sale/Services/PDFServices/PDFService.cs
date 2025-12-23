// Features/Sale/Services/PdfService.cs

using QuestPDF.Fluent;
using venta_stock_webapi.Sale.PDF;
using venta_stock_webapi.Sale.Repository;
using Microsoft.EntityFrameworkCore;
using proyecto_venta_stock.Data;
using proyecto_venta_stock.Features.Ferreteria.Repository;
using venta_stock_webapi.Shared.Utils;
using venta_stock_webapi.Client.Repository;

namespace venta_stock_webapi.Sale.Services
{
    public class PdfService : IPdfService
    {
        private readonly VentaStockContext _context;
        private readonly IFerreteriaRepository _ferreteriaRepository;
        private readonly ISaleRepository _saleRepositoy;
        private readonly IPendingSaleRepository _pendingSaleRepository;
        private readonly IClientRepository _clientRepository;
        private readonly ILogger<PdfService> _logger;

        public PdfService(
            VentaStockContext context,
            IFerreteriaRepository ferreteriaRepository,
            ISaleRepository saleRepository,
            IPendingSaleRepository pendingSaleRepository,
            IClientRepository clientRepository,
            ILogger<PdfService> logger)
        {
            _context = context;
            _ferreteriaRepository = ferreteriaRepository;
            _saleRepositoy = saleRepository;
            _pendingSaleRepository = pendingSaleRepository;
            _clientRepository = clientRepository;
            _logger = logger;

            // Configurar licencia de QuestPDF (Community - gratis)
            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        }

        public async Task<byte[]> GenerateSalePdfAsync(int idVenta)
        {
            try
            {
                _logger.LogInformation("Generando PDF para venta {id}", idVenta);

                /* Obtener datos de la ferreteria */

                var ferreteriaInfo = await _ferreteriaRepository.GetAsync();
                // Obtener venta con relaciones
                var venta = await _saleRepositoy.GetSaleByIdAsync(idVenta);

                if (venta == null)
                {
                    throw new Exception($"Venta {idVenta} no encontrada");
                }
                var nombreCliente = "N/A";
                if (!string.IsNullOrEmpty(venta.IdClienteNavigation.Nombre))
                {
                    nombreCliente = venta.IdClienteNavigation.Nombre + " " + venta.IdClienteNavigation.Apellido;
                } else
                {
                    if (!string.IsNullOrEmpty(venta.IdClienteNavigation.RazonSocial))
                    {
                         nombreCliente = venta.IdClienteNavigation.RazonSocial;
                    }
                   
                }
                

                // Mapear a data source
                var dataSource = new SaleDocumentDataSource
                {
                    DatosFerreteria = ferreteriaInfo,
                    CodigoVenta = venta.CodigoVenta,
                    Fecha = venta.Fecha ?? DateTime.Now,

                    ClienteNombre = nombreCliente,

                    ClienteDni = !string.IsNullOrWhiteSpace(venta.IdClienteNavigation.Dni)
                            ? venta.IdClienteNavigation.Dni
                            : (venta.IdClienteNavigation.Cuit ?? "N/A"),
                    ClienteTelefono = venta.IdClienteNavigation.Telefono ?? "-",


                    VendedorNombre = $"{venta.IdUsuarioNavigation?.Nombre} " +
                                    $"{venta.IdUsuarioNavigation?.Apellido}",

                    MedioPago = venta.IdMedioPagoNavigation?.MedioPago1 ?? "N/A",

                    Items = venta.DetalleVenta.Select((d, index) => new SaleItemPdf
                    {
                        Numero = index + 1,
                        Producto = d.IdProductoNavigation.Nombre ?? "N/A",
                        Marca = d.IdProductoNavigation.Marca,
                        Cantidad = d.Cantidad ?? 0,
                        PrecioUnitario = d.PrecioVenta ?? 0,
                        Subtotal = d.SubTotal ?? 0
                    }).ToList(),

                    Subtotal = venta.Total ?? 0,
                    Descuento = 0,
                    Total = venta.Total ?? 0,
                    TotalEnTexto = NumberToTextConverter.ConvertirATexto(venta.Total ?? 0),

                    EsVentaPendiente = false
                };

                // Generar PDF
                var document = new SaleDocument(dataSource);
                var pdfBytes = document.GeneratePdf();

                _logger.LogInformation(
                    "PDF generado exitosamente para venta {codigo}, tamaño: {size} bytes",
                    venta.CodigoVenta, pdfBytes.Length
                );

                return pdfBytes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando PDF para venta {id}", idVenta);
                throw;
            }
        }

        public async Task<byte[]> GeneratePendingSalePdfAsync(int idVentaPendiente)
        {
            try
            {
                _logger.LogInformation(
                    "Generando PDF para venta pendiente {id}",
                    idVentaPendiente
                );

                 /* Obtener datos de la ferreteria */

                var ferreteriaInfo = await _ferreteriaRepository.GetAsync();

                // Obtener venta pendiente con relaciones
                var ventaPendiente = await _pendingSaleRepository.GetByIdAsync(idVentaPendiente);

                if (ventaPendiente == null)
                {
                    throw new Exception($"Venta pendiente {idVentaPendiente} no encontrada");
                }

                var nombreCliente = "N/A";
                if (!string.IsNullOrEmpty(ventaPendiente.IdClienteNavigation.Nombre))
                {
                    nombreCliente = ventaPendiente.IdClienteNavigation.Nombre + " " + ventaPendiente.IdClienteNavigation.Apellido;
                } else
                {
                    if (!string.IsNullOrEmpty(ventaPendiente.IdClienteNavigation.RazonSocial))
                    {
                         nombreCliente = ventaPendiente.IdClienteNavigation.RazonSocial;
                    }
                   
                }

                // Mapear a data source
                var dataSource = new SaleDocumentDataSource
                {
                    DatosFerreteria = ferreteriaInfo,

                    TipoComprobante = "VENTA PENDIENTE DE AUTORIZACIÓN",
                    CodigoVenta = ventaPendiente.CodigoVenta,
                    Fecha = DateTime.Now,

                    ClienteNombre = nombreCliente,
                    ClienteDni = !string.IsNullOrWhiteSpace(ventaPendiente.IdClienteNavigation.Dni)
                            ? ventaPendiente.IdClienteNavigation.Dni
                            : (ventaPendiente.IdClienteNavigation.Cuit ?? "N/A"),
                    ClienteTelefono = ventaPendiente.IdClienteNavigation.Telefono ?? "-",

                    VendedorNombre = $"{ventaPendiente.IdUsuarioVendedorNavigation.Nombre} " +
                                    $"{ventaPendiente.IdUsuarioVendedorNavigation.Apellido}",

                   MedioPago = ventaPendiente.IdMedioPagoNavigation?.MedioPago1 ?? "N/A",

                    Items = ventaPendiente.DetalleVentaPendientes
                        .Select((d, index) => new SaleItemPdf
                        {
                            Numero = index + 1,
                            Producto = d.IdProductoNavigation.Nombre ?? "N/A",
                            Marca = d.IdProductoNavigation.Marca,
                            Cantidad = d.Cantidad,
                            PrecioUnitario = d.PrecioVenta,
                            Subtotal = d.Subtotal
                        }).ToList(),

                    Subtotal = ventaPendiente.Total,
                    Total = ventaPendiente.Total,

                    EsVentaPendiente = true,
                    Excedente = ventaPendiente.Excedente,

                    Observaciones = "Esta venta quedó pendiente por exceder el límite de crédito. " +
                                   "Requiere autorización del administrador."
                };

                // Generar PDF
                var document = new SaleDocument(dataSource);
                var pdfBytes = document.GeneratePdf();

                _logger.LogInformation(
                    "PDF generado para venta pendiente {codigo}",
                    ventaPendiente.CodigoVenta
                );

                return pdfBytes;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error generando PDF para venta pendiente {id}",
                    idVentaPendiente
                );
                throw;
            }
        }
    }
}
