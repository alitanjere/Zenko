using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Zenko.Hubs;
using Zenko.Models;

namespace Zenko.Services
{
    public class FileProcessingService : BackgroundService
    {
        private readonly FileProcessingQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<ProcessingHub> _hubContext;

        public FileProcessingService(FileProcessingQueue queue, IServiceScopeFactory scopeFactory, IHubContext<ProcessingHub> hubContext)
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
            _hubContext = hubContext;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var item in _queue.DequeueAsync(stoppingToken))
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var excelService = scope.ServiceProvider.GetRequiredService<ExcelService>();
                    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                    var connectionString = configuration.GetConnectionString("DefaultConnection");

                    using var connection = new SqlConnection(connectionString);
                    await connection.OpenAsync(stoppingToken);

                    await connection.ExecuteAsync("UPDATE Procesos SET Estado=@Estado, Porcentaje=@Porcentaje WHERE Id=@Id",
                        new { Estado = "Procesando", Porcentaje = 0, item.Id });

                    var formFile = new FormFile(new MemoryStream(item.Content), 0, item.Content.Length, item.FileName, item.FileName);

                    var (telas, avios) = excelService.LeerArchivos(new List<IFormFile> { formFile });

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

                    await connection.ExecuteAsync("UPDATE Procesos SET Porcentaje=@Porcentaje WHERE Id=@Id",
                        new { Porcentaje = 50, item.Id });
                    await _hubContext.Clients.All.SendAsync("ProgressUpdated", item.Id, 50, "Procesando", cancellationToken: stoppingToken);

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

                    await connection.ExecuteAsync("UPDATE Procesos SET Estado=@Estado, Porcentaje=@Porcentaje WHERE Id=@Id",
                        new { Estado = "Completado", Porcentaje = 100, item.Id });
                    await _hubContext.Clients.All.SendAsync("ProgressUpdated", item.Id, 100, "Completado", cancellationToken: stoppingToken);
                }
                catch (Exception)
                {
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                        var connectionString = configuration.GetConnectionString("DefaultConnection");
                        using var connection = new SqlConnection(connectionString);
                        await connection.ExecuteAsync("UPDATE Procesos SET Estado=@Estado WHERE Id=@Id", new { Estado = "Error", item.Id });
                        await _hubContext.Clients.All.SendAsync("ProgressUpdated", item.Id, 0, "Error", cancellationToken: stoppingToken);
                    }
                    catch { }
                }
            }
        }

        private async Task<int> ObtenerOInsertarTipoInsumo(SqlConnection connection, string codigoInsumo)
        {
            using var command = new SqlCommand("dbo.ObtenerOInsertarTipoInsumoPorCodigo", connection);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@CodigoInsumo", codigoInsumo);
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        private async Task InsertarInsumo(SqlConnection connection, Insumo insumo)
        {
            using var command = new SqlCommand("dbo.InsertarInsumo", connection);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@CodigoInsumo", insumo.CodigoInsumo);
            command.Parameters.AddWithValue("@IdTipoInsumo", insumo.IdTipoInsumo);
            command.Parameters.AddWithValue("@Costo", insumo.Costo);
            command.Parameters.AddWithValue("@FechaRegistro", insumo.FechaRegistro);
            await command.ExecuteNonQueryAsync();
        }
    }
}
