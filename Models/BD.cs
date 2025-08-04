using System;
using Microsoft.Data.SqlClient;
using Dapper;
using Newtonsoft.Json;
using Zenko.Models;
using System.Collections.Generic;
using System.Linq;

public static class BD
{
    private static string connectionString = "Server=localhost;Database=Zenko;Trusted_Connection=True;TrustServerCertificate=True;";

  public static void InsertarInsumo(Insumo insumo)
{
    using (var db = new SqlConnection(connectionString))
    {
        var parametros = new
        {
            CodigoInsumo = insumo.CodigoInsumo,
            IdTipoInsumo = insumo.IdTipoInsumo,
            Costo = insumo.Costo,
            FechaRegistro = insumo.FechaRegistro
        };

        db.Execute("InsertarInsumo", parametros, commandType: System.Data.CommandType.StoredProcedure);
    }
}


    public static int ObtenerIdTipoPorNombre(string nombre)
    {
        using (var db = new SqlConnection(connectionString))
        {
            string sql = "SELECT IdTipoInsumo FROM Tipos_Insumo WHERE Nombre = @Nombre";
            return db.QueryFirstOrDefault<int>(sql, new { Nombre = nombre });
        }
    }

    public static void InicializarTiposInsumo()
    {
        using (var db = new SqlConnection(connectionString))
        {
            // Verificar e insertar "Tela"
            int idTela = ObtenerIdTipoPorNombre("Tela");
            if (idTela == 0) // No existe
            {
                string sqlTela = "INSERT INTO Tipos_Insumo (IdTipoInsumo, Nombre, CodigoPrefijo) VALUES (@IdTipoInsumo, @Nombre, @CodigoPrefijo)";
                db.Execute(sqlTela, new { IdTipoInsumo = 1, Nombre = "Tela", CodigoPrefijo = "TLA" });
            }

            // Verificar e insertar "Avio"
            int idAvio = ObtenerIdTipoPorNombre("Avio");
            if (idAvio == 0) // No existe
            {
                string sqlAvio = "INSERT INTO Tipos_Insumo (IdTipoInsumo, Nombre, CodigoPrefijo) VALUES (@IdTipoInsumo, @Nombre, @CodigoPrefijo)";
                db.Execute(sqlAvio, new { IdTipoInsumo = 2, Nombre = "Avio", CodigoPrefijo = "AVI" });
            }
        }
    }

    public static int GuardarResultadoCalculo(object resultados)
    {
        var json = JsonConvert.SerializeObject(resultados);

        using (var conexion = new SqlConnection(connectionString))
        {
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
        using (var conexion = new SqlConnection(connectionString))
        {
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
        using (var conexion = new SqlConnection(connectionString))
        {
            conexion.Open();
            return conexion.Query<TipoInsumo>("SELECT * FROM Tipos_Insumo").ToList();
        }
    }
}
