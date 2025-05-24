using Zenko.Models;
using System.Collections.Generic;
using System.Linq;

namespace Zenko.Services
{
    public class CalculoService
    {
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
    }
}
