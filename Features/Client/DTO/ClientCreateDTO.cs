using System.ComponentModel.DataAnnotations;

namespace venta_stock_webapi.Client.DTO
{
    public class ClientCreateDTO : IValidatableObject
    {
        // 👉 OBLIGATORIOS SOLO PARA CLIENTE COMÚN (cuando EsEmpresa == false)
        [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
        public string Nombre { get; set; }

        [StringLength(100, ErrorMessage = "El apellido no puede superar los 100 caracteres.")]
        public string Apellido { get; set; }

        // Validación de formato delegada a IValidatableObject (condicional según EsEmpresa)
        public string Dni { get; set; }

        public bool EsEmpresa { get; set; }

        // 👉 OBLIGATORIOS SOLO PARA EMPRESA (cuando EsEmpresa == true)
        [StringLength(200, ErrorMessage = "La razón social no puede superar los 200 caracteres.")]
        public string RazonSocial { get; set; }

        // Validación de formato delegada a IValidatableObject (condicional según EsEmpresa)
        public string Cuit { get; set; }

        // 👉 SIEMPRE OBLIGATORIOS
        [Required(ErrorMessage = "El campo Teléfono es obligatorio.")]
        [RegularExpression(@"^\d{7,15}$", ErrorMessage = "El teléfono debe contener solo dígitos (7 a 15).")]
        [StringLength(15, MinimumLength = 7, ErrorMessage = "El teléfono debe tener entre 7 y 15 dígitos.")]
        public string Telefono { get; set; } = string.Empty;

        [Required(ErrorMessage = "El campo Mail es obligatorio.")]
        [EmailAddress(ErrorMessage = "El campo Mail no es una dirección de correo electrónico válida.")]
        [StringLength(200, ErrorMessage = "El email no puede superar los 200 caracteres.")]
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
                else if (!System.Text.RegularExpressions.Regex.IsMatch(Cuit, @"^\d{11}$"))
                {
                    yield return new ValidationResult(
                        "El CUIT debe contener exactamente 11 dígitos numéricos.",
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
                else if (!System.Text.RegularExpressions.Regex.IsMatch(Dni, @"^\d{7,8}$"))
                {
                    yield return new ValidationResult(
                        "El DNI debe contener entre 7 y 8 dígitos numéricos.",
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
                        "El límite de cuenta es obligatorio y debe ser mayor a 0 cuando el cliente tiene cuenta corriente.",
                        new[] { nameof(LimiteCuenta) }
                    );
                }

                if (SaldoInicial.HasValue && SaldoInicial.Value == 0)
                {
                    yield return new ValidationResult(
                        "El saldo inicial no puede ser cero.",
                        new[] { nameof(SaldoInicial) }
                    );
                }
            }
        }
    }
}
