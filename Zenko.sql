CREATE DATABASE Zenko;
GO

USE Zenko;
GO

CREATE TABLE Tipos_Insumo (
    IdTipoInsumo INT PRIMARY KEY,
    Nombre NVARCHAR(50) NOT NULL,
    CodigoPrefijo VARCHAR(5) NOT NULL
);

CREATE TABLE Insumos (
    CodigoInsumo nVARCHAR(20) PRIMARY KEY,
    IdTipoInsumo INT NOT NULL,
    Costo DECIMAL(10, 2),
    FechaRegistro DATE NOT NULL,
    FOREIGN KEY (IdTipoInsumo) REFERENCES Tipos_Insumo(IdTipoInsumo)
);

CREATE TABLE Productos (
    IdProducto INT PRIMARY KEY IDENTITY(1,1),
    Nombre NVARCHAR(100) NOT NULL,
    CodigoProducto NVARCHAR(20) NOT NULL UNIQUE
);

CREATE TABLE ProductoInsumo (
    IdProducto INT NOT NULL,
    CodigoInsumo NVARCHAR(20) NOT NULL,
    Cantidad DECIMAL(10,2) NOT NULL, -- Por ejemplo, metros de tela o unidades de botón

    PRIMARY KEY (IdProducto, CodigoInsumo),
    FOREIGN KEY (IdProducto) REFERENCES Productos(IdProducto),
    FOREIGN KEY (CodigoInsumo) REFERENCES Insumos(CodigoInsumo)
);


--Inserts futuros
--INSERT INTO Tipos_Insumo (IdTipoInsumo, Nombre, CodigoPrefijo) VALUES (1, 'Tela', 'V23');
--INSERT INTO Tipos_Insumo (IdTipoInsumo, Nombre, CodigoPrefijo) VALUES (2, 'Avio', 'I18');

public (List<TelaExcel> telas, List<AvioExcel> avios) LeerArchivoInsumos(Stream stream)
    {
        var telas = new List<TelaExcel>();
        var avios = new List<AvioExcel>();

        if (stream == null) return (telas, avios);

        using (var package = new ExcelPackage(stream))
        {
            var worksheet = package.Workbook.Worksheets.FirstOrDefault();
            if (worksheet == null) return (telas, avios);

            for (int row = 2; ; row++)
            {
                var codigo = worksheet.Cells[row, 1].Value?.ToString();
                if (string.IsNullOrWhiteSpace(codigo)) break;

                string tipoInsumo = ObtenerTipoDesdeBD(codigo);
                if (string.IsNullOrEmpty(tipoInsumo)) continue;

                var costoStr = worksheet.Cells[row, 2].Value?.ToString();
                decimal costo = ParsearDecimalDesdeString(costoStr);

                if (costo < 0) continue;

                if (tipoInsumo == "Tela")
                {
                    telas.Add(new TelaExcel
                    {
                        Codigo = codigo,
                        CostoPorMetro = costo
                    });
                }
                else if (tipoInsumo == "Avio")
                {
                    avios.Add(new AvioExcel
                    {
                        Codigo = codigo,
                        CostoUnidad = costo
                    });
                }
            }
        }

        return (telas, avios);
    }

    private string ObtenerTipoDesdeBD(string codigo)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            var result = connection.QueryFirstOrDefault<int?>(
                "dbo.ObtenerOInsertarTipoInsumoPorCodigo",
                new { CodigoInsumo = codigo },
                commandType: CommandType.StoredProcedure
            );

            if (result.HasValue)
            {
                string prefijo = $"{codigo[0]}{codigo[3]}";
                return prefijo switch
                {
                    "VK" or "VM" or "IK" or "IM" => "Tela",
                    "VA" or "IA" => "Avio",
                    _ => null
                };
            }

            return null;
        }
    }

CREATE PROCEDURE dbo.InsertarInsumo
    @CodigoInsumo NVARCHAR(20),
    @IdTipoInsumo INT,
    @Costo DECIMAL(10, 2),
    @FechaRegistro DATE
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM Insumos WHERE CodigoInsumo = @CodigoInsumo)
    BEGIN
        INSERT INTO Insumos (CodigoInsumo, IdTipoInsumo, Costo, FechaRegistro)
        VALUES (@CodigoInsumo, @IdTipoInsumo, @Costo, @FechaRegistro);
    END
END;