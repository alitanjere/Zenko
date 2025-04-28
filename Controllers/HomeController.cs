using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using System.IO;
using Microsoft.AspNetCore.Http;


namespace ZenkoApp.Controllers
{
    public class HomeController : Controller
    {
        static HomeController()
        {
            // Establecer la licencia gratuita de EPPlus (solo para uso no comercial)
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult UploadExcel(IFormFile file)
        {
            if (file != null && file.Length > 0)
            {
                using (var stream = new MemoryStream())
                {
                    file.CopyTo(stream);
                    using (var package = new ExcelPackage(stream))
                    {
                        var worksheet = package.Workbook.Worksheets[0];
                        var rowCount = worksheet.Dimension.Rows;

                        for (int row = 2; row <= rowCount; row++) // Starting from 2 to skip headers
                        {
                            var productName = worksheet.Cells[row, 1].Text;
                            var productPrice = worksheet.Cells[row, 2].Text;
                            var productCategory = worksheet.Cells[row, 3].Text;

                            // Aquí puedes guardar estos datos en la base de datos o procesarlos como desees.
                        }
                    }
                }
            }
            return RedirectToAction("Index"); // Redirige de vuelta al índice
        }
    }
}
