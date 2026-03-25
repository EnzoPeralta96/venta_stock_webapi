using AutoMapper;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using proyecto_venta_stock.Features.Ferreteria.Repository;
using proyecto_venta_stock.Message;
using proyecto_venta_stock.Models;
using proyecto_venta_stock.Proveedor.DTO;
using proyecto_venta_stock.Proveedor.PDF;
using proyecto_venta_stock.Proveedor.ProveedorRepository;
using proyecto_venta_stock.Shared.ResultPattern;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using venta_stock_webapi.Shared.Paged;

namespace proyecto_venta_stock.Proveedor.Services
{
    public class ProveedorServices : IProveedorServices
    {
        private readonly ILogger<ProveedorServices> _logger;
        private readonly IProveedorRepository _proveedorRepository;
        private readonly IFerreteriaRepository _ferreteriaRepository;
        private readonly IMapper _mapper;

        public ProveedorServices(
            IProveedorRepository proveedorRepository,
            IFerreteriaRepository ferreteriaRepository,
            ILogger<ProveedorServices> logger,
            IMapper mapper)
        {
            _proveedorRepository = proveedorRepository;
            _ferreteriaRepository = ferreteriaRepository;
            _logger = logger;
            _mapper = mapper;
        }

        public async Task<Result<bool>> Create(CreateProveedorDTO dto)
        {
            try
            {
                var exists = await _proveedorRepository.Exists(dto.Nombre);

                if (exists) 
                    return Result<bool>.Failure(ProveedorErrorCode.proveedor_name_in_use);

                var entity = _mapper.Map<Models.Proveedor>(dto);

                await _proveedorRepository.Create(entity);

                return Result<bool>.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError("Error inesperado: " + ex);
                return Result<bool>.Failure(ProveedorErrorCode.error_inesperado);
            }
        }

        public async Task<Result<bool>> Update(UpdateProveedorDTO dto)
        {
            try
            {
                var existing = await _proveedorRepository.GetById(dto.IdProveedor);

                if (existing is null) 
                    return Result<bool>.Failure(ProveedorErrorCode.proveedor_not_found);

                var exists = await _proveedorRepository.Exists(dto.Nombre, excludeId: dto.IdProveedor);

                if (exists) 
                    return Result<bool>.Failure(ProveedorErrorCode.proveedor_name_in_use);

                // update manual (similar a ProductServices)
                existing.Proveedor1 = dto.Nombre;
                existing.Direccion = dto.Direccion;
                existing.Telefono = dto.Telefono;

                await _proveedorRepository.Update(existing);

                return Result<bool>.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError("Error inesperado: " + ex);
                return Result<bool>.Failure(ProveedorErrorCode.error_inesperado);
            }
        }

        public async Task<Result<List<ProveedorDTO>>> GetAll()
        {
            try
            {
                var list = await _proveedorRepository.GetAll();

                var dtos = _mapper.Map<List<ProveedorDTO>>(list);

                return Result<List<ProveedorDTO>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error inesperado: " + ex);
                return Result<List<ProveedorDTO>>.Failure(ProveedorErrorCode.error_inesperado);
            }
        }

        public async Task<Result<ProveedorDTO>> GetById(int idProveedor)
        {
            try
            {
                var entity = await _proveedorRepository.GetById(idProveedor);

                if (entity is null) 
                    return Result<ProveedorDTO>.Failure(ProveedorErrorCode.proveedor_not_found);

                var dto = _mapper.Map<ProveedorDTO>(entity);

                return Result<ProveedorDTO>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error inesperado: " + ex);
                return Result<ProveedorDTO>.Failure(ProveedorErrorCode.error_inesperado);
            }
        }

        public async Task<Result<bool>> Delete(int idProveedor)
        {
            try
            {
                var existing = await _proveedorRepository.GetById(idProveedor);

                if (existing is null) 
                    return Result<bool>.Failure(ProveedorErrorCode.proveedor_not_found);

                await _proveedorRepository.Delete(existing);

                return Result<bool>.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError("Error inesperado: " + ex);
                return Result<bool>.Failure(ProveedorErrorCode.error_inesperado);
            }
        }

