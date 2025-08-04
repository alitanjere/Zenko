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
    CodigoInsumo NVARCHAR(20) PRIMARY KEY,
    IdTipoInsumo INT NOT NULL,
    Costo DECIMAL(10, 2),
    FechaRegistro DATETIME NOT NULL,
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
    Cantidad DECIMAL(10,2) NOT NULL,

    PRIMARY KEY (IdProducto, CodigoInsumo),
    FOREIGN KEY (IdProducto) REFERENCES Productos(IdProducto),
    FOREIGN KEY (CodigoInsumo) REFERENCES Insumos(CodigoInsumo)
);

CREATE TABLE Historico_Insumos (
    CodigoInsumo NVARCHAR(20),
    IdTipoInsumo INT,
    Costo DECIMAL(10, 2),
    FechaRegistro DATETIME
);
GO

CREATE PROCEDURE dbo.ObtenerOInsertarTipoInsumoPorCodigo
    @CodigoInsumo NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;

    IF LEN(@CodigoInsumo) < 4
    BEGIN
        RAISERROR('CodigoInsumo demasiado corto.', 16, 1);
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

CREATE PROCEDURE dbo.InsertarInsumo
    @CodigoInsumo NVARCHAR(20),
    @IdTipoInsumo INT,
    @Costo DECIMAL(10, 2),
    @FechaRegistro DATETIME
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM Insumos WHERE CodigoInsumo = @CodigoInsumo)
    BEGIN
        INSERT INTO Historico_Insumos (CodigoInsumo, IdTipoInsumo, Costo, FechaRegistro)
        SELECT CodigoInsumo, IdTipoInsumo, Costo, FechaRegistro
        FROM Insumos
        WHERE CodigoInsumo = @CodigoInsumo;

        UPDATE Insumos
        SET IdTipoInsumo = @IdTipoInsumo,
            Costo = @Costo,
            FechaRegistro = @FechaRegistro
        WHERE CodigoInsumo = @CodigoInsumo;
    END
    ELSE
    BEGIN
        INSERT INTO Insumos (CodigoInsumo, IdTipoInsumo, Costo, FechaRegistro)
        VALUES (@CodigoInsumo, @IdTipoInsumo, @Costo, @FechaRegistro);
    END
END;
GO

CREATE PROCEDURE dbo.UpsertProducto
    @CodigoProducto NVARCHAR(20),
    @NombreProducto NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ProductoID INT;

    SELECT @ProductoID = IdProducto FROM Productos WHERE CodigoProducto = @CodigoProducto;

    IF @ProductoID IS NOT NULL
    BEGIN
        -- Actualizar el producto existente
        UPDATE Productos
        SET Nombre = @NombreProducto
        WHERE IdProducto = @ProductoID;
    END
    ELSE
    BEGIN
        -- Insertar el nuevo producto
        INSERT INTO Productos (CodigoProducto, Nombre)
        VALUES (@CodigoProducto, @NombreProducto);
        SET @ProductoID = SCOPE_IDENTITY();
    END

    -- Devolver el ID del producto insertado o actualizado
    SELECT @ProductoID AS IdProducto;
END;
GO

CREATE PROCEDURE dbo.UpsertProductoInsumo
    @CodigoProducto NVARCHAR(20),
    @CodigoInsumo NVARCHAR(20),
    @Cantidad DECIMAL(10, 2)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @IdProducto INT;
    SELECT @IdProducto = IdProducto FROM Productos WHERE CodigoProducto = @CodigoProducto;

    IF @IdProducto IS NULL
    BEGIN
        RAISERROR ('Producto con CodigoProducto %s no encontrado.', 16, 1, @CodigoProducto);
        RETURN;
    END

    IF NOT EXISTS (SELECT 1 FROM Insumos WHERE CodigoInsumo = @CodigoInsumo)
    BEGIN
        RAISERROR ('Insumo con CodigoInsumo %s no encontrado.', 16, 1, @CodigoInsumo);
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM ProductoInsumo WHERE IdProducto = @IdProducto AND CodigoInsumo = @CodigoInsumo)
    BEGIN
        -- Actualizar la cantidad
        UPDATE ProductoInsumo
        SET Cantidad = @Cantidad
        WHERE IdProducto = @IdProducto AND CodigoInsumo = @CodigoInsumo;
    END
    ELSE
    BEGIN
        -- Insertar la nueva relación
        INSERT INTO ProductoInsumo (IdProducto, CodigoInsumo, Cantidad)
        VALUES (@IdProducto, @CodigoInsumo, @Cantidad);
    END
END;
GO

