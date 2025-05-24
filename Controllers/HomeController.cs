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

                    // --- Inicio: Integración con BD ---
                    int idTipoTela = BD.ObtenerIdTipoPorNombre("Tela");
                    int idTipoAvio = BD.ObtenerIdTipoPorNombre("Avio");

                    if (idTipoTela != 0 && telas != null)
                    {
                        foreach (var telaExcel in telas)
                        {
                            var nuevoInsumo = new Insumo
                            {
                                CodigoInsumo = telaExcel.Codigo,
                                IdTipoInsumo = idTipoTela,
                                Costo = telaExcel.CostoPorMetro,
                                FechaRegistro = System.DateTime.Now
                            };
                            BD.InsertarInsumo(nuevoInsumo);
                        }
                    }

                    if (idTipoAvio != 0 && avios != null)
                    {
                        foreach (var avioExcel in avios)
                        {
                            var nuevoInsumo = new Insumo
                            {
                                CodigoInsumo = avioExcel.Codigo,
                                IdTipoInsumo = idTipoAvio,
                                Costo = avioExcel.CostoUnidad,
                                FechaRegistro = System.DateTime.Now
                            };
                            BD.InsertarInsumo(nuevoInsumo);
                        }
                    }
                    // --- Fin: Integración con BD ---

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
            if (TempData["Resultados"] is string jsonResultados && !string.IsNullOrEmpty(jsonResultados))
            {
                var modelo = JsonConvert.DeserializeObject<ResultadoViewModel>(jsonResultados);
                return View(modelo);
            }
            else
            {
                var modeloVacio = new ResultadoViewModel
                {
                    MensajeError = "No se encontraron resultados para mostrar. Por favor, carga los archivos primero."
                };
                return View(modeloVacio);
            }
        }
    }
}
