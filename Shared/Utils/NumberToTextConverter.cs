using System.Text;

namespace venta_stock_webapi.Shared.Utils
{
    /// <summary>
    /// Convierte números a texto en español
    /// Ejemplo: 1850000 -> "Un millón ochocientos cincuenta mil pesos"
    /// </summary>
    public static class NumberToTextConverter
    {
        private static readonly string[] unidades = {
            "", "uno", "dos", "tres", "cuatro", "cinco", "seis", "siete", "ocho", "nueve"
        };

        private static readonly string[] decenas = {
            "", "diez", "veinte", "treinta", "cuarenta", "cincuenta",
            "sesenta", "setenta", "ochenta", "noventa"
        };

        private static readonly string[] especiales = {
            "diez", "once", "doce", "trece", "catorce", "quince",
            "dieciséis", "diecisiete", "dieciocho", "diecinueve"
        };

        private static readonly string[] centenas = {
            "", "ciento", "doscientos", "trescientos", "cuatrocientos", "quinientos",
            "seiscientos", "setecientos", "ochocientos", "novecientos"
        };

        /// <summary>
        /// Convierte un número decimal a texto en español con la moneda
        /// </summary>
        /// <param name="numero">El número a convertir</param>
        /// <param name="moneda">La moneda (por defecto "pesos")</param>
        /// <returns>El número en texto</returns>
        public static string ConvertirATexto(decimal numero, string moneda = "pesos")
        {
            if (numero == 0)
                return $"Cero {moneda}";

            // Separar parte entera y decimales
            long parteEntera = (long)Math.Floor(numero);
            int centavos = (int)Math.Round((numero - parteEntera) * 100);

            var resultado = new StringBuilder();

            // Convertir parte entera
            string textoEntero = ConvertirEnteroATexto(parteEntera);

            // Capitalizar primera letra
            if (!string.IsNullOrEmpty(textoEntero))
            {
                textoEntero = char.ToUpper(textoEntero[0]) + textoEntero.Substring(1);
            }

            resultado.Append(textoEntero);
            resultado.Append($" {moneda}");

            // Agregar centavos si existen
            if (centavos > 0)
            {
                resultado.Append($" con {centavos}/100");
            }

            return resultado.ToString();
        }

        /// <summary>
        /// Convierte la parte entera del número a texto
        /// </summary>
        private static string ConvertirEnteroATexto(long numero)
        {
            if (numero == 0)
                return "cero";

            if (numero < 0)
                return "menos " + ConvertirEnteroATexto(-numero);

            var partes = new List<string>();

            // Billones
            if (numero >= 1_000_000_000_000)
            {
                long billones = numero / 1_000_000_000_000;
                partes.Add(ConvertirGrupo((int)billones) + (billones == 1 ? " billón" : " billones"));
                numero %= 1_000_000_000_000;
            }

            // Miles de millones
            if (numero >= 1_000_000_000)
            {
                long milesMillones = numero / 1_000_000_000;
                partes.Add(ConvertirGrupo((int)milesMillones) + " mil millones");
                numero %= 1_000_000_000;
            }

            // Millones
            if (numero >= 1_000_000)
            {
                int millones = (int)(numero / 1_000_000);
                if (millones == 1)
                    partes.Add("un millón");
                else
                    partes.Add(ConvertirGrupo(millones) + " millones");
                numero %= 1_000_000;
            }

            // Miles
            if (numero >= 1_000)
            {
                int miles = (int)(numero / 1_000);
                if (miles == 1)
                    partes.Add("mil");
                else
                    partes.Add(ConvertirGrupo(miles) + " mil");
                numero %= 1_000;
            }

            // Centenas, decenas y unidades
            if (numero > 0)
            {
                partes.Add(ConvertirGrupo((int)numero));
            }

            return string.Join(" ", partes);
        }

        /// <summary>
        /// Convierte un grupo de tres dígitos (centenas, decenas, unidades) a texto
        /// </summary>
        private static string ConvertirGrupo(int numero)
        {
            if (numero == 0)
                return "";

            var resultado = new StringBuilder();

            // Centenas
            int cent = numero / 100;
            int resto = numero % 100;

            if (cent > 0)
            {
                if (numero == 100)
                    resultado.Append("cien");
                else
                    resultado.Append(centenas[cent]);

                if (resto > 0)
                    resultado.Append(" ");
            }

            // Decenas y unidades
            if (resto >= 10 && resto < 20)
            {
                // Casos especiales: 10-19
                resultado.Append(especiales[resto - 10]);
            }
            else
            {
                int dec = resto / 10;
                int uni = resto % 10;

                if (dec > 0)
                {
                    resultado.Append(decenas[dec]);

                    if (uni > 0)
                    {
                        if (dec == 2)
                            resultado.Append("i"); // veinti...
                        else
                            resultado.Append(" y ");
                    }
                }

                if (uni > 0)
                {
                    resultado.Append(unidades[uni]);
                }
            }

            return resultado.ToString();
        }

        /// <summary>
        /// Convierte un número a formato de moneda con texto
        /// Ejemplo: 1850000 -> "$1.850.000,00 (Un millón ochocientos cincuenta mil pesos)"
        /// </summary>
        public static string ConvertirAMonedaConTexto(decimal numero, string moneda = "pesos")
        {
            string numeroFormateado = numero.ToString("N2");
            string textoNumero = ConvertirATexto(numero, moneda);
            return $"${numeroFormateado} ({textoNumero})";
        }
    }
}
