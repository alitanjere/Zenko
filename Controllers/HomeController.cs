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
        try
        {
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
        catch (SqlException ex) when (ex.Message.Contains("Prefijo no valido"))
        {
            throw new Exception($"El código de insumo '{codigoInsumo}' tiene un formato no válido y no se puede procesar.");
        }
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

        List<ProductoInsumoExcel> relaciones = new List<ProductoInsumoExcel>();

        try
        {
            relaciones = _excelService.LeerProductoInsumos(archivosExcel);

            if (!relaciones.Any())
            {
                ModelState.AddModelError("", "El archivo no contiene filas de datos válidas o no se pudieron procesar las relaciones.");
                return View();
            }

            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();

                var codigosInsumoUnicos = relaciones.Select(r => r.CodigoInsumo).Distinct().ToList();
                var dt = new DataTable();
                dt.Columns.Add("CodigoInsumo", typeof(string));
                foreach (var codigo in codigosInsumoUnicos)
                {
                    dt.Rows.Add(codigo);
                }

                var insumosACrear = new List<string>();
                using (var command = new SqlCommand("dbo.FiltrarInsumosNoExistentes", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    var tvpParam = command.Parameters.AddWithValue("@CodigosInsumo", dt);
                    tvpParam.SqlDbType = SqlDbType.Structured;
                    tvpParam.TypeName = "dbo.CodigoInsumoList";

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            insumosACrear.Add(reader.GetString(0));
                        }
                    }
                }

                foreach (var codigoInsumo in insumosACrear)
                {
                    int idTipoInsumo = await ObtenerOInsertarTipoInsumo(connection, codigoInsumo);
                    await InsertarInsumo(connection, new Insumo
                    {
                        CodigoInsumo = codigoInsumo,
                        IdTipoInsumo = idTipoInsumo,
                        Costo = 0,
                        FechaRegistro = DateTime.Now
                    });
                }

                var productosUnicos = relaciones
                    .GroupBy(r => r.CodigoProducto)
                    .Select(g => g.First())
                    .ToList();

                foreach (var producto in productosUnicos)
                {
                    await UpsertProducto(connection, new ProductoExcel { CodigoProducto = producto.CodigoProducto, NombreProducto = producto.NombreProducto });
                }

                foreach (var relacion in relaciones)
                {
                    await UpsertProductoInsumo(connection, relacion);
                }
            }

            ViewData["RelacionesProcesadas"] = relaciones;
            return View();
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Error al procesar el archivo: {ex.Message}");
            return View();
        }
    }

    private async Task<int> UpsertProducto(SqlConnection connection, ProductoExcel producto)
    {
        using var command = new SqlCommand("dbo.UpsertProducto", connection);
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.AddWithValue("@CodigoProducto", producto.CodigoProducto);
        command.Parameters.AddWithValue("@NombreProducto", producto.NombreProducto);
        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    private async Task UpsertProductoInsumo(SqlConnection connection, ProductoInsumoExcel relacion)
    {
        using var command = new SqlCommand("dbo.UpsertProductoInsumo", connection);
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.AddWithValue("@CodigoProducto", relacion.CodigoProducto);
        command.Parameters.AddWithValue("@CodigoInsumo", relacion.CodigoInsumo);
        command.Parameters.AddWithValue("@Cantidad", relacion.Cantidad);
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