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
GO;

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

-- Determinar tipo según reglas
IF (@Prefijo = 'VK' OR @Prefijo = 'VM' OR @Prefijo = 'IK' OR @Prefijo = 'IM')
SET @Tipo = 'Tela';
ELSE IF (@Prefijo = 'VA' OR @Prefijo = 'IA')
SET @Tipo = 'Avio';
ELSE
BEGIN
RAISERROR('Prefijo no valido para tipo de insumo.', 16, 1);
RETURN;
END

-- Buscar IdTipoInsumo existente para el prefijo
SELECT @IdTipoInsumo = IdTipoInsumo FROM Tipos_Insumo WHERE CodigoPrefijo = @Prefijo;

-- Si no existe, insertarlo
IF @IdTipoInsumo IS NULL
BEGIN
-- Insertar con nuevo IdTipoInsumo (auto incrementado +1 del max)
DECLARE @NuevoId INT = ISNULL((SELECT MAX(IdTipoInsumo) FROM Tipos_Insumo), 0) + 1;

INSERT INTO Tipos_Insumo (IdTipoInsumo, Nombre, CodigoPrefijo)
VALUES (@NuevoId, @Tipo, @Prefijo);

SET @IdTipoInsumo = @NuevoId;
END

-- Devolver el IdTipoInsumo
SELECT @IdTipoInsumo AS IdTipoInsumo;
END;
EXEC dbo.ObtenerOInsertarTipoInsumoPorCodigo @CodigoInsumo = 'V23K1234';
go;

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