using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;
using Zenko.Services;
using Zenko.Models;
using System.Data;
using Microsoft.Data.SqlClient;
using System;
using System.Linq;
using Dapper;


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

    [HttpGet]
    public IActionResult SubirProductos()
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

        int usuarioId = 1;
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        var (telas, avios) = await _excelService.ProcesarArchivos(archivosExcel, connectionString, usuarioId);

        ViewData["Telas"] = telas;
        ViewData["Avios"] = avios;

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Auditoria(int? usuarioId, DateTime? fecha)
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        using var connection = new SqlConnection(connectionString);
        var sql = "SELECT * FROM Auditorias WHERE 1=1";
        var parameters = new DynamicParameters();
        if (usuarioId.HasValue)
        {
            sql += " AND UsuarioId = @UsuarioId";
            parameters.Add("@UsuarioId", usuarioId.Value);
        }
        if (fecha.HasValue)
        {
            sql += " AND CAST(Fecha AS DATE) = @Fecha";
            parameters.Add("@Fecha", fecha.Value.Date);
        }
        sql += " ORDER BY Fecha DESC";
        var auditorias = await connection.QueryAsync<Auditoria>(sql, parameters);
        return View(auditorias);
    }

    [HttpPost]
    public async Task<IActionResult> SubirProductos(List<IFormFile> archivosExcel)
    {
        if (archivosExcel == null || archivosExcel.Count == 0)
        {
            ModelState.AddModelError("", "Por favor, suba al menos un archivo Excel de productos.");
            return View();
        }

        var productosExitosos = 0;
        var relacionesExitosas = 0;
        var variantesExitosas = new HashSet<string>();
        var variantesFallidas = new HashSet<string>();

        try
        {
            var (fichaTecnica, todasLasVariantes) = _excelService.LeerProductoInsumos(archivosExcel);

            if (!fichaTecnica.Any() && !todasLasVariantes.Any())
            {
                ModelState.AddModelError("", "El archivo no contiene filas de datos válidas o las columnas requeridas no se encontraron.");
                return View();
            }

            var productosAgrupados = fichaTecnica.GroupBy(f => f.VarianteCodigo);

            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();

                foreach (var grupoProducto in productosAgrupados)
                {
                    var primerItem = grupoProducto.First();
                    var varianteCodigo = primerItem.VarianteCodigo;
                    try
                    {
                        // 1. Upsert del producto
                        await UpsertProducto(connection, primerItem);

                        // 2. Preparar y reemplazar la receta (lista de insumos)
                        var insumosParaTvp = new DataTable();
                        insumosParaTvp.Columns.Add("CodigoInsumo", typeof(string));
                        insumosParaTvp.Columns.Add("Cantidad", typeof(decimal));

                        foreach (var item in grupoProducto)
                        {
                            insumosParaTvp.Rows.Add(item.InsumoCodigo, item.Cantidad);
                        }

                        await ReemplazarInsumos(connection, varianteCodigo, insumosParaTvp);

                        productosExitosos++;
                        relacionesExitosas += grupoProducto.Count();
                        variantesExitosas.Add(varianteCodigo);
                    }
                    catch (SqlException)
                    {
                        var modeloCodigo = primerItem.ModeloCodigo;
                        ModelState.AddModelError("", $"el producto {modeloCodigo}, en {varianteCodigo} variante falló.");
                        variantesFallidas.Add(varianteCodigo);
                    }
                }
            }

            // Check for variants that were in the file but had no valid insumos and were not processed.
            foreach (var entry in todasLasVariantes)
            {
                var varianteCodigo = entry.Key;
                if (!variantesExitosas.Contains(varianteCodigo) && !variantesFallidas.Contains(varianteCodigo))
                {
                    var modeloCodigo = entry.Value;
                    ModelState.AddModelError("", $"el producto {modeloCodigo}, en {varianteCodigo} variante falló.");
                }
            }

            if (productosExitosos > 0)
            {
                ViewData["MensajeExito"] = $"Se han procesado exitosamente {productosExitosos} de {todasLasVariantes.Count} productos y {relacionesExitosas} relaciones de insumos.";
            }
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Error al procesar el archivo: {ex.Message}");
        }

        return View();
    }

    private async Task UpsertProducto(SqlConnection connection, ProductoInsumoExcel producto)
    {
        using var command = new SqlCommand("dbo.UpsertProducto", connection);
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.AddWithValue("@VarianteCodigo", producto.VarianteCodigo);
        command.Parameters.AddWithValue("@VarianteNombre", producto.VarianteNombre);
        command.Parameters.AddWithValue("@ModeloCodigo", producto.ModeloCodigo);
        command.Parameters.AddWithValue("@ModeloNombre", producto.ModeloNombre);
        await command.ExecuteNonQueryAsync();
    }

    private async Task ReemplazarInsumos(SqlConnection connection, string varianteCodigo, DataTable insumos)
    {
        using var command = new SqlCommand("dbo.ReemplazarInsumosPorProducto", connection);
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.AddWithValue("@VarianteCodigo", varianteCodigo);
        var tvpParam = command.Parameters.AddWithValue("@Insumos", insumos);
        tvpParam.SqlDbType = SqlDbType.Structured;
        tvpParam.TypeName = "dbo.InsumoConCantidadList";
        await command.ExecuteNonQueryAsync();
    }

    public async Task<IActionResult> Resultados()
    {
        List<ReporteFinalViewModel> model;
        using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
        {
            await connection.OpenAsync();
            model = (await connection.QueryAsync<ReporteFinalViewModel>("ObtenerReporteFinal", commandType: CommandType.StoredProcedure)).ToList();
        }
        return View(model);
    }

    public async Task<IActionResult> DescargarResultados()
    {
        List<ReporteFinalViewModel> model;
        using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
        {
            await connection.OpenAsync();
            model = (await connection.QueryAsync<ReporteFinalViewModel>("ObtenerReporteFinal", commandType: CommandType.StoredProcedure)).ToList();
        }

        byte[] fileContents = _excelService.CrearExcelReporteFinal(model);
        return File(fileContents, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ReporteFinalCostos.xlsx");
    }
}