        public async Task<Result<PagedList<ProveedorDTO>>> ProveedoresPagedAsync(int pageIndex, int pageSize, string searchTerm, string estado = "activos")
        {
            try
            {
                var query = _proveedorRepository.ProveedoresQueryable(searchTerm);

                if (estado.ToLower() == "activos")
                    query = query.Where(p => p.Activo);
                else if (estado.ToLower() == "eliminados")
                    query = query.Where(p => !p.Activo);

                var projected = _mapper.ProjectTo<ProveedorDTO>(query);
                var paged = await PagedList<ProveedorDTO>.CreateAsync(projected, pageIndex, pageSize);

                return Result<PagedList<ProveedorDTO>>.Success(paged);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error inesperado: " + ex);
                return Result<PagedList<ProveedorDTO>>.Failure(ProveedorErrorCode.error_inesperado);
            }
        }

        public async Task<Result<bool>> ToggleEstado(int idProveedor)
        {
            try
            {
                var existing = await _proveedorRepository.GetById(idProveedor);
                if (existing is null)
                    return Result<bool>.Failure(ProveedorErrorCode.proveedor_not_found);

                existing.Activo = !existing.Activo;
                if (existing.Activo) existing.FechaBaja = null;
                await _proveedorRepository.Update(existing);

                return Result<bool>.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError("Error inesperado: " + ex);
                return Result<bool>.Failure(ProveedorErrorCode.error_inesperado);
            }
        }

        public async Task<Result<byte[]>> ExportarExcelAsync()
        {
            try
            {
                var lista = await _proveedorRepository.GetAll();

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using var package = new ExcelPackage();
                var ws = package.Workbook.Worksheets.Add("Proveedores");

                // Header
                var headers = new[] { "#", "Nombre", "Dirección", "Teléfono", "Estado" };
                for (int i = 0; i < headers.Length; i++)
                {
                    ws.Cells[1, i + 1].Value = headers[i];
                    ws.Cells[1, i + 1].Style.Font.Bold = true;
                    ws.Cells[1, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    ws.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(68, 114, 196));
                    ws.Cells[1, i + 1].Style.Font.Color.SetColor(System.Drawing.Color.White);
                }

                // Data
                for (int i = 0; i < lista.Count; i++)
                {
                    var p = lista[i];
                    ws.Cells[i + 2, 1].Value = i + 1;
                    ws.Cells[i + 2, 2].Value = p.Proveedor1;
                    ws.Cells[i + 2, 3].Value = p.Direccion;
                    ws.Cells[i + 2, 4].Value = p.Telefono;
                    ws.Cells[i + 2, 5].Value = p.Activo ? "Sí" : "No";
                }

                ws.Cells[ws.Dimension.Address].AutoFitColumns();
                return Result<byte[]>.Success(package.GetAsByteArray());
            }
            catch (Exception ex)
            {
                _logger.LogError("Error inesperado al exportar proveedores: {Ex}", ex);
                return Result<byte[]>.Failure(ProveedorErrorCode.error_inesperado);
            }
        }

        public async Task<Result<byte[]>> ExportarPdfAsync()
        {
            try
            {
                var lista = await _proveedorRepository.GetAll();
                var dtos = _mapper.Map<List<ProveedorDTO>>(lista);
                var ferreteria = await _ferreteriaRepository.GetAsync();

                QuestPDF.Settings.License = LicenseType.Community;

                var document = new ProveedorListDocument(dtos, ferreteria);
                var bytes = document.GeneratePdf();

                return Result<byte[]>.Success(bytes);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error inesperado al exportar proveedores en PDF: {Ex}", ex);
                return Result<byte[]>.Failure(ProveedorErrorCode.error_inesperado);
            }
        }
    }
}