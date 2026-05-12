using AutoMapper;
using System.Text.Json;
using venta_stock_webapi.Client.DTO;
using venta_stock_webapi.Client.Message;
using venta_stock_webapi.Client.Repository;
using proyecto_venta_stock.Data;
using proyecto_venta_stock.Shared.ResultPattern;
using proyecto_venta_stock.Models;
using venta_stock_webapi.Shared.Paged;
using venta_stock_webapi.CurrentAccount.Repository;
using venta_stock_webapi.Data.Audit;
using venta_stock_webapi.Shared.Identity;
using venta_stock_webapi.Features.Audit.Repository;

namespace venta_stock_webapi.Client.Services
{
    public class ClientService : IClientService
    {
        private readonly IClientRepository _clienteRepository;
        private readonly IAccountMovementRepository _accountMovementRepository;
        private readonly VentaStockContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<ClientService> _logger;
        private readonly IUserContext _userContext;
        private readonly IAuditRepository _auditRepository;

        public ClientService(
            IClientRepository clienteRepository,
            VentaStockContext context,
            IMapper mapper,
            ILogger<ClientService> logger,
            IAccountMovementRepository accountMovementRepository,
            IUserContext userContext,
            IAuditRepository auditRepository)
        {
            _clienteRepository = clienteRepository;
            _context = context;
            _mapper = mapper;
            _logger = logger;
            _accountMovementRepository = accountMovementRepository;
            _userContext = userContext;
            _auditRepository = auditRepository;
        }

