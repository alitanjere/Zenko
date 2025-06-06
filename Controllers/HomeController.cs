using Microsoft.AspNetCore.Mvc;
using Zenko.Services;
using Zenko.Models;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

public class HomeController : Controller
{
    private readonly ExcelService _excelService;

    public HomeController(ExcelService excelService)
    {
        _excelService = excelService;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Index(List<IFormFile> archivosExcel)
    {
        if (archivosExcel == null || archivosExcel.Count == 0)
        {
            ModelState.AddModelError("", "Por favor, suba al menos un archivo Excel.");
            return View();
        }

        // Llamamos al método unificado que devuelve telas y avíos juntos
        var (telas, avios) = _excelService.LeerArchivos(archivosExcel);

        // Aquí podrías hacer lo que necesites con las listas telas y avios
        // Por ejemplo, guardarlas en base de datos, o mostrarlas en la vista

        ViewData["Telas"] = telas;
        ViewData["Avios"] = avios;

        return View();
    }
}
