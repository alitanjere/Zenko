using System;
using System.Data;
using System.Data.SqlClient;
using Dapper;
using Newtonsoft.Json;
using Zenko.Models; // Asumiendo que Insumo, TipoInsumo, etc. están aquí
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration; // Para IConfiguration

public static class BD
{
    // La cadena de conexión se inyectará o se leerá de appsettings.json
    // Por ahora, la dejamos como estaba, pero idealmente se gestiona centralizadamente.
    private static string _connectionString = ""; // Se establecerá en Inicializar

    // Método para establecer la cadena de conexión. Debería ser llamado desde Startup.cs o Program.cs
    public static void Inicializar(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
        // La lógica de InicializarTiposInsumo ya no es necesaria aquí si el SQL se encarga.
        // Opcionalmente, se puede mantener si se quiere una verificación desde C#.
        // Comentado por ahora para simplificar, asumiendo que el script SQL maneja la creación de Tipos_Insumo.
        // InicializarTiposInsumoBase();
    }

    // Método renombrado para reflejar que llama al nuevo SP.
    // El modelo Insumo que recibe debe tener las propiedades CodigoInsumo, IdTipoInsumo, Costo, FechaRegistro
    // que se mapearán a los parámetros del SP InsertarInsumoNuevo.
    public static void InsertarInsumoNuevo(Insumo insumo)
    {
        using (var db = new SqlConnection(_connectionString))
        {
            var parametros = new
            {
                insumo.CodigoInsumo,
                insumo.IdTipoInsumo,
                Costo = insumo.Costo, // El SP espera @Costo
                FechaRegistro = insumo.FechaRegistro // El SP espera @FechaRegistro
            };
            // Llamar al nuevo procedimiento almacenado
            db.Execute("dbo.InsertarInsumoNuevo", parametros, commandType: CommandType.StoredProcedure);
        }
    }

    // Ya no se usa directamente ObtenerIdTipoPorNombre si el SP ObtenerOInsertarTipoInsumoPorCodigo maneja la lógica de tipos.
    // El SP ObtenerOInsertarTipoInsumoPorCodigo devuelve el IdTipoInsumo (1 o 2) directamente.
    // El HomeController ya usa este SP.

    // Comentando InicializarTiposInsumo ya que el script SQL Zenko.sql se encarga de esto.
    /*
    private static void InicializarTiposInsumoBase()
    {
        using (var db = new SqlConnection(_connectionString))
        {
            // Asegurar "Tela"
            var tela = db.QueryFirstOrDefault<TipoInsumo>("SELECT * FROM Tipos_Insumo WHERE IdTipoInsumo = 1");
            if (tela == null)
            {
                db.Execute("INSERT INTO Tipos_Insumo (IdTipoInsumo, Nombre) VALUES (1, 'Tela')");
            }

            // Asegurar "Avio"
            var avio = db.QueryFirstOrDefault<TipoInsumo>("SELECT * FROM Tipos_Insumo WHERE IdTipoInsumo = 2");
            if (avio == null)
            {
                db.Execute("INSERT INTO Tipos_Insumo (IdTipoInsumo, Nombre) VALUES (2, 'Avio')");
            }
        }
    }
    */

    // Nuevo método para crear un registro en SubidasHistoricas y devolver el IdSubida
    public static int CrearRegistroSubida(string nombreArchivoOriginal)
    {
        using (var db = new SqlConnection(_connectionString))
        {
            var sql = @"INSERT INTO SubidasHistoricas (NombreArchivoOriginal, FechaSubida)
                        OUTPUT INSERTED.IdSubida
                        VALUES (@NombreArchivoOriginal, GETDATE());";
            return db.ExecuteScalar<int>(sql, new { NombreArchivoOriginal = nombreArchivoOriginal });
        }
    }

    // Nuevo método para actualizar CantidadRegistrosSubidos en SubidasHistoricas
    public static void ActualizarRegistroSubida(int idSubida, int cantidadRegistrosSubidos)
    {
        using (var db = new SqlConnection(_connectionString))
        {
            var sql = @"UPDATE SubidasHistoricas
                        SET CantidadRegistrosSubidos = @CantidadRegistrosSubidos
                        WHERE IdSubida = @IdSubida;";
            db.Execute(sql, new { CantidadRegistrosSubidos = cantidadRegistrosSubidos, IdSubida = idSubida });
        }
    }

