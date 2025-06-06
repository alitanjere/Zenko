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
    }
}
