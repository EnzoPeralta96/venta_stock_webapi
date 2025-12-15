using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using proyecto_venta_stock.Data;
using proyecto_venta_stock.Models;

namespace venta_stock_webapi.Features.Login.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PasswordActualizacionController : ControllerBase
    {
        private readonly VentaStockContext _dbContext;
        private readonly ILogger<PasswordActualizacionController> _logger;

        public PasswordActualizacionController(VentaStockContext dbContext, ILogger<PasswordActualizacionController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        /// <summary>
        /// Endpoint temporal para migrar contraseñas de texto plano a hash.
        /// IMPORTANTE: Este endpoint debe ser eliminado después de la migración.
        /// </summary>
        [HttpPost("migrate-passwords")]
        public IActionResult MigratePasswords()
        {
            try
            {
                var hasher = new PasswordHasher<Usuario>();
                var users = _dbContext.Usuarios.ToList();
                int updatedCount = 0;

                _logger.LogInformation($"Iniciando migración de contraseñas. Total usuarios: {users.Count}");

                foreach (var user in users)
                {
                    // Solo si todavía está en texto plano
                    if (!user.Password.StartsWith("AQAAAA"))
                    {
                        var plainPassword = user.Password;
                        user.Password = hasher.HashPassword(user, plainPassword);
                        updatedCount++;

                        _logger.LogInformation($"Usuario '{user.Usuario1}' (ID: {user.IdUsuario}) - Contraseña migrada");
                    }
                    else
                    {
                        _logger.LogInformation($"Usuario '{user.Usuario1}' (ID: {user.IdUsuario}) - Ya tiene hash, omitido");
                    }
                }

                if (updatedCount > 0)
                {
                    _dbContext.SaveChanges();
                    _logger.LogInformation($"Migración completada. {updatedCount} contraseñas actualizadas.");
                }
                else
                {
                    _logger.LogInformation("No se encontraron contraseñas en texto plano.");
                }

                return Ok(new
                {
                    message = "Migración de contraseñas completada exitosamente",
                    totalUsuarios = users.Count,
                    usuariosActualizados = updatedCount,
                    usuariosYaHasheados = users.Count - updatedCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error durante la migración de contraseñas: {ex}");
                return StatusCode(500, new
                {
                    message = "Error durante la migración de contraseñas",
                    error = ex.Message
                });
            }
        }
    }
}
