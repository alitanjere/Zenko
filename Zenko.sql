-- Dropping existing objects to start clean, in reverse order of dependency.
IF OBJECT_ID('dbo.UpsertProductoInsumo', 'P') IS NOT NULL DROP PROCEDURE dbo.UpsertProductoInsumo;
IF OBJECT_ID('dbo.ReemplazarInsumosPorProducto', 'P') IS NOT NULL DROP PROCEDURE dbo.ReemplazarInsumosPorProducto;
IF OBJECT_ID('dbo.ProductoInsumo', 'U') IS NOT NULL DROP TABLE dbo.ProductoInsumo;
IF OBJECT_ID('dbo.Historico_Insumos', 'U') IS NOT NULL DROP TABLE dbo.Historico_Insumos;
IF OBJECT_ID('dbo.InsertarInsumo', 'P') IS NOT NULL DROP PROCEDURE dbo.InsertarInsumo;
IF OBJECT_ID('dbo.Insumos', 'U') IS NOT NULL DROP TABLE dbo.Insumos;
IF OBJECT_ID('dbo.ObtenerReporteFinal', 'P') IS NOT NULL DROP PROCEDURE dbo.ObtenerReporteFinal;
IF OBJECT_ID('dbo.ObtenerOInsertarTipoInsumoPorCodigo', 'P') IS NOT NULL DROP PROCEDURE dbo.ObtenerOInsertarTipoInsumoPorCodigo;
IF OBJECT_ID('dbo.Tipos_Insumo', 'U') IS NOT NULL DROP TABLE dbo.Tipos_Insumo;
IF OBJECT_ID('dbo.UpsertProducto', 'P') IS NOT NULL DROP PROCEDURE dbo.UpsertProducto;
IF OBJECT_ID('dbo.Productos', 'U') IS NOT NULL DROP TABLE dbo.Productos;
IF TYPE_ID('dbo.InsumoConCantidadList') IS NOT NULL DROP TYPE dbo.InsumoConCantidadList;
IF TYPE_ID('dbo.CodigoInsumoList') IS NOT NULL DROP TYPE dbo.CodigoInsumoList;
GO

-- Create Tables with new schema
CREATE TABLE Tipos_Insumo (
    IdTipoInsumo INT PRIMARY KEY,
    Nombre NVARCHAR(50) NOT NULL,
    CodigoPrefijo VARCHAR(5) NOT NULL
);

CREATE TABLE Insumos (
    CodigoInsumo NVARCHAR(20) PRIMARY KEY,
    IdTipoInsumo INT NOT NULL,
    Descripcion NVARCHAR(255) NULL,
    Costo DECIMAL(10, 2),
    FechaRegistro DATETIME NOT NULL,
    FOREIGN KEY (IdTipoInsumo) REFERENCES Tipos_Insumo(IdTipoInsumo)
);

CREATE TABLE Productos (
    VarianteCodigo NVARCHAR(20) PRIMARY KEY, -- New Primary Key
    VarianteNombre NVARCHAR(100) NOT NULL,
    ModeloCodigo NVARCHAR(20) NOT NULL,
    ModeloNombre NVARCHAR(100) NOT NULL
);

CREATE TABLE ProductoInsumo (
    IdProductoInsumo INT PRIMARY KEY IDENTITY(1,1),
    VarianteCodigo NVARCHAR(20) NOT NULL,
    CodigoInsumo NVARCHAR(20) NOT NULL,
    Cantidad DECIMAL(10,2) NOT NULL,
    FOREIGN KEY (VarianteCodigo) REFERENCES Productos(VarianteCodigo),
    FOREIGN KEY (CodigoInsumo) REFERENCES Insumos(CodigoInsumo)
);
GO

-- Create Stored Procedures with new logic

-- Stored procedure to classify insumos (STRICT version)
CREATE PROCEDURE dbo.ObtenerOInsertarTipoInsumoPorCodigo
    @CodigoInsumo NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    IF LEN(@CodigoInsumo) < 4
    BEGIN
        RAISERROR('El CodigoInsumo ''%s'' es demasiado corto.', 16, 1, @CodigoInsumo);
        RETURN;
    END
    DECLARE @PrimeraLetra CHAR(1) = LEFT(@CodigoInsumo, 1);
    DECLARE @TercerLetra CHAR(1) = SUBSTRING(@CodigoInsumo, 4, 1);
    DECLARE @Prefijo VARCHAR(2) = @PrimeraLetra + @TercerLetra;
    DECLARE @Tipo NVARCHAR(10);
    DECLARE @IdTipoInsumo INT;

    IF (@Prefijo = 'VK' OR @Prefijo = 'VM' OR @Prefijo = 'IK' OR @Prefijo = 'IM')
        SET @Tipo = 'Tela';
    ELSE IF (@Prefijo = 'VA' OR @Prefijo = 'IA')
        SET @Tipo = 'Avio';
    ELSE
    BEGIN
        RAISERROR('Prefijo no valido para tipo de insumo.', 16, 1);
        RETURN;
    END

    SELECT @IdTipoInsumo = IdTipoInsumo FROM Tipos_Insumo WHERE CodigoPrefijo = @Prefijo;
    IF @IdTipoInsumo IS NULL
    BEGIN
        DECLARE @NuevoId INT = ISNULL((SELECT MAX(IdTipoInsumo) FROM Tipos_Insumo), 0) + 1;
        INSERT INTO Tipos_Insumo (IdTipoInsumo, Nombre, CodigoPrefijo)
        VALUES (@NuevoId, @Tipo, @Prefijo);
        SET @IdTipoInsumo = @NuevoId;
    END
    SELECT @IdTipoInsumo AS IdTipoInsumo;
