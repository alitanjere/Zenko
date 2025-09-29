using System;
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

        public ConsumoResultadoViewModel CalcularConsumosPromedio(List<ConsumoExcel> registros)
        {
            var resultado = new ConsumoResultadoViewModel();

            if (registros == null || registros.Count == 0)
            {
                return resultado;
            }

            resultado.RegistrosProcesados = registros.Count;

            resultado.ConsumosPorOperacion = registros
                .Where(r => !string.IsNullOrWhiteSpace(r.OpNumero) && !string.IsNullOrWhiteSpace(r.InsumoCodigo))
                .GroupBy(r => new { r.OpNumero, r.InsumoCodigo, Costo = r.CostoInsumoOperacion })
                .Select(g => new ConsumoPromedioPorOperacion
                {
                    OpNumero = g.Key.OpNumero,
                    InsumoCodigo = g.Key.InsumoCodigo,
                    InsumoDescripcion = g.Select(x => x.InsumoDescripcion).FirstOrDefault(d => !string.IsNullOrWhiteSpace(d)),
                    CostoInsumoOperacion = g.Key.Costo,
                    ConsumoPromedio = CalcularPromedio(g.Select(x => x.ConsumoPromedio)),
                    ConsumoReal = CalcularPromedio(g.Select(x => x.ConsumoRealOperacion)),
                    CantidadRemitidaTotal = g.Where(x => x.CantidadRemitida.HasValue).Sum(x => x.CantidadRemitida ?? 0m),
                    DevolucionTotal = g.Where(x => x.Devolucion.HasValue).Sum(x => x.Devolucion ?? 0m),
                    ModeloCodigo = g.Select(x => x.ModeloCodigo).FirstOrDefault(m => !string.IsNullOrWhiteSpace(m)),
                    ModeloNombre = g.Select(x => x.ModeloNombre).FirstOrDefault(m => !string.IsNullOrWhiteSpace(m))
                })
                .OrderBy(r => r.OpNumero)
                .ThenBy(r => r.InsumoCodigo)
                .ToList();

            resultado.ConsumosPorProducto = registros
                .Where(r => !string.IsNullOrWhiteSpace(r.ModeloCodigo) && !string.IsNullOrWhiteSpace(r.InsumoCodigo))
                .GroupBy(r => new { r.ModeloCodigo, r.ModeloNombre, r.InsumoCodigo })
                .Select(g => new ConsumoPromedioPorProducto
                {
                    ModeloCodigo = g.Key.ModeloCodigo,
                    ModeloNombre = g.Key.ModeloNombre,
                    InsumoCodigo = g.Key.InsumoCodigo,
                    InsumoDescripcion = g.Select(x => x.InsumoDescripcion).FirstOrDefault(d => !string.IsNullOrWhiteSpace(d)),
                    ConsumoPromedio = CalcularPromedio(g.Select(x => x.ConsumoPromedio)),
                    ConsumoReal = CalcularPromedio(g.Select(x => x.ConsumoRealOperacion)),
                    CantidadRemitidaTotal = g.Where(x => x.CantidadRemitida.HasValue).Sum(x => x.CantidadRemitida ?? 0m),
                    DevolucionTotal = g.Where(x => x.Devolucion.HasValue).Sum(x => x.Devolucion ?? 0m),
                    OperacionesDistintas = g.Select(x => x.OpNumero).Where(op => !string.IsNullOrWhiteSpace(op)).Distinct().Count()
                })
                .OrderBy(r => r.ModeloCodigo)
                .ThenBy(r => r.InsumoCodigo)
                .ToList();

            return resultado;
        }

        private decimal? CalcularPromedio(IEnumerable<decimal?> valores)
        {
            var filtrados = valores
                .Where(v => v.HasValue)
                .Select(v => v.Value)
                .ToList();

            if (!filtrados.Any())
            {
                return null;
            }

            return decimal.Round(filtrados.Average(), 4, System.MidpointRounding.AwayFromZero);
        }
    }
}
