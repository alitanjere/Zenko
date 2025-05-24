using OfficeOpenXml;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Zenko.Models;

namespace Zenko.Services
{
    public class ExcelService
    {
        static ExcelService()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public List<TelaExcel> LeerArchivoTelas(System.IO.Stream stream)
        {
            var telas = new List<TelaExcel>();
            if (stream == null) return telas;

            using (var package = new ExcelPackage(stream))
            {
                var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                if (worksheet == null) return telas;

                for (int row = 2; ; row++)
                {
                    var codigo = worksheet.Cells[row, 1].Value?.ToString();
                    if (string.IsNullOrWhiteSpace(codigo)) break;

                    var descripcion = worksheet.Cells[row, 2].Value?.ToString();
                    var costoPorMetroStr = worksheet.Cells[row, 3].Value?.ToString();
                    var proveedor = worksheet.Cells[row, 4].Value?.ToString();

                    if (decimal.TryParse(costoPorMetroStr, out decimal costoPorMetro))
                    {
                        telas.Add(new TelaExcel
                        {
                            Codigo = codigo,
                            Descripcion = descripcion,
                            CostoPorMetro = costoPorMetro,
                            Proveedor = proveedor
                        });
                    }
                    // else: Omitir fila si CostoPorMetro no es un decimal válido
                }
            }
            return telas;
        }

        public List<AvioExcel> LeerArchivoAvios(System.IO.Stream stream)
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

                    var descripcion = worksheet.Cells[row, 2].Value?.ToString();
                    var costoUnidadStr = worksheet.Cells[row, 3].Value?.ToString();
                    var unidadMedida = worksheet.Cells[row, 4].Value?.ToString();
                    var proveedor = worksheet.Cells[row, 5].Value?.ToString();

                    if (decimal.TryParse(costoUnidadStr, out decimal costoUnidad))
                    {
                        avios.Add(new AvioExcel
                        {
                            Codigo = codigo,
                            Descripcion = descripcion,
                            CostoUnidad = costoUnidad,
                            UnidadMedida = unidadMedida,
                            Proveedor = proveedor
                        });
                    }
                    // else: Omitir fila si CostoUnidad no es un decimal válido
                }
            }
            return avios;
        }
    }
}
