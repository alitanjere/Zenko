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

    var (telas, avios) = _excelService.LeerArchivos(archivosExcel);

    using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
    await connection.OpenAsync();

    // Insertar TELAS (actualizar si existen)
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

    // Insertar AVÍOS (actualizar si existen)
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

    [HttpGet]
    public async Task<IActionResult> Variaciones(DateTime? fechaAnterior)
    {
        var fecha = fechaAnterior?.Date ?? DateTime.Today;
        using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        var variaciones = (await connection.QueryAsync<VariacionCosto>(
            "ObtenerVariacionCostos",
            new { FechaAnterior = fecha },
            commandType: CommandType.StoredProcedure)).ToList();
        return View(variaciones);
    }
}