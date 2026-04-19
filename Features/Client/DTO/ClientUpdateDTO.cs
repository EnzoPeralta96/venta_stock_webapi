using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace venta_stock_webapi.Client.DTO
{
    public class ClientUpdateDTO : IValidatableObject
    {
        [Required(ErrorMessage = "El ID del cliente es obligatorio.")]
        public int IdCliente { get; set; }

        // Obligatorios solo para clientes comunes (EsEmpresa == false)
        [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
        public string Nombre { get; set; }

        [StringLength(100, ErrorMessage = "El apellido no puede superar los 100 caracteres.")]
        public string Apellido { get; set; }

        // Obligatorios solo para empresas (EsEmpresa == true)
        [StringLength(200, ErrorMessage = "La razón social no puede superar los 200 caracteres.")]
        public string RazonSocial { get; set; }

        // Validación de formato delegada a IValidatableObject (condicional según EsEmpresa)
        public string Dni { get; set; }
        public string Cuit { get; set; }

        // Siempre obligatorios
        [Required(ErrorMessage = "El teléfono es obligatorio.")]
        [RegularExpression(@"^\d{7,15}$", ErrorMessage = "El teléfono debe contener solo dígitos (7 a 15).")]
        [StringLength(15, MinimumLength = 7, ErrorMessage = "El teléfono debe tener entre 7 y 15 dígitos.")]
        public string Telefono { get; set; }

        [Required(ErrorMessage = "El mail es obligatorio.")]
        [EmailAddress(ErrorMessage = "El campo Mail no es una dirección de correo electrónico válida.")]
        [StringLength(200, ErrorMessage = "El email no puede superar los 200 caracteres.")]
        public string Mail { get; set; }

        public bool EsEmpresa { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // ---------------------------
            // ID del cliente
            // ---------------------------
            if (IdCliente <= 0)
            {
                yield return new ValidationResult(
                    "El ID del cliente debe ser mayor que 0.",
                    new[] { nameof(IdCliente) }
                );
            }

            // ---------------------------
            // EMPRESA
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
                        "El nombre es obligatorio para clientes comunes.",
                        new[] { nameof(Nombre) }
                    );
                }

                if (string.IsNullOrWhiteSpace(Apellido))
                {
                    yield return new ValidationResult(
                        "El apellido es obligatorio para clientes comunes.",
                        new[] { nameof(Apellido) }
                    );
                }

                if (string.IsNullOrWhiteSpace(Dni))
                {
                    yield return new ValidationResult(
                        "El DNI es obligatorio para clientes comunes.",
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
        }
    }
}
