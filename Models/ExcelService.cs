using OfficeOpenXml;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
using Zenko.Models;

namespace Zenko.Services
{
    public enum ExcelFileType
    {
        Unknown,
        Tela,
        Avio
    }

    public class ExcelService
    {
        static ExcelService()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public List<TelaExcel> LeerArchivoTelas(Stream stream)
        {
            var telas = new List<TelaExcel>();
            if (stream == null) return telas;

            using (var package = new ExcelPackage(stream))
            {                var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                if (worksheet == null) return telas;

                for (int row = 2; ; row++)
                {
                    var codigo = worksheet.Cells[row, 1].Value?.ToString();
                    if (string.IsNullOrWhiteSpace(codigo)) break;

                    if (!EsCodigoTela(codigo)) continue;

                    var costoStr = worksheet.Cells[row, 2].Value?.ToString();
                    decimal costoPorMetro = ParsearDecimalDesdeString(costoStr);

                    if (costoPorMetro >= 0)
                    {
                        telas.Add(new TelaExcel
                        {
                            Codigo = codigo,
                            CostoPorMetro = costoPorMetro
                        });
                    }
                }
            }
            return telas;
        }

        public List<AvioExcel> LeerArchivoAvios(Stream stream)
        {
            var avios = new List<AvioExcel>();
            if (stream == null) return avios;

            using (var package = new ExcelPackage(stream))
            {
                var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                if (worksheet == null) return avios;

                for (int row = 2; ; row++)
                {
                    var codigo = worksheet.Cells[row, 1].Value?.ToString();
                    if (string.IsNullOrWhiteSpace(codigo)) break;

                    if (!EsCodigoAvio(codigo)) continue;

                    var costoStr = worksheet.Cells[row, 2].Value?.ToString();
                    decimal costoUnidad = ParsearDecimalDesdeString(costoStr);

                    if (costoUnidad >= 0)
                    {
                        avios.Add(new AvioExcel
                        {
                            Codigo = codigo,
                            CostoUnidad = costoUnidad
                        });
                    }
                }
            }
            return avios;
        }

        private decimal ParsearDecimalDesdeString(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return -1;

            // Quitar $ y espacios
            string limpio = valor.Replace("$", "").Trim();

            // Quitar puntos de miles y cambiar coma decimal a punto
            limpio = limpio.Replace(".", "").Replace(",", ".");

            if (decimal.TryParse(limpio, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal resultado))
                return resultado;

            return -1; // Indicamos error en parseo
        }
        private bool EsCodigoTela(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo) || codigo.Length < 5) return false;
            string prefijo = codigo.Substring(0, 3); // por ejemplo, V23, I18, etc.
            char tipo = codigo[4]; // letra en posición 5

            return (codigo.StartsWith("V") || codigo.StartsWith("I")) && (tipo == 'K' || tipo == 'M');
        }

        private bool EsCodigoAvio(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo) || codigo.Length < 5) return false;
            string prefijo = codigo.Substring(0, 3);
            char tipo = codigo[4]; // letra en posición 5

            return (codigo.StartsWith("V") || codigo.StartsWith("I")) && tipo == 'A';
        }

        public ExcelFileType DetectFileType(Stream stream)
        {
            if (stream == null || stream.Length == 0)
            {
                return ExcelFileType.Unknown;
            }

            // Ensure the stream is at the beginning if it's seekable
            if (stream.CanSeek)
            {
                stream.Position = 0;
            }

            using (var package = new ExcelPackage(stream))
            {
                var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                if (worksheet == null)
                {
                    return ExcelFileType.Unknown;
                }

                // Check a few rows for codes (e.g., up to 5 data rows, starting from row 2)
                int telasCount = 0;
                int aviosCount = 0;
                int rowsToCheck = 5; // Number of data rows to inspect

                for (int row = 2; row < 2 + rowsToCheck; row++)
                {
                    var codigoCell = worksheet.Cells[row, 1].Value;
                    if (codigoCell == null || string.IsNullOrWhiteSpace(codigoCell.ToString()))
                    {
                        // If we hit an empty cell in the first column, assume end of relevant data for detection
                        break;
                    }
                    string codigo = codigoCell.ToString().Trim();

                    if (EsCodigoTela(codigo))
                    {
                        telasCount++;
                    }
                    if (EsCodigoAvio(codigo)) // Using 'if' instead of 'else if' in case a code could ambiguously match both (though unlikely with current logic)
                    {
                        aviosCount++;
                    }
                }

                // Restore stream position if it was seekable, so it can be read again by other methods
                if (stream.CanSeek)
                {
                    stream.Position = 0;
                }

                // Determine file type based on counts
                if (telasCount > 0 && aviosCount == 0)
                {
                    return ExcelFileType.Tela;
                }
                if (aviosCount > 0 && telasCount == 0)
                {
                    return ExcelFileType.Avio;
                }
                // If both are > 0, or both are 0 after checking rows, it's ambiguous or not identifiable by this logic
                return ExcelFileType.Unknown;
            }
        }
    }
}


