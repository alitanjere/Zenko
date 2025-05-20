using System.Data.SqlClient;
using Dapper;

public static class BD
{
    private static string connectionString = "Server=localhost;Database=Zenko;Trusted_Connection=True;";

    public static void InsertarInsumo(Insumo insumo)
    {
        using (var db = new SqlConnection(connectionString))
        {
            string sql = @"INSERT INTO Insumos (CodigoInsumo, IdTipoInsumo, Costo, FechaRegistro)
                           VALUES (@CodigoInsumo, @IdTipoInsumo, @Costo, @FechaRegistro)";
            db.Execute(sql, insumo);
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
}
