using OfficeOpenXml;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
using Zenko.Models;
using Microsoft.AspNetCore.Http;

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

        public List<ProductoInsumoExcel> LeerProductoInsumos(List<IFormFile> archivosExcel)
        {
            var relaciones = new List<ProductoInsumoExcel>();

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
                    var varianteCodigo = worksheet.Cells[row, colIndices["VarianteCodigo"]].Value?.ToString();
                    var insumoCodigo = worksheet.Cells[row, colIndices["InsumoCodigo"]].Value?.ToString();

                    if (string.IsNullOrWhiteSpace(varianteCodigo) || string.IsNullOrWhiteSpace(insumoCodigo))
                    {
                        continue;
                    }

                    decimal cantidad = ParsearDecimalDesdeString(worksheet.Cells[row, colIndices["Cantidad"]].Value?.ToString());
                    if (cantidad < 0) continue;

                    relaciones.Add(new ProductoInsumoExcel
                    {
                        VarianteCodigo = varianteCodigo.Trim(),
                        VarianteNombre = worksheet.Cells[row, colIndices["VarianteNombre"]].Value?.ToString().Trim(),
                        ModeloCodigo = worksheet.Cells[row, colIndices["ModeloCodigo"]].Value?.ToString().Trim(),
                        ModeloNombre = worksheet.Cells[row, colIndices["ModeloNombre"]].Value?.ToString().Trim(),
                        InsumoCodigo = insumoCodigo.Trim(),
                        InsumoDescripcion = worksheet.Cells[row, colIndices["InsumoDescripcion"]].Value?.ToString().Trim(),
                        Cantidad = cantidad
                    });
                }
            }
            return relaciones;
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
