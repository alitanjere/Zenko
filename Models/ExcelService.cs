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

                for (int row = 2; ; row++)
                {
                    var codigoProducto = worksheet.Cells[row, 1].Value?.ToString();
                    var nombreProducto = worksheet.Cells[row, 2].Value?.ToString();
                    var codigoInsumo = worksheet.Cells[row, 3].Value?.ToString();
                    var cantidadStr = worksheet.Cells[row, 4].Value?.ToString();

                    if (string.IsNullOrWhiteSpace(codigoProducto) ||
                        string.IsNullOrWhiteSpace(nombreProducto) ||
                        string.IsNullOrWhiteSpace(codigoInsumo) ||
                        string.IsNullOrWhiteSpace(cantidadStr))
                    {
                        break;
                    }

                    decimal cantidad = ParsearDecimalDesdeString(cantidadStr);
                    if (cantidad < 0) continue;

                    relaciones.Add(new ProductoInsumoExcel
                    {
                        CodigoProducto = codigoProducto.Trim(),
                        NombreProducto = nombreProducto.Trim(),
                        CodigoInsumo = codigoInsumo.Trim(),
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
                var worksheet = package.Workbook.Worksheets.Add("Reporte Final");

                // Encabezados
                worksheet.Cells[1, 1].Value = "Producto";
                worksheet.Cells[1, 2].Value = "Código Producto";
                worksheet.Cells[1, 3].Value = "Código Insumo";
                worksheet.Cells[1, 4].Value = "Costo Insumo";
                worksheet.Cells[1, 5].Value = "Cantidad";
                worksheet.Cells[1, 6].Value = "Costo Total";

                // Datos
                int row = 2;
                foreach (var item in reporte)
                {
                    worksheet.Cells[row, 1].Value = item.NombreProducto;
                    worksheet.Cells[row, 2].Value = item.CodigoProducto;
                    worksheet.Cells[row, 3].Value = item.CodigoInsumo;
                    worksheet.Cells[row, 4].Value = item.CostoInsumo;
                    worksheet.Cells[row, 5].Value = item.Cantidad;
                    worksheet.Cells[row, 6].Value = item.CostoTotal;
                    row++;
                }

                worksheet.Cells.AutoFitColumns();

                return package.GetAsByteArray();
            }
        }
    }
}