    // Nuevo método para llamar al SP MoverInsumosAHistoricoYVaciarActuales
    public static void MoverInsumosAHistoricoYVaciar(int idSubida)
    {
        using (var db = new SqlConnection(_connectionString))
        {
            db.Execute("dbo.MoverInsumosAHistoricoYVaciarActuales", new { IdSubida = idSubida }, commandType: CommandType.StoredProcedure);
        }
    }

    // Los métodos GuardarResultadoCalculo y ObtenerResultadoPorId parecen no estar relacionados
    // con la funcionalidad de precios de insumos, así que los mantenemos como están.
    public static int GuardarResultadoCalculo(object resultados)
    {
        var json = JsonConvert.SerializeObject(resultados);
        using (var conexion = new SqlConnection(_connectionString))
        {
            // Asegurarse que la tabla Resultados_Calculos exista si se va a usar esta funcionalidad.
            // Esta tabla no está en el script Zenko.sql proporcionado recientemente.
            // Si es necesaria, debe añadirse al script Zenko.sql.
            // Por ahora, se asume que existe o que esta funcionalidad no es crítica para la tarea actual.
            conexion.Open();
            var comando = new SqlCommand(
                "INSERT INTO Resultados_Calculos (DatosJson) OUTPUT INSERTED.IdResultado VALUES (@datos)", conexion);
            comando.Parameters.AddWithValue("@datos", json);
            int id = (int)comando.ExecuteScalar();
            return id;
        }
    }

    public static T ObtenerResultadoPorId<T>(int id)
    {
        using (var conexion = new SqlConnection(_connectionString))
        {
            // Ver comentario en GuardarResultadoCalculo sobre la tabla Resultados_Calculos.
            conexion.Open();
            var comando = new SqlCommand("SELECT DatosJson FROM Resultados_Calculos WHERE IdResultado = @id", conexion);
            comando.Parameters.AddWithValue("@id", id);
            var reader = comando.ExecuteReader();
            if (reader.Read())
            {
                var json = reader.GetString(0);
                return JsonConvert.DeserializeObject<T>(json);
            }
            return default(T);
        }
    }

    public static List<TipoInsumo> ObtenerTiposInsumo()
    {
        using (var conexion = new SqlConnection(_connectionString))
        {
            // Esta consulta asume que Tipos_Insumo tiene IdTipoInsumo y Nombre.
            return conexion.Query<TipoInsumo>("SELECT IdTipoInsumo, Nombre FROM Tipos_Insumo").ToList();
        }
    }

    // Nuevo método para obtener el historial de precios de un insumo
    // Necesitaremos un modelo para InsumoHistoricoViewModel o similar para transportar estos datos.
    // Por ahora, devolveremos una lista de dynamic o un modelo simple si ya existe.
    // Asumiendo que InsumoHistorico es un modelo existente o se creará.
    public static List<InsumoHistorico> ObtenerHistorialInsumo(string codigoInsumo)
    {
        using (var db = new SqlConnection(_connectionString))
        {
            var sql = @"SELECT
                            ih.IdHistorico,
                            ih.CodigoInsumo,
                            ih.IdTipoInsumo,
                            ti.Nombre as NombreTipoInsumo,
                            ih.CostoAnterior,
                            ih.FechaCambio,
                            ih.IdSubida,
                            sh.FechaSubida,
                            sh.NombreArchivoOriginal
                        FROM InsumosHistoricos ih
                        JOIN Tipos_Insumo ti ON ih.IdTipoInsumo = ti.IdTipoInsumo
                        JOIN SubidasHistoricas sh ON ih.IdSubida = sh.IdSubida
                        WHERE ih.CodigoInsumo = @CodigoInsumo
                        ORDER BY ih.FechaCambio DESC;";
            return db.Query<InsumoHistorico>(sql, new { CodigoInsumo = codigoInsumo }).ToList();
        }
    }
}
