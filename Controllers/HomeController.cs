using Microsoft.AspNetCore.Mvc;
using Zenko.Models;
using Zenko.Services;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using OfficeOpenXml; // Para EPPlus

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
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial; // Asegurar licencia para EPPlus
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UploadExcel(List<IFormFile> files)
        {
            if (files == null || files.Count != 2)
            {
                ModelState.AddModelError("", "Debes seleccionar exactamente dos archivos Excel: uno para Telas y uno para Avíos.");
                return View("Index");
            }

            IFormFile formFileTelas = null;
            IFormFile formFileAvios = null;

            foreach (var file in files)
            {
                if (file == null || file.Length == 0) continue;

                try
                {
                    using (var stream = new MemoryStream()) // Usar MemoryStream para poder resetear si es necesario o leer varias veces
                    {
                        await file.CopyToAsync(stream);
                        stream.Position = 0;

                        using (var package = new ExcelPackage(stream))
                        {
                            var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                            if (worksheet == null || worksheet.Dimension == null) continue;

                            var headerTexts = new List<string>();
                            for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
                            {
                                headerTexts.Add(worksheet.Cells[1, col].Value?.ToString().Trim().ToLower() ?? "");
                            }

                            // Criterios de identificación
                            bool esTela = headerTexts.Contains("costopormetro");
                            bool esAvio = headerTexts.Contains("costounidad") || headerTexts.Contains("unidadmedida");

                            if (esTela && formFileTelas == null)
                            {
                                formFileTelas = file;
                            }
                            else if (esAvio && formFileAvios == null)
                            {
                                formFileAvios = file;
                            }
                        }
                    }
                }
                catch (System.Exception ex) // Captura excepciones de lectura de Excel (ej. archivo corrupto, no es Excel)
                {
                    // Loggear ex si es necesario
                    ModelState.AddModelError("", $"Error al procesar el archivo '{file.FileName}': {ex.Message}. Asegúrate de que sea un archivo Excel válido.");
                    return View("Index");
                }
            }

            if (formFileTelas == null || formFileAvios == null || formFileTelas == formFileAvios)
            {
                ModelState.AddModelError("", "No se pudieron identificar unívocamente los archivos de Telas y Avíos basados en su contenido. Verifique las columnas de sus archivos Excel (ej. 'CostoPorMetro' para Telas, 'CostoUnidad'/'UnidadMedida' para Avíos).");
                return View("Index");
            }

            using (var telasStream = new MemoryStream())
            using (var aviosStream = new MemoryStream())
            {
                await formFileTelas.CopyToAsync(telasStream);
                await formFileAvios.CopyToAsync(aviosStream);

                telasStream.Position = 0;
                aviosStream.Position = 0;

                var telasData = _excelService.LeerArchivoTelas(telasStream); // Renombrado para evitar colisión con 'telas' del foreach
                var aviosData = _excelService.LeerArchivoAvios(aviosStream); // Renombrado

                BD.InicializarTiposInsumo();
                int idTipoTela = BD.ObtenerIdTipoPorNombre("Tela");
                int idTipoAvio = BD.ObtenerIdTipoPorNombre("Avio");

                if (idTipoTela != 0 && telasData != null)
                {
                    foreach (var telaExcel in telasData)
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

                if (idTipoAvio != 0 && aviosData != null)
                {
                    foreach (var avioExcel in aviosData)
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

                var resultados = _calculoService.CalcularCostos(telasData, aviosData);
                TempData["Resultados"] = JsonConvert.SerializeObject(resultados);
            }
            return RedirectToAction("Resultados");
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