END;
GO

-- Stored procedure for Insumos (this is for the *other* upload page)
CREATE PROCEDURE dbo.InsertarInsumo
    @CodigoInsumo NVARCHAR(20),
    @IdTipoInsumo INT,
    @Descripcion NVARCHAR(255),
    @Costo DECIMAL(10, 2),
    @FechaRegistro DATETIME
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM Insumos WHERE CodigoInsumo = @CodigoInsumo)
    BEGIN
        UPDATE Insumos
        SET IdTipoInsumo = @IdTipoInsumo,
            Descripcion = @Descripcion,
            Costo = @Costo,
            FechaRegistro = @FechaRegistro
        WHERE CodigoInsumo = @CodigoInsumo;
    END
    ELSE
    BEGIN
        INSERT INTO Insumos (CodigoInsumo, IdTipoInsumo, Descripcion, Costo, FechaRegistro)
        VALUES (@CodigoInsumo, @IdTipoInsumo, @Descripcion, @Costo, @FechaRegistro);
    END
END;
GO

-- Stored procedure to upsert products with the new schema
CREATE PROCEDURE dbo.UpsertProducto
    @VarianteCodigo NVARCHAR(20),
    @VarianteNombre NVARCHAR(100),
    @ModeloCodigo NVARCHAR(20),
    @ModeloNombre NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM Productos WHERE VarianteCodigo = @VarianteCodigo)
    BEGIN
        UPDATE Productos
        SET VarianteNombre = @VarianteNombre,
            ModeloCodigo = @ModeloCodigo,
            ModeloNombre = @ModeloNombre
        WHERE VarianteCodigo = @VarianteCodigo;
    END
    ELSE
    BEGIN
        INSERT INTO Productos (VarianteCodigo, VarianteNombre, ModeloCodigo, ModeloNombre)
        VALUES (@VarianteCodigo, @VarianteNombre, @ModeloCodigo, @ModeloNombre);
    END
END;
GO

-- TVP for the recipe
CREATE TYPE dbo.InsumoConCantidadList AS TABLE (
    CodigoInsumo NVARCHAR(20),
    Cantidad DECIMAL(10,2)
);
GO

-- Stored procedure to replace the recipe for a product
CREATE PROCEDURE dbo.ReemplazarInsumosPorProducto
    @VarianteCodigo NVARCHAR(20),
    @Insumos dbo.InsumoConCantidadList READONLY
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;

    -- Check that all insumos exist. If not, rollback and error.
    IF EXISTS (SELECT 1 FROM @Insumos i LEFT JOIN Insumos db_i ON i.CodigoInsumo = db_i.CodigoInsumo WHERE db_i.CodigoInsumo IS NULL)
    BEGIN
        ROLLBACK TRANSACTION;
        RAISERROR('Uno o más códigos de insumo no existen en la base de datos.', 16, 1);
        RETURN;
    END

    -- Delete old recipe
    DELETE FROM ProductoInsumo WHERE VarianteCodigo = @VarianteCodigo;

    -- Insert new recipe
    INSERT INTO ProductoInsumo (VarianteCodigo, CodigoInsumo, Cantidad)
    SELECT @VarianteCodigo, CodigoInsumo, Cantidad FROM @Insumos;

    COMMIT TRANSACTION;
END;
GO

-- Stored procedure for the final report (updated for new schema)
CREATE PROCEDURE dbo.ObtenerReporteFinal
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        p.VarianteCodigo,
        p.VarianteNombre,
        p.ModeloCodigo,
        p.ModeloNombre,
        i.CodigoInsumo,
        i.Descripcion AS InsumoDescripcion,
        i.Costo AS CostoInsumo,
        pi.Cantidad,
        (i.Costo * pi.Cantidad) AS CostoTotal
    FROM
        Productos p
    JOIN
        ProductoInsumo pi ON p.VarianteCodigo = pi.VarianteCodigo
    JOIN
        Insumos i ON pi.CodigoInsumo = i.CodigoInsumo
    ORDER BY
        p.VarianteCodigo, i.CodigoInsumo;
END;
GO