        private async Task LogAsync(string accion, string entidadTipo, string detalle,
            object? anterior = null, object? nuevo = null)
        {
            try
            {
                await _auditRepository.LogAsync(new Auditoria
                {
                    FechaHora = DateTimeOffset.UtcNow,
                    IdUsuario = _userContext.UserId,
                    UsuarioNombre = _userContext.UserName,
                    Accion = accion,
                    EntidadTipo = entidadTipo,
                    Detalle = detalle,
                    ValoresAnteriores = anterior != null ? JsonSerializer.Serialize(anterior) : null,
                    ValoresNuevos = nuevo != null ? JsonSerializer.Serialize(nuevo) : null,
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo registrar auditoría.");
            }
        }

        private async Task LogCreationClientAsync(ClientCreateDTO clienteDTO)
        {

            string nombreCliente = clienteDTO.EsEmpresa
                ? $"'{clienteDTO.RazonSocial}' | CUIT: {clienteDTO.Cuit}"
                : $"'{clienteDTO.Nombre} {clienteDTO.Apellido}' | DNI: {clienteDTO.Dni}";

            object nuevoClienteAudit = clienteDTO.EsEmpresa
                ? (object)new { RazonSocial = clienteDTO.RazonSocial, CUIT = clienteDTO.Cuit, Telefono = clienteDTO.Telefono, Mail = clienteDTO.Mail }
                : new { Nombre = clienteDTO.Nombre, Apellido = clienteDTO.Apellido, DNI = clienteDTO.Dni, Telefono = clienteDTO.Telefono, Mail = clienteDTO.Mail };

            await LogAsync("CREACION", "CLIENTE", $"Cliente creado: {nombreCliente}", null, nuevoClienteAudit);

            if (clienteDTO.TieneCuentaCorriente)
            {
                string nombreCC = clienteDTO.EsEmpresa
                    ? clienteDTO.RazonSocial ?? "N/A"
                    : $"{clienteDTO.Nombre} {clienteDTO.Apellido}";
                await LogAsync("CC_CREADA", "CLIENTE",
                    $"Cuenta corriente habilitada: '{nombreCC}' | Límite: ${clienteDTO.LimiteCuenta!.Value:N2} | Saldo inicial: ${clienteDTO.SaldoInicial ?? 0:N2}",
                    null,
                    new { cliente = nombreCC, limiteCredito = clienteDTO.LimiteCuenta!.Value, saldoInicial = clienteDTO.SaldoInicial ?? 0 });
            }
        }

        private async Task CreateCurrentAccount(ClientCreateDTO clienteDTO, Cliente clienteCreado)
        {
            var movimientoCC = new MovimientoCc
            {
                IdTipoMovimiento = 2, // Alta cliente
                IdEstado = 2, // Aprobado
                Fecha = DateTime.Now,
                Importe = 0,
                Detalle = "Saldo inicial del cliente al registrarse",
                LimiteCuenta = clienteDTO.LimiteCuenta!.Value,
                SaldoActual = clienteDTO.SaldoInicial ?? 0,
                IdUsuarioRegistra = clienteDTO.idUsuarioRegistra,
                IdCliente = clienteCreado.IdCliente,
                IdVenta = null,
                IdUsuarioAutoriza = null,
                FechaAutorizacion = null
            };

            await _accountMovementRepository.CreateMovement(movimientoCC);
        }

        private async Task<(bool flowControl, Result<ClientResponseDTO> value)> ValidateRulesCreate(ClientCreateDTO clienteDTO)
        {
            // Validar unicidad de Email (global para todos los clientes)
            if (await _clienteRepository.EmailExistsAsync(clienteDTO.Mail))
            {
                return (flowControl: false, value: Result<ClientResponseDTO>.Failure(ClientErrorCode.email_in_use));
            }

            // Validaciones específicas según tipo de cliente
            if (clienteDTO.EsEmpresa)
            {
                // EMPRESA: Validar RazonSocial única
                if (await _clienteRepository.EnterpriseExistsAsync(clienteDTO.RazonSocial))
                {
                    return (flowControl: false, value: Result<ClientResponseDTO>.Failure(ClientErrorCode.empresa_in_use));
                }
                // CUIT NO se valida (puede repetirse)
            }
            else
            {
                // PERSONA FÍSICA: Validar DNI único
                if (await _clienteRepository.DniExistsAsync(clienteDTO.Dni))
                {
                    return (flowControl: false, value: Result<ClientResponseDTO>.Failure(ClientErrorCode.dni_in_use));
                }
            }

            // Validar que si tiene CC, debe venir el límite de cuenta
            if (clienteDTO.TieneCuentaCorriente && !clienteDTO.LimiteCuenta.HasValue)
            {
                return (flowControl: false, value: Result<ClientResponseDTO>.Failure(ClientErrorCode.limite_cuenta_required));
            }

            return (flowControl: true, value: null);
        }


        public async Task<Result<ClientResponseDTO>> CreateClienteAsync(ClientCreateDTO clienteDTO)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                //Validar reglas de negocio y unicidad antes de crear el cliente
                (bool flowControl, Result<ClientResponseDTO> value) = await ValidateRulesCreate(clienteDTO);

                if (!flowControl)
                {
                    return value;
                }

                // Crear el cliente
                var cliente = _mapper.Map<Cliente>(clienteDTO);

                cliente.FechaAlta = DateOnly.FromDateTime(DateTime.Now);

                var clienteCreado = await _clienteRepository.CreateAsync(cliente);

                // Si tiene cuenta corriente, crear el MovimientoCC inicial
                if (clienteDTO.TieneCuentaCorriente)
                {
                    await CreateCurrentAccount(clienteDTO, clienteCreado);
                }

                await transaction.CommitAsync();

                // Auditoría de creación de cliente
                await LogCreationClientAsync(clienteDTO);

                // Obtener el cliente recién creado con sus relaciones
                var clienteCompleto = await _clienteRepository.GetByIdAsync(clienteCreado.IdCliente);

                var responseDTO = _mapper.Map<ClientResponseDTO>(clienteCompleto);

                return Result<ClientResponseDTO>.Success(responseDTO);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError("Error inesperado al crear cliente: " + ex);
                return Result<ClientResponseDTO>.Failure(ClientErrorCode.unexpected_error);
            }
        }


