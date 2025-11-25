using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace venta_stock_webapi.Client.DTO
{
    public class ClientUpdateDTO : IValidatableObject
    {
        [Required(ErrorMessage = "El ID del cliente es obligatorio.")]
        public int IdCliente { get; set; }

        //Obligatorios solo para clientes comunes (EsEmpresa == false)
        public string Nombre { get; set; }

        public string Apellido { get; set; }

        //Obligatorios solo para empresas (EsEmpresa == true)
        public string RazonSocial { get; set; }
        public string Dni { get; set; }
        public string Cuit { get; set; }

        //Siempre obligatorios
        [Required(ErrorMessage = "El teléfono es obligatorio.")]
        public string Telefono { get; set; }

        [Required(ErrorMessage = "El mail es obligatorio.")]
        [EmailAddress(ErrorMessage = "El campo Mail no es una dirección de correo electrónico válida.")]
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
            }
        }
    }
}
