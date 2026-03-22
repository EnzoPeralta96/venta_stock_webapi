# Prompt — Implementación: PDF de Nota de Crédito (Venta Anulada)

```
Contexto del proyecto:
  - ASP.NET Core 8, EF Core, PostgreSQL, QuestPDF para generación de PDFs.
  - Ya existe SaleDocument.cs (QuestPDF) que genera el comprobante de venta normal.
  - Ya existe SaleDocumentDataSource.cs con los datos para el PDF de venta.
  - Ya existe IPdfService / PdfService con el método GenerateSalePdfAsync(int idVenta).
  - El endpoint GET /api/Sale/{idVenta}/pdf ya genera el comprobante de venta normal.
  - El endpoint POST /api/Sale/{idVenta}/annul ya anula una venta y retorna:
    {
      "idVenta": 42,
      "codigoVenta": "VENTA-20250310-0012",
      "estado": "Anulada",
      "idMovimientoNc": 135  // null si fue al contado
    }
  - Para ventas CC, el comprobante NC es generado por payment-receipt/{idMovimientoNc}
    del módulo CurrentAccount. Ese PDF ya existe y funciona.
  - El problema: para ventas al contado no se genera ningún comprobante al anular.

  ANTES DE IMPLEMENTAR, leer:
  - Features/Sale/PDF/SaleDocument.cs       → patrón QuestPDF existente
  - Features/Sale/PDF/SaleDocumentDataSource.cs → dataSource existente
  - Features/Sale/Services/PDFServices/PdfService.cs → servicio PDF existente
  - Features/Sale/Repository/SaleRepository/SaleRepository.cs → métodos disponibles

  ══════════════════════════════════════════════════════════
  OBJETIVO
  ══════════════════════════════════════════════════════════

  Añadir un endpoint que genere el PDF de "Nota de Crédito" para una venta anulada.
  
  Este comprobante:
  - Aplica a CUALQUIER venta anulada (tanto CC como al contado).
  - Muestra los datos de la venta original con watermark/sello "ANULADA".
  - Indica el motivo de la anulación.
  - Es el mismo estilo visual que el comprobante de venta (SaleDocument) pero con
    color rojo y badge "NOTA DE CRÉDITO" en lugar de "COMPROBANTE DE VENTA".

  ══════════════════════════════════════════════════════════
  PARTE A — Nuevo DataSource
  ══════════════════════════════════════════════════════════

  Agregar a SaleDocumentDataSource.cs los campos necesarios para la NC:

     // Ya existentes en SaleDocumentDataSource (no duplicar):
     // CodigoVenta, Fecha, ClienteNombre, ClienteDni, ClienteTelefono,
     // VendedorNombre, MedioPago, Items, Total, TotalEnTexto,
     // Observaciones, TipoComprobante, DatosFerreteria

     // Nuevos campos a agregar:
     public bool EsNotaCredito { get; set; } = false;
     public string? MotivoNc { get; set; }        // ej. "Devolución de producto"
     public string? DetalleAdicional { get; set; }
     public DateTime? FechaAnulacion { get; set; }

  ══════════════════════════════════════════════════════════
  PARTE B — Modificar SaleDocument.cs para renderizar la NC
  ══════════════════════════════════════════════════════════

  Modificar SaleDocument.cs para que cuando _data.EsNotaCredito == true:

  1. En ComposeHeader():
     - Cambiar el color del borde inferior de Blue.Darken2 a Red.Darken2.
     - Cambiar el texto de TipoComprobante a "NOTA DE CRÉDITO" (ya viene en el DataSource).
     - El código de venta sigue mostrando el CodigoVenta original.
     - Agregar debajo del código de venta:
         "Fecha anulación: {FechaAnulacion:dd/MM/yyyy HH:mm}"
     - Cambiar el color del número de venta a Red.Darken2.

  2. En ComposeContent() — después de la tabla de items:
     - Si EsNotaCredito == true, agregar un bloque de "MOTIVO DE ANULACIÓN":

       container.Background(Colors.Red.Lighten4)
           .Border(1)
           .BorderColor(Colors.Red.Darken1)
           .Padding(10)
           .Column(col =>
           {
               col.Item().Text("MOTIVO DE ANULACIÓN")
                   .Bold()
                   .FontSize(11)
                   .FontColor(Colors.Red.Darken2);

               col.Item().Text(_data.MotivoNc ?? "No especificado")
                   .FontSize(10);

               if (!string.IsNullOrWhiteSpace(_data.DetalleAdicional))
               {
                   col.Item().PaddingTop(4)
                       .Text($"Detalle: {_data.DetalleAdicional}")
                       .FontSize(9)
                       .Italic();
               }
           });

  3. En ComposeTotals():
     - Si EsNotaCredito == true, mostrar el total en ROJO con texto "CRÉDITO:":
       row.ConstantItem(150).Text("CRÉDITO:").FontSize(14).Bold().FontColor(Colors.Red.Darken2);
       row.ConstantItem(100).AlignRight().Text($"${_data.Total:N2}").FontSize(14).Bold().FontColor(Colors.Red.Darken2);

  4. En ComposeFooter():
     - Si EsNotaCredito == true, cambiar "Gracias por su compra" por:
       "Este documento es constancia de la anulación de la venta indicada."

  ══════════════════════════════════════════════════════════
  PARTE C — Nuevo método en IPdfService / PdfService
  ══════════════════════════════════════════════════════════

  Agregar a IPdfService:

     Task<byte[]> GenerateAnnulReceiptAsync(int idVenta);

  Implementar en PdfService. Requiere:
  - ISaleRepository (ya inyectado) para obtener la venta con:
    .Include(v => v.IdClienteNavigation)
    .Include(v => v.IdMedioPagoNavigation)
    .Include(v => v.IdEstadoNavigation)
    .Include(v => v.IdUsuarioNavigation)
    .Include(v => v.DetalleVenta).ThenInclude(d => d.IdProductoNavigation)
  - IFerreRepository (o como se llame en el proyecto) para obtener los datos de la ferretería.
  - IAccountMovementRepository para obtener el motivo NC (si la venta es CC):
    Buscar el último movimiento de tipo NOTA_CREDITO (IdTipoMovimiento == 4)
    vinculado a la venta (IdVenta == idVenta).

  Implementación de GenerateAnnulReceiptAsync:

     public async Task<byte[]> GenerateAnnulReceiptAsync(int idVenta)
     {
         // 1. Obtener la venta con todas sus relaciones
         var venta = await _saleRepository.GetSaleByIdAsync(idVenta);
         if (venta is null)
             throw new Exception($"Venta {idVenta} no encontrada.");

         // 2. Verificar que la venta esté anulada (IdEstado == 5)
         //    Si no está anulada, lanzar excepción o retornar error apropiado.

         // 3. Obtener datos de la ferretería
         var ferreteria = await _ferreteriaRepository.GetFerreteria();

         // 4. Intentar obtener motivo NC del movimiento CC vinculado a la venta
         string? motivoNc = null;
         string? detalleAdicional = null;
         DateTime? fechaAnulacion = null;

         // Buscar en movimiento_cc el movimiento NC (tipo 4) vinculado a esta venta
         var movimientoNc = await _context.MovimientoCcs
             .Include(m => m.IdMotivoNcNavigation)
             .Where(m => m.IdVenta == idVenta && m.IdTipoMovimiento == 4)
             .OrderByDescending(m => m.Fecha)
             .FirstOrDefaultAsync();

         if (movimientoNc is not null)
         {
             motivoNc = movimientoNc.IdMotivoNcNavigation?.Nombre;
             // El detalle del movimiento tiene formato: "MotivoNombre — Detalle adicional — Venta CODIGO"
             // Extraer el detalle adicional si existe (entre primer y segundo " — ")
             var partes = movimientoNc.Detalle?.Split(" — ");
             if (partes?.Length >= 3)
                 detalleAdicional = string.Join(" — ", partes[1..^1]); // todo excepto primero y último
             fechaAnulacion = movimientoNc.Fecha;
         }

         // Si no hay movimiento NC (venta al contado), usar la fecha actual como referencia
         fechaAnulacion ??= DateTime.Now;

         // 5. Construir el DataSource
         var cliente = venta.IdClienteNavigation;
         var nombreCliente = !string.IsNullOrEmpty(cliente?.RazonSocial)
             ? cliente.RazonSocial
             : $"{cliente?.Nombre} {cliente?.Apellido}".Trim();

         var dataSource = new SaleDocumentDataSource
         {
             DatosFerreteria     = ferreteria,
             CodigoVenta         = venta.CodigoVenta,
             TipoComprobante     = "NOTA DE CRÉDITO",
             Fecha               = venta.Fecha ?? DateTime.MinValue,
             ClienteNombre       = nombreCliente,
             ClienteDni          = cliente?.Dni ?? cliente?.Cuit ?? "N/A",
             ClienteTelefono     = cliente?.Telefono ?? "N/A",
             VendedorNombre      = $"{venta.IdUsuarioNavigation?.Nombre} {venta.IdUsuarioNavigation?.Apellido}".Trim(),
             MedioPago           = venta.IdMedioPagoNavigation?.MedioPago1 ?? "N/A",
             Items               = venta.DetalleVenta.Select((d, i) => new SaleItemPdf
             {
                 Numero           = i + 1,
                 Producto         = d.IdProductoNavigation?.Nombre ?? "N/A",
                 Marca            = d.IdProductoNavigation?.Marca ?? "-",
                 Cantidad         = d.Cantidad ?? 0,
                 PrecioUnitario   = d.PrecioVenta ?? 0,
                 Subtotal         = d.SubTotal ?? 0
             }).ToList(),
             Subtotal            = venta.Total ?? 0,
             Total               = venta.Total ?? 0,
             TotalEnTexto        = ConvertirMontoATexto(venta.Total ?? 0), // helper existente
             EsVentaPendiente    = false,
             EsNotaCredito       = true,
             MotivoNc            = motivoNc,
             DetalleAdicional    = detalleAdicional,
             FechaAnulacion      = fechaAnulacion
         };

         // 6. Generar el PDF usando SaleDocument (mismo documento, datos distintos)
         var document = new SaleDocument(dataSource);
         return document.GeneratePdf();
     }

  ══════════════════════════════════════════════════════════
  PARTE D — Nuevo endpoint en SaleController
  ══════════════════════════════════════════════════════════

  Agregar en SaleController:

     /// <summary>
     /// Genera el PDF de Nota de Crédito para una venta anulada.
     /// Funciona para ventas al contado y para ventas CC.
     /// Para ventas CC también se puede usar el endpoint payment-receipt/{idMovimientoNc}.
     /// </summary>
     [Authorize(Policy = "PERM:VEN_READ")]
     [HttpGet("{idVenta:int}/credit-note-pdf")]
     public async Task<IActionResult> GetCreditNotePdf(int idVenta)
     {
         try
         {
             var pdfBytes = await _pdfService.GenerateAnnulReceiptAsync(idVenta);
             return File(
                 pdfBytes,
                 "application/pdf",
                 $"NotaCredito_{idVenta}_{DateTime.Now:yyyyMMdd}.pdf"
             );
         }
         catch (Exception ex)
         {
             _logger.LogError(ex, "Error generando PDF de nota de crédito para venta {idVenta}", idVenta);
             return StatusCode(500, new { message = "Error generando el PDF de nota de crédito." });
         }
     }

  ══════════════════════════════════════════════════════════
  RESUMEN DE ARCHIVOS A MODIFICAR
  ══════════════════════════════════════════════════════════

  ── Archivos a modificar ──
  Features/Sale/PDF/SaleDocumentDataSource.cs   → agregar 4 campos nuevos
  Features/Sale/PDF/SaleDocument.cs             → renderizado condicional para NC
  Features/Sale/Services/PDFServices/IPdfService.cs  → agregar GenerateAnnulReceiptAsync
  Features/Sale/Services/PDFServices/PdfService.cs   → implementar el método
  Features/Sale/Controllers/SaleController.cs   → agregar endpoint GET {id}/credit-note-pdf

  ══════════════════════════════════════════════════════════
  ACTUALIZACIÓN EN EL FRONTEND
  ══════════════════════════════════════════════════════════

  Una vez implementado el PDF, en la respuesta de POST /api/Sale/{idVenta}/annul
  el frontend ya tiene el `idVenta`. Puede usar indistintamente:

  - Para TODAS las ventas anuladas:
    GET /api/Sale/{idVenta}/credit-note-pdf  → PDF de NC (nuevo endpoint, funciona siempre)

  - Para ventas CC (si se tiene idMovimientoNc):
    GET /api/CurrentAccount/payment-receipt/{idMovimientoNc} → PDF del movimiento NC en CC

  Recomendación: usar siempre el nuevo endpoint /credit-note-pdf por consistencia.
  Es más completo ya que incluye el detalle de los productos anulados.

  ══════════════════════════════════════════════════════════
  ORDEN DE IMPLEMENTACIÓN
  ══════════════════════════════════════════════════════════

  1. Agregar campos a SaleDocumentDataSource (Parte A)
  2. Modificar SaleDocument para renderizar modo NC (Parte B)
  3. Agregar e implementar GenerateAnnulReceiptAsync en PdfService (Parte C)
  4. Agregar endpoint en SaleController (Parte D)
  5. Compilar y verificar que el proyecto arranca sin errores
  6. Probar: anular una venta y llamar a GET /api/Sale/{idVenta}/credit-note-pdf
```