        public async Task<Result<ClientResponseDTO>> GetClient(int id)
        {
            try
            {
                var cliente = await _clienteRepository.GetByIdAsync(id);

                if (cliente is null)
                    return Result<ClientResponseDTO>.Failure(ClientErrorCode.cliente_not_found);

                var responseDTO = _mapper.Map<ClientResponseDTO>(cliente);
                return Result<ClientResponseDTO>.Success(responseDTO);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error inesperado al obtener cliente: " + ex);
                return Result<ClientResponseDTO>.Failure(ClientErrorCode.unexpected_error);
            }
        }

        public async Task<Result<PagedList<ClientResponseDTO>>> Search(int pageIndex, int pageSize, string searchTerm, string estado = "activos")
        {
            try
            {
                var query = _clienteRepository.ClientsQueryable(searchTerm);

                if (estado.ToLower() == "activos")
                    query = query.Where(c => c.FechaBaja == null);
                else if (estado.ToLower() == "eliminados")
                    query = query.Where(c => c.FechaBaja != null);

                var projected = _mapper.ProjectTo<ClientResponseDTO>(query);

                var paged = await PagedList<ClientResponseDTO>.CreateAsync(projected, pageIndex, pageSize);

                return Result<PagedList<ClientResponseDTO>>.Success(paged);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error inesperado en búsqueda de clientes: " + ex);
                return Result<PagedList<ClientResponseDTO>>.Failure(ClientErrorCode.unexpected_error);
            }
        }

        private async Task<(bool flowControl, Result<ClientResponseDTO> value)> ValidateRulesUpdate(ClientUpdateDTO clienteDTO)
        {
            if (await _clienteRepository.EmailExistsForOtherClientAsync(clienteDTO.Mail, clienteDTO.IdCliente))
                return (false, Result<ClientResponseDTO>.Failure(ClientErrorCode.email_in_use));

            if (clienteDTO.EsEmpresa)
            {
                if (await _clienteRepository.EnterpriseExistsForOtherClientAsync(clienteDTO.RazonSocial, clienteDTO.IdCliente))
                    return (false, Result<ClientResponseDTO>.Failure(ClientErrorCode.empresa_in_use));
            }
            else
            {
                if (await _clienteRepository.DniExistsForOtherClientAsync(clienteDTO.Dni, clienteDTO.IdCliente))
                    return (false, Result<ClientResponseDTO>.Failure(ClientErrorCode.dni_in_use));
            }

            return (true, null);
        }

        private (Dictionary<string, object?> anterior, Dictionary<string, object?> nuevo) ApplyAndTrackChanges(Cliente existente, ClientUpdateDTO dto)
        {
            var anterior = new Dictionary<string, object?>();
            var nuevo    = new Dictionary<string, object?>();

            void Track(string campo, object? old, object? newVal)
            {
                if (!Equals(old, newVal)) { anterior[campo] = old; nuevo[campo] = newVal; }
            }

            Track("Nombre",      existente.Nombre,      dto.Nombre);
            Track("Apellido",    existente.Apellido,    dto.Apellido);
            Track("RazonSocial", existente.RazonSocial, dto.RazonSocial);
            Track("DNI",         existente.Dni,         dto.Dni);
            Track("CUIT",        existente.Cuit,        dto.Cuit);
            Track("Telefono",    existente.Telefono,    dto.Telefono);
            Track("Mail",        existente.Mail,        dto.Mail);

            existente.Nombre      = dto.Nombre;
            existente.Apellido    = dto.Apellido;
            existente.RazonSocial = dto.RazonSocial;
            existente.Dni         = dto.Dni;
            existente.Cuit        = dto.Cuit;
            existente.Telefono    = dto.Telefono;
            existente.Mail        = dto.Mail;

            return (anterior, nuevo);
        }

