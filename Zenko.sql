 DATABASE Zenko;
GO

-- Usar la base de datos recién creada
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