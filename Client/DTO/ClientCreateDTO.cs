using System.ComponentModel.DataAnnotations;

namespace venta_stock_webapi.Client.DTO
{
    public class ClientCreateDTO : IValidatableObject
    {
        // 👉 OBLIGATORIOS SOLO PARA CLIENTE COMÚN (cuando EsEmpresa == false)
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string Dni { get; set; }

        public bool EsEmpresa { get; set; }

        // 👉 OBLIGATORIOS SOLO PARA EMPRESA (cuando EsEmpresa == true)
        public string RazonSocial { get; set; }
        public string Cuit { get; set; }

        // 👉 SIEMPRE OBLIGATORIOS
        [Required(ErrorMessage = "El campo Teléfono es obligatorio.")]
        public string Telefono { get; set; } = string.Empty;

        [Required(ErrorMessage = "El campo Mail es obligatorio.")]
        [EmailAddress(ErrorMessage = "El campo Mail no es una dirección de correo electrónico válida.")]
        public string Mail { get; set; } = string.Empty;

        // 👉 CUENTA CORRIENTE OPCIONAL
        public bool TieneCuentaCorriente { get; set; }
        public decimal? LimiteCuenta { get; set; }
        public decimal? SaldoInicial { get; set; }

        public int idUsuarioRegistra { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // ---------------------------
            // CLIENTE EMPRESA
            // ---------------------------
            if (EsEmpresa)
            {
                if (string.IsNullOrWhiteSpace(RazonSocial))
                {
                    yield return new ValidationResult(
                        "El campo Razón Social es obligatorio para empresas.",
                        new[] { nameof(RazonSocial) }
                    );
                }

                if (string.IsNullOrWhiteSpace(Cuit))
                {
                    yield return new ValidationResult(
                        "El campo CUIT es obligatorio para empresas.",
                        new[] { nameof(Cuit) }
                    );
                }
            }
            // ---------------------------
            // CLIENTE COMÚN / PERSONA
            // ---------------------------
            else
            {
                if (string.IsNullOrWhiteSpace(Nombre))
                {
                    yield return new ValidationResult(
                        "El campo Nombre es obligatorio para clientes comunes.",
                        new[] { nameof(Nombre) }
                    );
                }

                if (string.IsNullOrWhiteSpace(Apellido))
                {
                    yield return new ValidationResult(
                        "El campo Apellido es obligatorio para clientes comunes.",
                        new[] { nameof(Apellido) }
                    );
                }

                if (string.IsNullOrWhiteSpace(Dni))
                {
                    yield return new ValidationResult(
                        "El campo DNI es obligatorio para clientes comunes.",
                        new[] { nameof(Dni) }
                    );
                }
            }

            // ---------------------------
            // CUENTA CORRIENTE (OPCIONAL)
            // ---------------------------
            if (TieneCuentaCorriente)
            {
                if (!LimiteCuenta.HasValue || LimiteCuenta <= 0)
                {
                    yield return new ValidationResult(
                        "El campo LimiteCuenta es obligatorio y debe ser mayor a 0 cuando el cliente tiene cuenta corriente.",
                        new[] { nameof(LimiteCuenta) }
                    );
                }

                if (SaldoInicial.HasValue && SaldoInicial.Value < 0)
                {
                    yield return new ValidationResult(
                        "El campo SaldoInicial no puede ser negativo.",
                        new[] { nameof(SaldoInicial) }
                    );
                }
            }
        }

    }
}