        private async Task LogUpdateClientAsync(ClientUpdateDTO clienteDTO, Dictionary<string, object?> anterior, Dictionary<string, object?> nuevo)
        {
            string nombreCliente = clienteDTO.EsEmpresa
                ? clienteDTO.RazonSocial
                : $"{clienteDTO.Nombre} {clienteDTO.Apellido}".Trim();

            await LogAsync("ACTUALIZACION", "CLIENTE",
                $"Cliente actualizado: '{nombreCliente}' | Tel: {clienteDTO.Telefono} | Mail: {clienteDTO.Mail}",
                anterior.Count > 0 ? anterior : null,
                nuevo.Count    > 0 ? nuevo    : null);
        }

        public async Task<Result<ClientResponseDTO>> UpdateClient(ClientUpdateDTO clienteDTO)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var clienteExistente = await _clienteRepository.GetByIdAsync(clienteDTO.IdCliente);

                if (clienteExistente is null)
                    return Result<ClientResponseDTO>.Failure(ClientErrorCode.cliente_not_found);

                (bool flowControl, Result<ClientResponseDTO> value) = await ValidateRulesUpdate(clienteDTO);

                if (!flowControl)
                    return value;

                var (anterior, nuevo) = ApplyAndTrackChanges(clienteExistente, clienteDTO);

                await _clienteRepository.UpdateAsync(clienteExistente);

                await transaction.CommitAsync();

                await LogUpdateClientAsync(clienteDTO, anterior, nuevo);

                var clienteActualizado = await _clienteRepository.GetByIdAsync(clienteDTO.IdCliente);
                
                return Result<ClientResponseDTO>.Success(_mapper.Map<ClientResponseDTO>(clienteActualizado));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError("Error inesperado al actualizar cliente: " + ex);
                return Result<ClientResponseDTO>.Failure(ClientErrorCode.unexpected_error);
            }
        }

        private (bool flowControl, Result<string> value) ValidateRulesToggle(Cliente cliente, bool isActive)
        {
            if (!isActive && cliente.FechaBaja != null)
                return (false, Result<string>.Failure(ClientErrorCode.cliente_already_inactive));

            if (isActive && cliente.FechaBaja == null)
                return (false, Result<string>.Failure(ClientErrorCode.cliente_already_active));

            return (true, null);
        }

        private async Task LogToggleStatusAsync(string nombreCliente, bool isActive)
        {
            if (!isActive)
                await LogAsync("BAJA", "CLIENTE", $"Cliente dado de baja: '{nombreCliente}'",
                    new { Activo = true }, new { Activo = false });
            else
                await LogAsync("REACTIVACION", "CLIENTE", $"Cliente reactivado: '{nombreCliente}'",
                    new { Activo = false }, new { Activo = true });
        }

        public async Task<Result<string>> ToggleStatus(ClientToggleStatusDTO dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var cliente = await _clienteRepository.GetByIdAsync(dto.IdCliente);

                if (cliente == null)
                    return Result<string>.Failure(ClientErrorCode.cliente_not_found);

                (bool flowControl, Result<string> value) = ValidateRulesToggle(cliente, dto.IsActive);

                if (!flowControl)
                    return value;

                string nombreCliente = !string.IsNullOrWhiteSpace(cliente.RazonSocial)
                    ? cliente.RazonSocial
                    : $"{cliente.Nombre} {cliente.Apellido}";

                DateOnly? fechaBaja = dto.IsActive ? null : DateOnly.FromDateTime(DateTime.Now);

                await _clienteRepository.UpdateStatusAsync(dto.IdCliente, fechaBaja);
                await transaction.CommitAsync();

                await LogToggleStatusAsync(nombreCliente, dto.IsActive);

                string mensaje = dto.IsActive ? "Cliente reactivado exitosamente." : "Cliente dado de baja exitosamente.";
                return Result<string>.Success(mensaje);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError("Error inesperado al cambiar estado del cliente: " + ex);
                return Result<string>.Failure(ClientErrorCode.unexpected_error);
            }
        }
    }
}