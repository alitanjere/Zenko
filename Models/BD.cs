using System;
using Microsoft.Data.SqlClient;
using Dapper;
using Newtonsoft.Json;
using Zenko.Models;
using System.Collections.Generic;
using System.Linq;

public static class BD
{
    public static void InsertarInsumo(string connectionString, Insumo insumo)
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

    public static int ObtenerIdTipoPorNombre(string connectionString, string nombre)
    {
        using (var db = new SqlConnection(connectionString))
        {
            string sql = "SELECT IdTipoInsumo FROM Tipos_Insumo WHERE Nombre = @Nombre";
            return db.QueryFirstOrDefault<int>(sql, new { Nombre = nombre });
        }
    }

    public static void InicializarTiposInsumo(string connectionString)
    {
        // Verificar e insertar "Tela"
        int idTela = ObtenerIdTipoPorNombre(connectionString, "Tela");
        if (idTela == 0) // No existe
        {
            using (var db = new SqlConnection(connectionString))
            {
                string sqlTela = "INSERT INTO Tipos_Insumo (IdTipoInsumo, Nombre, CodigoPrefijo) VALUES (@IdTipoInsumo, @Nombre, @CodigoPrefijo)";
                db.Execute(sqlTela, new { IdTipoInsumo = 1, Nombre = "Tela", CodigoPrefijo = "TLA" });
            }
        }

        // Verificar e insertar "Avio"
        int idAvio = ObtenerIdTipoPorNombre(connectionString, "Avio");
        if (idAvio == 0) // No existe
        {
            using (var db = new SqlConnection(connectionString))
            {
                string sqlAvio = "INSERT INTO Tipos_Insumo (IdTipoInsumo, Nombre, CodigoPrefijo) VALUES (@IdTipoInsumo, @Nombre, @CodigoPrefijo)";
                db.Execute(sqlAvio, new { IdTipoInsumo = 2, Nombre = "Avio", CodigoPrefijo = "AVI" });
            }
        }
    }

    public static int GuardarResultadoCalculo(string connectionString, object resultados)
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

    public static T ObtenerResultadoPorId<T>(string connectionString, int id)
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

    public static List<TipoInsumo> ObtenerTiposInsumo(string connectionString)
    {
        using (var conexion = new SqlConnection(connectionString))
        {
            conexion.Open();
            return conexion.Query<TipoInsumo>("SELECT * FROM Tipos_Insumo").ToList();
        }
    }

    public static bool ValidarUsuario(string connectionString, string usuario, string password)
    {
        using (var conexion = new SqlConnection(connectionString))
        {
            conexion.Open();
            string sql = "SELECT COUNT(1) FROM Usuarios WHERE NombreUsuario = @usuario AND Password = @password";
            int count = conexion.QueryFirst<int>(sql, new { usuario, password });
            return count > 0;
        }
    }

    public static bool RegistrarUsuario(string connectionString, string usuario, string password)
    {
        using (var conexion = new SqlConnection(connectionString))
        {
            conexion.Open();
            string checkSql = "SELECT COUNT(1) FROM Usuarios WHERE NombreUsuario = @usuario";
            int count = conexion.QueryFirst<int>(checkSql, new { usuario });
            if (count > 0)
            {
                return false;
            }
            string insertSql = "INSERT INTO Usuarios (NombreUsuario, Password) VALUES (@usuario, @password)";
            conexion.Execute(insertSql, new { usuario, password });
            return true;
        }
    }
}
