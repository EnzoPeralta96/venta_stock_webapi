using Microsoft.EntityFrameworkCore.Query;
using proyecto_venta_stock.Models;
using proyecto_venta_stock.Shared.ResultPattern;
using venta_stock_webapi.Client.Message;
using venta_stock_webapi.Client.Repository;
using venta_stock_webapi.CurrentAccount.DTO.MovementDTO;
using venta_stock_webapi.CurrentAccount.Message;
using venta_stock_webapi.CurrentAccount.Repository;
using venta_stock_webapi.CurrentAccount.Services.CurrentAccountService.StrategyCurrentAccount;

namespace venta_stock_webapi.CurrentAccount.Services.CurrentAccountService
{
    public class CurrentAccountService : ICurrentAccountService
    {
        private readonly IAccountMovementRepository _accountMovementRepository;
        private readonly IClientRepository _clientRepository;
        private readonly AutoMapper.IMapper _mapper;
        private readonly ILogger<CurrentAccountService> _logger;
        private readonly MovementStrategyFactory _movementStrategyFactory;
        public CurrentAccountService(IAccountMovementRepository accountMovementRepository, AutoMapper.IMapper mapper, ILogger<CurrentAccountService> logger, IClientRepository clientRepository, MovementStrategyFactory movementStrategyFactory)
        {
            _accountMovementRepository = accountMovementRepository;
            _clientRepository = clientRepository;
            _mapper = mapper;
            _logger = logger;
            _movementStrategyFactory = movementStrategyFactory;
        }

        public async Task<Result<List<AccountMovementDTO>>> GetAccountMovementsByClientId(int clientId)
        {
            try
            {
                var client = await _clientRepository.ExistsByIdAsync(clientId);

                if (!client)
                {
                    _logger.LogWarning("Client with ID {ClientId} not found", clientId);
                    return Result<List<AccountMovementDTO>>.Failure(CurrentAccountCode.account_not_found);
                }

                var movements = await _accountMovementRepository.GetMovements(clientId);

                var movementDTOs = _mapper.Map<List<AccountMovementDTO>>(movements);

                return Result<List<AccountMovementDTO>>.Success(movementDTOs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting account movements for client {ClientId}", clientId);
                return Result<List<AccountMovementDTO>>.Failure(ClientErrorCode.unexpected_error);
            }
        }

        public async Task<Result<string>> CreateAccountMovement(CreateCurrentAccountDTO accountMovementDTO)
        {
            try
            {
                //Agregar validaciones de usuario.
                var clientExists = await _clientRepository.ExistsByIdAsync(accountMovementDTO.IdCliente);

                if (!clientExists)
                {
                    _logger.LogWarning("Client with ID {ClientId} not found", accountMovementDTO.IdCliente);
                    return Result<string>.Failure(ClientErrorCode.cliente_not_found);
                }

                var accountMovement = _mapper.Map<MovimientoCc>(accountMovementDTO);
                
                accountMovement.Detalle = await _accountMovementRepository.GetDetailMovement((int)accountMovement.IdTipoMovimiento);

                await _accountMovementRepository.CreateMovement(accountMovement);

                return Result<string>.Success("Creacion de cuenta exitosa");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding account movement for client {ClientId}", accountMovementDTO.IdCliente);
                return Result<string>.Failure(CurrentAccountCode.unexpected_error);
            }
        }

        public async Task<Result<bool>> RegisterMovement(AddMovementDTO addMovementDTO)
        {
            try
            {
                var lastMovement = await _accountMovementRepository.GetLastMovement(addMovementDTO.IdCliente);

                decimal balanceBase = lastMovement?.SaldoActual ?? 0;
                decimal limitBase = lastMovement?.LimiteCuenta ?? 0;

                IMovementStrategy movementStrategy = _movementStrategyFactory.GetStrategy((TypeMovement)addMovementDTO.IdTipoMovimiento);

                CalculationResult calculationResult = movementStrategy.Calculate(balanceBase, limitBase, addMovementDTO.Importe);
                
                var newMovement = new MovimientoCc
                {
                    IdCliente = addMovementDTO.IdCliente,
                    Importe = addMovementDTO.Importe,
                    Detalle = addMovementDTO.Detalle,
                    IdTipoMovimiento = addMovementDTO.IdTipoMovimiento,
                    IdUsuarioRegistra = addMovementDTO.IdUsuarioRegistra,
                    SaldoActual = calculationResult.NewBalance,
                    LimiteCuenta = calculationResult.NewLimit,
                    Fecha = DateTime.Now
                };

                await _accountMovementRepository.CreateMovement(newMovement);

                return Result<bool>.Success(true);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error registering movement for client {ClientId}", addMovementDTO.IdCliente);
                return Result<bool>.Failure(CurrentAccountCode.unexpected_error);
            }
        }

        public async Task<Result<List<TypeMovementDTO>>> GetMovementTypes()
        {
            try
            {
                var types = await _accountMovementRepository.GetMovementType();
                var typesDTO = _mapper.Map<List<TypeMovementDTO>>(types);
                return Result<List<TypeMovementDTO>>.Success(typesDTO);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error getting movement types");
                return Result<List<TypeMovementDTO>>.Failure(CurrentAccountCode.unexpected_error);
            }
        }
    }
}