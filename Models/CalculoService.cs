using Zenko.Models;
using System.Collections.Generic;
using System.Linq;

namespace Zenko.Services
{
    public class CalculoService
    {
        // Unidades base utilizadas en la aplicación
        public const string UnidadBaseTela = "m";       // metros
        public const string UnidadBaseAvio = "unidad"; // unidades

        public ResultadoViewModel CalcularCostos(List<TelaExcel> telas, List<AvioExcel> avios)
        {
            var resultado = new ResultadoViewModel();

            resultado.TelasDetalle = telas ?? new List<TelaExcel>();
            resultado.AviosDetalle = avios ?? new List<AvioExcel>();

            resultado.CostoTotalTelas = resultado.TelasDetalle?.Sum(t => t.CostoPorMetro) ?? 0m;
            resultado.CostoTotalAvios = resultado.AviosDetalle?.Sum(a => a.CostoUnidad) ?? 0m;
            resultado.CostoTotalGeneral = resultado.CostoTotalTelas + resultado.CostoTotalAvios;

            if (!resultado.TelasDetalle.Any() && !resultado.AviosDetalle.Any())
            {
                resultado.MensajeError = "No se proporcionaron datos de telas ni de avíos para calcular.";
            }

            return resultado;
        }

        /// <summary>
        /// Convierte un valor desde una unidad de origen a una unidad de destino base.
        /// Se soportan algunas unidades básicas de longitud y cantidad.
        /// </summary>
        /// <param name="valor">Valor a convertir.</param>
        /// <param name="unidadOrigen">Unidad en la que está expresado el valor.</param>
        /// <param name="unidadBase">Unidad a la que se desea convertir.</param>
        /// <returns>Valor convertido a la unidad base especificada.</returns>
        public decimal ConvertirUnidad(decimal valor, string unidadOrigen, string unidadBase)
        {
            if (valor < 0)
                return 0m;

            string origen = NormalizarUnidad(unidadOrigen);
            string destino = NormalizarUnidad(unidadBase);

            decimal factorOrigen = ObtenerFactor(origen);
            decimal factorDestino = ObtenerFactor(destino);

            if (factorOrigen <= 0 || factorDestino <= 0)
                return valor; // No se puede convertir, se devuelve el valor original

            return valor * (factorOrigen / factorDestino);
        }

        private string NormalizarUnidad(string unidad)
        {
            return (unidad ?? string.Empty).Trim().ToLowerInvariant();
        }

        private decimal ObtenerFactor(string unidad)
        {
            switch (unidad)
            {
                // Longitud
                case "m":
                case "metro":
                case "metros":
                case "mt":
                case "mts":
                    return 1m;
                case "cm":
                case "centimetro":
                case "centimetros":
                    return 0.01m;
                case "mm":
                case "milimetro":
                case "milimetros":
                    return 0.001m;

                // Cantidad
                case "unidad":
                case "unidades":
                case "u":
                    return 1m;
                case "par":
                case "pares":
                    return 2m;
                case "docena":
                case "docenas":
                case "dz":
                    return 12m;
                default:
                    return -1m; // Unidad desconocida
            }
        }
    }
}
