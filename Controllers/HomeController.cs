using Microsoft.AspNetCore.Mvc;
using Zenko.Models;
using Zenko.Services;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Collections.Generic;

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
        public async Task<IActionResult> UploadExcel(IFormFile excelFile)
        {
            if (excelFile == null)
            {
                ModelState.AddModelError("", "Debes subir un archivo Excel.");
                return View("Index");
            }

            List<TelaExcel> telas = null;
            List<AvioExcel> avios = null;

            using (var excelFileStream = new MemoryStream())
            {
                await excelFile.CopyToAsync(excelFileStream);
                excelFileStream.Position = 0; // Ensure stream is at the beginning for detection

                ExcelFileType fileType = _excelService.DetectFileType(excelFileStream);
                // DetectFileType should reset position if seekable, which it does.
                // If not, ensure excelFileStream.Position = 0; here before parsing.

                if (fileType == ExcelFileType.Tela)
                {
                    telas = _excelService.LeerArchivoTelas(excelFileStream);
                    // avios = new List<AvioExcel>(); // Initialize as empty or null
                }
                else if (fileType == ExcelFileType.Avio)
                {
                    avios = _excelService.LeerArchivoAvios(excelFileStream);
                    // telas = new List<TelaExcel>(); // Initialize as empty or null
                }
                else // ExcelFileType.Unknown
                {
                    // ModelState.AddModelError("", "No se pudo determinar el tipo de archivo Excel (Tela o Avío). Verifique el contenido del archivo.");
                    TempData["UploadErrorMessage"] = "No se pudo determinar el tipo de archivo Excel (Tela o Avío). Verifique el contenido del archivo.";
                    return RedirectToAction("Index"); // Redirect to show the message
                }

                // Proceed with database operations only if data was successfully parsed
                if ((telas != null && telas.Any()) || (avios != null && avios.Any()))
                {
                    int idTipoTela = BD.ObtenerIdTipoPorNombre("Tela");
                    int idTipoAvio = BD.ObtenerIdTipoPorNombre("Avio");

                    if (idTipoTela == 0 || idTipoAvio == 0)
                    {
                        // This error is more of a database setup issue
                        ModelState.AddModelError("", "Configuración de base de datos incompleta: No se encontraron los tipos de insumo 'Tela' o 'Avio'.");
                        TempData["UploadErrorMessage"] = "Error de configuración del sistema. Contacte al administrador.";
                        return RedirectToAction("Index");
                    }

                    if (telas != null)
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

                    if (avios != null)
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
                    TempData["UploadSuccessMessage"] = "Archivo procesado y datos guardados correctamente.";
                }
                else if (fileType != ExcelFileType.Unknown) // Parsed as Tela or Avio, but no items found (e.g. empty valid file)
                {
                    TempData["UploadWarningMessage"] = $"El archivo fue identificado como tipo '{fileType}' pero no contenía datos para procesar o los datos no eran válidos.";
                }
                // If fileType was Unknown, we've already redirected.

                // var resultados = _calculoService.CalcularCostos(telas, avios);
                // TempData["Resultados"] = JsonConvert.SerializeObject(resultados);
            }
            return RedirectToAction("Index");
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
