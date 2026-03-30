using System.Drawing;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using proyecto_venta_stock.ListaPrecio.DTO;
using proyecto_venta_stock.ListaPrecio.ListaPrecioRepository;
using proyecto_venta_stock.Message;
using proyecto_venta_stock.Models;
using proyecto_venta_stock.Shared.ResultPattern;

namespace proyecto_venta_stock.ListaPrecio.Services;

public class ListaPrecioItemServices : IListaPrecioItemServices
{
    private readonly IListaPrecioItemRepository _repo;
    private readonly IListaPrecioRepository _listaRepo;
    private readonly ILogger<ListaPrecioItemServices> _logger;

    public ListaPrecioItemServices(
        IListaPrecioItemRepository repo,
        IListaPrecioRepository listaRepo,
        ILogger<ListaPrecioItemServices> logger)
    {
        _repo = repo;
        _listaRepo = listaRepo;
        _logger = logger;
    }

    public async Task<Result<List<ListaPrecioItemDTO>>> GetItemsAsync(int idLista)
    {
        try
        {
            if (!await _repo.ListaExistsAsync(idLista))
                return Result<List<ListaPrecioItemDTO>>.Failure(ListaPrecioItemErrorCode.lista_not_found);

            var items = await _repo.GetItemsByListaAsync(idLista);

            var dtos = items.Select(x => new ListaPrecioItemDTO
            {
                IdLista = x.IdLista,
                IdProducto = x.IdProducto,
                Precio = x.Precio,
                Margen = x.Margen,
                NombreProducto = x.IdProductoNavigation?.Nombre,
                Marca = x.IdProductoNavigation?.Marca,
                IdUnidadMedida = x.IdProductoNavigation?.IdUnidadMedida
            }).ToList();

            return Result<List<ListaPrecioItemDTO>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error inesperado: " + ex);
            return Result<List<ListaPrecioItemDTO>>.Failure(ListaPrecioItemErrorCode.error_inesperado);
        }
    }

    public async Task<Result<bool>> AddItemAsync(int idLista, ListaPrecioItemUpsertDTO dto)
    {
        try
        {
            if (!await _repo.ListaExistsAsync(idLista))
                return Result<bool>.Failure(ListaPrecioItemErrorCode.lista_not_found);

            if (!await _repo.ProductoExistsAsync(dto.IdProducto))
                return Result<bool>.Failure(ListaPrecioItemErrorCode.producto_not_found);

            if (await _repo.ItemExistsAsync(idLista, dto.IdProducto))
                return Result<bool>.Failure(ListaPrecioItemErrorCode.item_already_exists);

            var entity = new ProductoListaprecioProveedor
            {
                IdLista = idLista,
                IdProducto = dto.IdProducto,
                Precio = dto.Precio,
                Margen = dto.Margen
            };

            await _repo.CreateAsync(entity);

            return Result<bool>.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError("Error inesperado: " + ex);
            return Result<bool>.Failure(ListaPrecioItemErrorCode.error_inesperado);
        }
    }

    public async Task<Result<bool>> UpdateItemAsync(int idLista, int idProducto, ListaPrecioItemUpsertDTO dto)
    {
        try
        {
            var item = await _repo.GetItemAsync(idLista, idProducto);
            if (item == null)
                return Result<bool>.Failure(ListaPrecioItemErrorCode.item_not_found);

            item.Precio = dto.Precio;
            item.Margen = dto.Margen;

            await _repo.UpdateAsync(item);
            return Result<bool>.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError("Error inesperado: " + ex);
            return Result<bool>.Failure(ListaPrecioItemErrorCode.error_inesperado);
        }
    }

    public async Task<Result<bool>> DeleteItemAsync(int idLista, int idProducto)
    {
        try
        {
            var item = await _repo.GetItemAsync(idLista, idProducto);
            if (item == null)
                return Result<bool>.Failure(ListaPrecioItemErrorCode.item_not_found);

            await _repo.DeleteAsync(item);
            return Result<bool>.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError("Error inesperado: " + ex);
            return Result<bool>.Failure(ListaPrecioItemErrorCode.error_inesperado);
        }
    }

    // ── Plantilla ─────────────────────────────────────────────────────────────

