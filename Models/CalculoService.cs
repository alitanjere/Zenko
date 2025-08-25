using Zenko.Models;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System;

namespace Zenko.Services
{
    public class CalculoService
    {
        private readonly string _connectionString;

        public CalculoService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

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

        public List<HistorialPrecio> ObtenerHistorial(int insumoId)
        {
            var historial = new List<HistorialPrecio>();
            using (var conexion = new SqlConnection(_connectionString))
            {
                conexion.Open();
                var comando = new SqlCommand("SELECT FechaCarga, DatosJson FROM Resultados_Calculos ORDER BY FechaCarga", conexion);
                var reader = comando.ExecuteReader();
                while (reader.Read())
                {
                    var fecha = reader.GetDateTime(0);
                    var json = reader.GetString(1);
                    var resultado = JsonConvert.DeserializeObject<ResultadoViewModel>(json);
                    var codigo = insumoId.ToString();

                    var tela = resultado.TelasDetalle?.FirstOrDefault(t => t.Codigo == codigo);
                    if (tela != null)
                    {
                        historial.Add(new HistorialPrecio { Fecha = fecha, Precio = tela.CostoPorMetro });
                        continue;
                    }

                    var avio = resultado.AviosDetalle?.FirstOrDefault(a => a.Codigo == codigo);
                    if (avio != null)
                    {
                        historial.Add(new HistorialPrecio { Fecha = fecha, Precio = avio.CostoUnidad });
                    }
                }
            }
            return historial.OrderBy(h => h.Fecha).ToList();
        }
    }
}
