using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;
using Zenko.Services;
using Zenko.Models;
using System.Data;
using System.Data.SqlClient;
using System;


public class HomeController : Controller
{
    private readonly ExcelService _excelService;
    private readonly IConfiguration _configuration;

    public HomeController(ExcelService excelService, IConfiguration configuration)
    {
        _excelService = excelService;
        _configuration = configuration;
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

    var (telas, avios) = _excelService.LeerArchivos(archivosExcel);

    using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
    await connection.OpenAsync();

    // 1) Mover todo lo que hay en Insumos a Historico_Insumos y vaciar Insumos
    var moverAHistorico = new SqlCommand(@"
        INSERT INTO Historico_Insumos
        SELECT * FROM Insumos;
        DELETE FROM Insumos;", connection);

    await moverAHistorico.ExecuteNonQueryAsync();

    // 2) Insertar TELAS
    foreach (var tela in telas)
    {
        int idTipoInsumo = await ObtenerOInsertarTipoInsumo(connection, tela.Codigo);
        await InsertarInsumo(connection, new Insumo
        {
            CodigoInsumo = tela.Codigo,
            IdTipoInsumo = idTipoInsumo,
            Costo = tela.CostoPorMetro,
            FechaRegistro = DateTime.Now
        });
    }

    // 3) Insertar AVÍOS - Igual que con telas, no olvides hacer esto
    foreach (var avio in avios)
    {
        int idTipoInsumo = await ObtenerOInsertarTipoInsumo(connection, avio.Codigo);
        await InsertarInsumo(connection, new Insumo
        {
            CodigoInsumo = avio.Codigo,
            IdTipoInsumo = idTipoInsumo,
            Costo = avio.CostoUnidad,
            FechaRegistro = DateTime.Now
        });
    }

    ViewData["Telas"] = telas;
    ViewData["Avios"] = avios;

    return View();
}

    // Método auxiliar para llamar al SP ObtenerOInsertarTipoInsumoPorCodigo
    private async Task<int> ObtenerOInsertarTipoInsumo(SqlConnection connection, string codigoInsumo)
    {
        using var command = new SqlCommand("dbo.ObtenerOInsertarTipoInsumoPorCodigo", connection);
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.AddWithValue("@CodigoInsumo", codigoInsumo);
        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    // Método auxiliar para llamar al SP InsertarInsumo
    private async Task InsertarInsumo(SqlConnection connection, Insumo insumo)
    {
        using var command = new SqlCommand("dbo.InsertarInsumo", connection);
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.AddWithValue("@CodigoInsumo", insumo.CodigoInsumo);
        command.Parameters.AddWithValue("@IdTipoInsumo", insumo.IdTipoInsumo);
        command.Parameters.AddWithValue("@Costo", insumo.Costo);
        command.Parameters.AddWithValue("@FechaRegistro", insumo.FechaRegistro);
        await command.ExecuteNonQueryAsync();
    }
}