    public async Task<Result<byte[]>> DescargarPlantillaAsync(int idLista)
    {
        try
        {
            var lista = await _listaRepo.GetByIdAsync(idLista);
            if (lista == null)
                return Result<byte[]>.Failure(ListaPrecioItemErrorCode.lista_not_found);

            var items = await _repo.GetItemsWithBarcodesAsync(idLista);

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Lista de Precios");

            // Headers
            ws.Cells[1, 1].Value = "CodigoBarra";
            ws.Cells[1, 2].Value = "NombreProducto";
            ws.Cells[1, 3].Value = "NuevoCosto";
            ws.Cells[1, 4].Value = "MargenGanancia";

            using (var headerRange = ws.Cells[1, 1, 1, 4])
            {
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                headerRange.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(68, 114, 196));
                headerRange.Style.Font.Color.SetColor(Color.White);
            }

            // Data rows — pre-populated with existing items
            int row = 2;
            foreach (var item in items)
            {
                var codigoPrincipal = item.IdProductoNavigation?.CodigoBarras
                    .FirstOrDefault(c => c.Prinicial == true)?.Codigo
                    ?? item.IdProductoNavigation?.CodigoBarras.FirstOrDefault()?.Codigo
                    ?? string.Empty;

                ws.Cells[row, 1].Value = codigoPrincipal;
                ws.Cells[row, 2].Value = item.IdProductoNavigation?.Nombre ?? string.Empty;
                ws.Cells[row, 3].Value = item.Precio;
                ws.Cells[row, 4].Value = item.Margen.HasValue ? (object)item.Margen.Value : null;
                row++;
            }

            if (ws.Dimension != null)
                ws.Cells[ws.Dimension.Address].AutoFitColumns();

            return Result<byte[]>.Success(package.GetAsByteArray());
        }
        catch (Exception ex)
        {
            _logger.LogError("Error generando plantilla de lista {IdLista}: {Ex}", idLista, ex);
            return Result<byte[]>.Failure(ListaPrecioItemErrorCode.error_inesperado);
        }
    }

    // ── Importación ───────────────────────────────────────────────────────────

    public async Task<Result<ImportListaPrecioResultDTO>> ImportarAsync(int idLista, IFormFile file, bool actualizarPrecioVenta)
    {
        try
        {
            if (!await _repo.ListaExistsAsync(idLista))
                return Result<ImportListaPrecioResultDTO>.Failure(ListaPrecioItemErrorCode.lista_not_found);

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            List<ImportRow> rows;

            if (ext == ".csv")
                rows = await ParseCsvAsync(file);
            else if (ext is ".xlsx" or ".xls")
                rows = ParseExcel(file);
            else
                return Result<ImportListaPrecioResultDTO>.Failure(ListaPrecioItemErrorCode.formato_no_soportado);

            var resultado = new ImportListaPrecioResultDTO();

            await using var transaction = await _repo.BeginTransactionAsync();
            try
            {
                foreach (var row in rows)
                {
                    resultado.TotalProcesados++;

                    var idProducto = await _repo.GetProductoIdByCodigoBarraAsync(row.CodigoBarra);
                    if (idProducto == null)
                    {
                        resultado.CodigosIgnorados.Add(row.CodigoBarra);
                        continue;
                    }

                    bool existed = await _repo.ItemExistsAsync(idLista, idProducto.Value);
                    await _repo.UpsertItemNoSaveAsync(idLista, idProducto.Value, row.NuevoCosto, row.Margen);

                    if (existed) resultado.Actualizados++;
                    else resultado.Insertados++;

                    if (actualizarPrecioVenta && row.Margen.HasValue)
                    {
                        var precioVenta = row.NuevoCosto * (1 + row.Margen.Value / 100);
                        await _repo.UpdateProductoPrecioNoSaveAsync(idProducto.Value, precioVenta);
                    }
                }

                await _repo.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            return Result<ImportListaPrecioResultDTO>.Success(resultado);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error inesperado al importar lista {IdLista}: {Ex}", idLista, ex);
            return Result<ImportListaPrecioResultDTO>.Failure(ListaPrecioItemErrorCode.error_inesperado);
        }
    }

    // ── Parsers ───────────────────────────────────────────────────────────────
    // Columns: 1=CodigoBarra, 2=NombreProducto (ignored), 3=NuevoCosto, 4=MargenGanancia

    private record ImportRow(string CodigoBarra, decimal NuevoCosto, decimal? Margen);

    private static async Task<List<ImportRow>> ParseCsvAsync(IFormFile file)
    {
        var rows = new List<ImportRow>();
        using var reader = new StreamReader(file.OpenReadStream());

        await reader.ReadLineAsync(); // skip header

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line)) continue;

            // Support both comma and semicolon separators
            var cols = line.Contains(';') ? line.Split(';') : line.Split(',');
            if (cols.Length < 3) continue;

            var codigo = cols[0].Trim();
            if (string.IsNullOrWhiteSpace(codigo)) continue;

            // col[1] = NombreProducto — skip
            if (!decimal.TryParse(cols[2].Trim(), System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var costo)) continue;

            decimal? margen = null;
            if (cols.Length >= 4 && decimal.TryParse(cols[3].Trim(), System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var m))
                margen = m;

            rows.Add(new ImportRow(codigo, costo, margen));
        }

        return rows;
    }

    private static List<ImportRow> ParseExcel(IFormFile file)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        var rows = new List<ImportRow>();

        using var stream = file.OpenReadStream();
        using var package = new ExcelPackage(stream);

        var ws = package.Workbook.Worksheets[0];
        if (ws?.Dimension == null) return rows;

        for (int row = 2; row <= ws.Dimension.Rows; row++)
        {
            var codigo = ws.Cells[row, 1].Text.Trim();
            if (string.IsNullOrWhiteSpace(codigo)) continue;

            // col 2 = NombreProducto — skip
            var costoText = ws.Cells[row, 3].Text.Trim();
            if (!decimal.TryParse(costoText, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var costo)) continue;

            decimal? margen = null;
            var margenText = ws.Cells[row, 4].Text.Trim();
            if (!string.IsNullOrWhiteSpace(margenText) &&
                decimal.TryParse(margenText, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var m))
                margen = m;

            rows.Add(new ImportRow(codigo, costo, margen));
        }

        return rows;
    }
}
