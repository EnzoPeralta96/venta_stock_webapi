using AutoMapper;
using proyecto_venta_stock.Models;
using proyecto_venta_stock.Shared.ResultPattern;
using venta_stock_webapi.CurrentAccount.DTO.InterestConfigDTO;
using venta_stock_webapi.CurrentAccount.Message;
using venta_stock_webapi.CurrentAccount.Repository;

namespace venta_stock_webapi.CurrentAccount.Services.InterestConfigService;

public class InterestConfigService : IInterestConfigService
{
    private readonly IInterestConfigRepository _interestConfigRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<InterestConfigService> _logger;

    public InterestConfigService(
        IInterestConfigRepository interestConfigRepository,
        IMapper mapper,
        ILogger<InterestConfigService> logger)
    {
        _interestConfigRepository = interestConfigRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<InterestConfigDTO>> GetById(int idConfig)
    {
        try
        {
            var config = await _interestConfigRepository.GetByIdAsync(idConfig);

            if (config is null)
                return Result<InterestConfigDTO>.Failure(InterestConfigCode.config_not_found);

            return Result<InterestConfigDTO>.Success(_mapper.Map<InterestConfigDTO>(config));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting interest config by ID {IdConfig}", idConfig);
            return Result<InterestConfigDTO>.Failure(InterestConfigCode.unexpected_error);
        }
    } 

    public async Task<Result<List<InterestConfigDTO>>> GetAll()
    {
        try
        {
            var configs = await _interestConfigRepository.GetAllAsync();
            return Result<List<InterestConfigDTO>>.Success(_mapper.Map<List<InterestConfigDTO>>(configs));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all interest configs");
            return Result<List<InterestConfigDTO>>.Failure(InterestConfigCode.unexpected_error);
        }
    }

    public async Task<Result<InterestConfigDTO>> GetCurrent()
    {
        try
        {
            var config = await _interestConfigRepository.GetCurrentAsync();
            
            if (config is null)
                return Result<InterestConfigDTO>.Failure(InterestConfigCode.no_active_config);

            return Result<InterestConfigDTO>.Success(_mapper.Map<InterestConfigDTO>(config));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current interest config");
            return Result<InterestConfigDTO>.Failure(InterestConfigCode.unexpected_error);
        }
    }

    public async Task<Result<string>> Create(CreateInterestConfigDTO dto)
    {
        try
        {
            if (await _interestConfigRepository.ExistsByNameAsync(dto.Nombre))
                return Result<string>.Failure(InterestConfigCode.config_name_exists);

            var config = _mapper.Map<ConfiguracionInteres>(dto);
            await _interestConfigRepository.CreateAsync(config);

            return Result<string>.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating interest config");
            return Result<string>.Failure(InterestConfigCode.unexpected_error);
        }
    }

    public async Task<Result<string>> Update(UpdateInterestConfigDTO dto)
    {
        try
        {
            if (await _interestConfigRepository.GetByIdAsync(dto.IdConfig) is null)
                return Result<string>.Failure(InterestConfigCode.config_not_found);

            if (await _interestConfigRepository.ExistsByNameAsync(dto.IdConfig, dto.Nombre))
                return Result<string>.Failure(InterestConfigCode.config_name_exists);

            var config = _mapper.Map<ConfiguracionInteres>(dto);
            await _interestConfigRepository.UpdateAsync(config);

            return Result<string>.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating interest config");
            return Result<string>.Failure(InterestConfigCode.unexpected_error);
        }
    }

    public async Task<Result<string>> SetAsCurrent(int idConfig)
    {
        try
        {
            if (await _interestConfigRepository.GetByIdAsync(idConfig) is null)
                return Result<string>.Failure(InterestConfigCode.config_not_found);

            await _interestConfigRepository.SetAsCurrentAsync(idConfig);

            return Result<string>.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting interest config as current");
            return Result<string>.Failure(InterestConfigCode.unexpected_error);
        }
    }
}
