using Microsoft.AspNetCore.Mvc;
using Zenko.Models;
using Zenko.Services;
using Newtonsoft.Json;
using System.Threading.Tasks;      // Para Task<>
using Microsoft.AspNetCore.Http;   // Para IFormFile
using System.IO;                   // Para MemoryStream
using System.Collections.Generic;  // Para List<>

namespace Zenko.Controllers
{
    public class HomeController : Controller
    {
        private readonly ExcelService _excelService;
        private readonly CalculoService _calculoService;

        public HomeController(ExcelService excelService, CalculoService calculoService)
        {
            _excelService = excelService;
            _calculoService = calculoService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UploadExcel(IFormFile fileTelas, IFormFile fileAvios)
        {
            if (fileTelas != null && fileAvios != null)
            {
                using (var telasStream = new MemoryStream())
                using (var aviosStream = new MemoryStream())
                {
                    await fileTelas.CopyToAsync(telasStream);
                    await fileAvios.CopyToAsync(aviosStream);

                    telasStream.Position = 0;
                    aviosStream.Position = 0;

                    var telas = _excelService.LeerArchivoTelas(telasStream);
                    var avios = _excelService.LeerArchivoAvios(aviosStream);
                    var resultados = _calculoService.CalcularCostos(telas, avios);

                    TempData["Resultados"] = JsonConvert.SerializeObject(resultados);
                }

                return RedirectToAction("Resultados");
            }

            ModelState.AddModelError("", "Debés subir ambos archivos Excel.");
            return View("Index");
        }

        [HttpGet]
        public IActionResult Resultados()
        {
            // Ejemplo de datos para pasar a la vista
            var datos = new List<string> { "Elemento 1", "Elemento 2", "Elemento 3" };

            return View(datos); // Pasamos la lista como modelo
        }
    }
}
