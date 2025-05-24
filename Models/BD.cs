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
}
