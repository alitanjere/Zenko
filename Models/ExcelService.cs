using OfficeOpenXml;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
using System;
using Zenko.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Dapper;
using System.Data;
using System.Threading.Tasks;

namespace Zenko.Services
{
    public class ExcelService
    {
        static ExcelService()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        // Método unificado para leer un único stream y separar telas y avíos
        private (List<TelaExcel> telas, List<AvioExcel> avios) LeerArchivoUnificado(Stream stream)
        {
            var telas = new List<TelaExcel>();
            var avios = new List<AvioExcel>();

            if (stream == null) return (telas, avios);

            using (var package = new ExcelPackage(stream))
            {
                var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                if (worksheet == null) return (telas, avios);

                for (int row = 2; ; row++)
                {
                    var codigo = worksheet.Cells[row, 1].Value?.ToString();
                    if (string.IsNullOrWhiteSpace(codigo)) break;

                    var costoStr = worksheet.Cells[row, 2].Value?.ToString();
                    decimal costo = ParsearDecimalDesdeString(costoStr);
                    if (costo < 0) continue;

                    if (EsCodigoTela(codigo))
                    {
                        telas.Add(new TelaExcel
                        {
                            Codigo = codigo,
                            CostoPorMetro = costo
                        });
                    }
                    else if (EsCodigoAvio(codigo))
                    {
                        avios.Add(new AvioExcel
                        {
                            Codigo = codigo,
                            CostoUnidad = costo
                        });
                    }
                }
            }

            return (telas, avios);
        }

        // Método público que recibe lista de archivos, procesa cada uno y une resultados
        public (List<TelaExcel> telas, List<AvioExcel> avios) LeerArchivos(List<IFormFile> archivosExcel)
{
    var telas = new List<TelaExcel>();
    var avios = new List<AvioExcel>();

    foreach (var archivo in archivosExcel)
    {
        using var stream = new MemoryStream();
        archivo.CopyTo(stream);
        stream.Position = 0;

        using var package = new ExcelPackage(stream);
        var worksheet = package.Workbook.Worksheets.FirstOrDefault();
        if (worksheet == null) continue;

        for (int row = 2; ; row++)
        {
            var codigo = worksheet.Cells[row, 1].Value?.ToString();
            if (string.IsNullOrWhiteSpace(codigo)) break;

            var costoStr = worksheet.Cells[row, 2].Value?.ToString();
            decimal costo = ParsearDecimalDesdeString(costoStr);

            if (costo < 0) continue;

            if (EsCodigoTela(codigo))
            {
                telas.Add(new TelaExcel
                {
                    Codigo = codigo,
                    CostoPorMetro = costo
                });
            }
            else if (EsCodigoAvio(codigo))
            {
                avios.Add(new AvioExcel
                {
                    Codigo = codigo,
                    CostoUnidad = costo
                });
            }
            // Si no es ninguno, simplemente se ignora esa fila
        }
    }

    return (telas, avios);
}

        private decimal ParsearDecimalDesdeString(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return -1;

            string limpio = valor.Replace("$", "").Trim();
            limpio = limpio.Replace(".", "").Replace(",", ".");

            if (decimal.TryParse(limpio, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal resultado))
                return resultado;

            return -1;
        }

       private bool EsCodigoTela(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo) || codigo.Length < 4) return false;

            codigo = codigo.Trim().ToUpperInvariant();

            // Primer letra: V o I
            if (!(codigo.StartsWith("V") || codigo.StartsWith("I")))
                return false;

            // Letra en la posición 4 (índice 3)
            char tipo = codigo[3];

