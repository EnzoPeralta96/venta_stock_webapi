using AutoMapper;
using proyecto_venta_stock.Shared.ResultPattern;
using proyecto_venta_stock.Features.Ferreteria.Repository;
using venta_stock_webapi.Features.Ferreteria.DTO;
using venta_stock_webapi.Features.Ferreteria.Message;

namespace venta_stock_webapi.Features.Ferreteria.Services
{
    public class FerreteriaService : IFerreteriaService
    {
        private readonly ILogger<FerreteriaService> _logger;
        private readonly IMapper _mapper;
        private readonly IFerreteriaRepository _ferreteriaRepository;

        public FerreteriaService(
            ILogger<FerreteriaService> logger,
            IMapper mapper,
            IFerreteriaRepository ferreteriaRepository)
        {
            _logger = logger;
            _mapper = mapper;
            _ferreteriaRepository = ferreteriaRepository;
        }

        public async Task<Result<FerreteriaResponseDTO>> GetAsync()
        {
            try
            {
                var ferreteria = await _ferreteriaRepository.GetAsync();

                if (ferreteria == null)
                {
                    _logger.LogWarning("No se encontró la configuración de la ferretería");
                    return Result<FerreteriaResponseDTO>.Failure(FerreteriaErrorCode.ferreteria_not_found);
                }

                var ferreteriaDTO = _mapper.Map<FerreteriaResponseDTO>(ferreteria);
                return Result<FerreteriaResponseDTO>.Success(ferreteriaDTO);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error inesperado al obtener la ferretería: " + ex.ToString());
                return Result<FerreteriaResponseDTO>.Failure(FerreteriaErrorCode.unexpected_error);
            }
        }

        public async Task<Result<bool>> UpdateAsync(FerreteriaUpdateDTO ferreteriaDTO)
        {
            try
            {
                var ferreteria = await _ferreteriaRepository.GetAsync();

                if (ferreteria == null)
                {
                    _logger.LogWarning("No se encontró la configuración de la ferretería para actualizar");
                    return Result<bool>.Failure(FerreteriaErrorCode.ferreteria_not_found);
                }

                // Actualizar los campos con los valores del DTO
                ferreteria.Nombre = ferreteriaDTO.Nombre;
                ferreteria.Direccion = ferreteriaDTO.Direccion;
                ferreteria.Telefono = ferreteriaDTO.Telefono;
                ferreteria.Email = ferreteriaDTO.Email;
                ferreteria.Cuit = ferreteriaDTO.Cuit;
                ferreteria.LogoUrl = ferreteriaDTO.LogoUrl;

                await _ferreteriaRepository.UpdateAsync(ferreteria);

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error inesperado al actualizar la ferretería: " + ex.ToString());
                return Result<bool>.Failure(FerreteriaErrorCode.update_failed);
            }
        }
    }
}
