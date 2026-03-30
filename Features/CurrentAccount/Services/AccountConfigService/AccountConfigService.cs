using AutoMapper;
using proyecto_venta_stock.Data;
using proyecto_venta_stock.Models;
using proyecto_venta_stock.Shared.ResultPattern;
using venta_stock_webapi.CurrentAccount.DTO;
using venta_stock_webapi.CurrentAccount.Message;
using venta_stock_webapi.CurrentAccount.Repository;

namespace venta_stock_webapi.CurrentAccount.Services.AccountConfigService
{
    public class AccountConfigService : IAccountConfigService
    {
        private readonly IAccountConfigRepository _accountConfigRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<AccountConfigService> _logger;
        private readonly VentaStockContext _context;

        public AccountConfigService(IAccountConfigRepository accountConfigRepository, IMapper mapper, ILogger<AccountConfigService> logger, VentaStockContext context)
        {
            _accountConfigRepository = accountConfigRepository;
            _mapper = mapper;
            _logger = logger;
            _context = context;
        }

        public async Task<Result<AccountConfigDTO>> GetAccountConfigById(int configId)
        {
            try
            {
                var config = await _accountConfigRepository.GetAccountConfigByIdAsync(configId);

                if (config == null)
                {
                    return Result<AccountConfigDTO>.Failure(AccountConfigCode.account_config_not_found);
                }

                var configDTO = _mapper.Map<AccountConfigDTO>(config);

                return Result<AccountConfigDTO>.Success(configDTO);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error retrieving account config by ID");
                return Result<AccountConfigDTO>.Failure(AccountConfigCode.unexpected_error);
            }
        }

        public async Task<Result<List<AccountConfigDTO>>> GetAccountConfigs(bool? activo = null)
        {
            try
            {
                var configs = await _accountConfigRepository.GetAccountConfigsAsync(activo);

                var configsDTO = _mapper.Map<List<AccountConfigDTO>>(configs);

                return Result<List<AccountConfigDTO>>.Success(configsDTO);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error retrieving account configs");
                return Result<List<AccountConfigDTO>>.Failure(AccountConfigCode.unexpected_error);
            }
        }


        public async Task<Result<string>> CreateAccountConfig(CreateAccountConfigDTO accountConfigDTO)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (await _accountConfigRepository.AccountConfigExistsByNameAsync(accountConfigDTO.Nombre))
                    return Result<string>.Failure(AccountConfigCode.account_config_name_exists);

                if (await _accountConfigRepository.AccountConfigExistsByLimitAsync(accountConfigDTO.MontoLimite))
                    return Result<string>.Failure(AccountConfigCode.account_config_limit_exists);

                var config = _mapper.Map<ConfiguracionCc>(accountConfigDTO);

                await _accountConfigRepository.CreateAccountConfigAsync(config);

                await transaction.CommitAsync();
                return Result<string>.Success();
            }
            catch (System.Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating account config");
                return Result<string>.Failure(AccountConfigCode.unexpected_error);
            }
        }

        public async Task<Result<string>> UpdateAccountConfig(UpdateAccountConfigDTO accountConfigDTO)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (await _accountConfigRepository.AccountConfigExistsByNameAsync(accountConfigDTO.IdConfig, accountConfigDTO.Nombre))
                    return Result<string>.Failure(AccountConfigCode.account_config_name_exists);

                if (await _accountConfigRepository.AccountConfigExistsByLimitAsync(accountConfigDTO.IdConfig, accountConfigDTO.MontoLimite))
                    return Result<string>.Failure(AccountConfigCode.account_config_limit_exists);

                var config = _mapper.Map<ConfiguracionCc>(accountConfigDTO);

                await _accountConfigRepository.UpdateAccountConfigAsync(config);

                await transaction.CommitAsync();
                return Result<string>.Success();
            }
            catch (System.Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error updating account config");
                return Result<string>.Failure(AccountConfigCode.unexpected_error);
            }
        }

        public async Task<Result<string>> ToggleStateAccountConfig(int configId, bool active)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (await _accountConfigRepository.GetAccountConfigByIdAsync(configId) is null)
                    return Result<string>.Failure(AccountConfigCode.account_config_not_found);

                await _accountConfigRepository.ToggleStateAccountConfigAsync(configId, active);

                await transaction.CommitAsync();
                return Result<string>.Success();
            }
            catch (System.Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error toggling account config state");
                return Result<string>.Failure(AccountConfigCode.unexpected_error);
            }
        }
    }
}