            return tipo == 'K' || tipo == 'M';
        }

        private bool EsCodigoAvio(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo) || codigo.Length < 4) return false;

            codigo = codigo.Trim().ToUpperInvariant();

            if (!(codigo.StartsWith("V") || codigo.StartsWith("I")))
                return false;

            char tipo = codigo[3];

            return tipo == 'A';
        }

        public (List<ProductoInsumoExcel> relaciones, Dictionary<string, string> todasLasVariantes) LeerProductoInsumos(List<IFormFile> archivosExcel)
        {
            var relaciones = new List<ProductoInsumoExcel>();
            var todasLasVariantes = new Dictionary<string, string>();

            foreach (var archivo in archivosExcel)
            {
                using var stream = new MemoryStream();
                archivo.CopyTo(stream);
                stream.Position = 0;

                using var package = new ExcelPackage(stream);
                var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                if (worksheet == null) continue;

                var headers = worksheet.Cells[1, 1, 1, worksheet.Dimension.End.Column]
                    .Select(cell => cell.Value?.ToString().Trim().ToUpper() ?? "")
                    .ToList();

                var requiredHeaders = new Dictionary<string, string>
                {
                    { "VarianteCodigo", "VARIANTE_CODIGO" },
                    { "VarianteNombre", "VARIANTE" },
                    { "ModeloCodigo", "MODELO" },
                    { "ModeloNombre", "MODELO_NOMBRE" },
                    { "InsumoCodigo", "INSUMO" },
                    { "InsumoDescripcion", "INSUMO_DESC" },
                    { "Cantidad", "CANTIDAD" }
                };

                var colIndices = new Dictionary<string, int>();
                foreach (var header in requiredHeaders)
                {
                    int index = headers.IndexOf(header.Value);
                    if (index == -1)
                    {
                        throw new Exception($"No se pudo encontrar la columna requerida '{header.Value}' en el archivo Excel.");
                    }
                    colIndices[header.Key] = index + 1;
                }

                for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                {
                    var varianteCodigo = worksheet.Cells[row, colIndices["VarianteCodigo"]].Value?.ToString()?.Trim();
                    var modeloCodigo = worksheet.Cells[row, colIndices["ModeloCodigo"]].Value?.ToString()?.Trim();

                    if (!string.IsNullOrWhiteSpace(varianteCodigo) && !string.IsNullOrWhiteSpace(modeloCodigo))
                    {
                        if (!todasLasVariantes.ContainsKey(varianteCodigo))
                        {
                            todasLasVariantes.Add(varianteCodigo, modeloCodigo);
                        }
                    }

                    var insumoCodigo = worksheet.Cells[row, colIndices["InsumoCodigo"]].Value?.ToString();

                    if (string.IsNullOrWhiteSpace(varianteCodigo) || string.IsNullOrWhiteSpace(insumoCodigo))
                    {
                        continue;
                    }

                    decimal cantidad = ParsearDecimalDesdeString(worksheet.Cells[row, colIndices["Cantidad"]].Value?.ToString());
                    if (cantidad < 0) continue;

                    relaciones.Add(new ProductoInsumoExcel
                    {
                        VarianteCodigo = varianteCodigo,
                        VarianteNombre = worksheet.Cells[row, colIndices["VarianteNombre"]].Value?.ToString().Trim(),
                        ModeloCodigo = modeloCodigo,
                        ModeloNombre = worksheet.Cells[row, colIndices["ModeloNombre"]].Value?.ToString().Trim(),
                        InsumoCodigo = insumoCodigo.Trim(),
                        InsumoDescripcion = worksheet.Cells[row, colIndices["InsumoDescripcion"]].Value?.ToString().Trim(),
                        Cantidad = cantidad
                    });
                }
            }
            return (relaciones, todasLasVariantes);
        }

        public async Task<(List<TelaExcel> telas, List<AvioExcel> avios)> ProcesarArchivos(List<IFormFile> archivosExcel, string connectionString, int usuarioId)
        {
            var (telas, avios) = LeerArchivos(archivosExcel);

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            var existentes = (await connection.QueryAsync<Insumo>("SELECT CodigoInsumo, Costo FROM Insumos"))
                .ToDictionary(i => i.CodigoInsumo, i => i.Costo);

            int altas = 0;
            int modificaciones = 0;
            var procesados = new HashSet<string>();

            foreach (var tela in telas)
            {
                procesados.Add(tela.Codigo);
                int idTipo = await ObtenerOInsertarTipoInsumo(connection, tela.Codigo);
                if (existentes.TryGetValue(tela.Codigo, out decimal costoPrevio))
                {
                    if (costoPrevio != tela.CostoPorMetro) modificaciones++;
                    existentes.Remove(tela.Codigo);
                }
                else
                {
                    altas++;
                }
                await InsertarInsumo(connection, new Insumo
                {
                    CodigoInsumo = tela.Codigo,
                    IdTipoInsumo = idTipo,
                    Costo = tela.CostoPorMetro,
                    FechaRegistro = DateTime.Now
                });
            }

            foreach (var avio in avios)
            {
                procesados.Add(avio.Codigo);
                int idTipo = await ObtenerOInsertarTipoInsumo(connection, avio.Codigo);
                if (existentes.TryGetValue(avio.Codigo, out decimal costoPrevio))
                {
                    if (costoPrevio != avio.CostoUnidad) modificaciones++;
                    existentes.Remove(avio.Codigo);
                }
                else
                {
                    altas++;
                }
                await InsertarInsumo(connection, new Insumo
                {
                    CodigoInsumo = avio.Codigo,
                    IdTipoInsumo = idTipo,
                    Costo = avio.CostoUnidad,
                    FechaRegistro = DateTime.Now
                });
            }

            int bajas = existentes.Count;
            if (bajas > 0)
            {
                var codigosBaja = existentes.Keys.ToList();
                await connection.ExecuteAsync("DELETE FROM Insumos WHERE CodigoInsumo IN @Codigos", new { Codigos = codigosBaja });
            }

            string archivos = string.Join(", ", archivosExcel.Select(a => a.FileName));
            string resumen = $"Altas: {altas}, Bajas: {bajas}, Modificaciones: {modificaciones}";
            await connection.ExecuteAsync(
                "INSERT INTO Auditorias (Archivo, UsuarioId, Fecha, ResumenCambios) VALUES (@Archivo, @UsuarioId, @Fecha, @Resumen)",
                new { Archivo = archivos, UsuarioId = usuarioId, Fecha = DateTime.Now, Resumen = resumen });

            return (telas, avios);
        }

        private async Task<int> ObtenerOInsertarTipoInsumo(SqlConnection connection, string codigoInsumo)
        {
            using var command = new SqlCommand("dbo.ObtenerOInsertarTipoInsumoPorCodigo", connection);
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@CodigoInsumo", codigoInsumo);
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        private async Task InsertarInsumo(SqlConnection connection, Insumo insumo)
        {
            using var command = new SqlCommand("dbo.InsertarInsumo", connection);
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@CodigoInsumo", insumo.CodigoInsumo);
            command.Parameters.AddWithValue("@IdTipoInsumo", insumo.IdTipoInsumo);
            command.Parameters.AddWithValue("@Costo", insumo.Costo);
            command.Parameters.AddWithValue("@FechaRegistro", insumo.FechaRegistro);
            await command.ExecuteNonQueryAsync();
        }

        public byte[] CrearExcelReporteFinal(List<ReporteFinalViewModel> reporte)
        {
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Reporte Final de Costos");

                // Encabezados
                worksheet.Cells[1, 1].Value = "Código Variante";
                worksheet.Cells[1, 2].Value = "Nombre Variante";
                worksheet.Cells[1, 3].Value = "Código Modelo";
                worksheet.Cells[1, 4].Value = "Nombre Modelo";
                worksheet.Cells[1, 5].Value = "Código Insumo";
                worksheet.Cells[1, 6].Value = "Descripción Insumo";
                worksheet.Cells[1, 7].Value = "Costo Insumo";
                worksheet.Cells[1, 8].Value = "Cantidad";
                worksheet.Cells[1, 9].Value = "Costo Total";

                // Datos
                int row = 2;
                foreach (var item in reporte)
                {
                    worksheet.Cells[row, 1].Value = item.VarianteCodigo;
                    worksheet.Cells[row, 2].Value = item.VarianteNombre;
                    worksheet.Cells[row, 3].Value = item.ModeloCodigo;
                    worksheet.Cells[row, 4].Value = item.ModeloNombre;
                    worksheet.Cells[row, 5].Value = item.CodigoInsumo;
                    worksheet.Cells[row, 6].Value = item.InsumoDescripcion;
                    worksheet.Cells[row, 7].Value = item.CostoInsumo;
                    worksheet.Cells[row, 8].Value = item.Cantidad;
                    worksheet.Cells[row, 9].Value = item.CostoTotal;
                    row++;
                }

                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                return package.GetAsByteArray();
            }
        }
    }
}
