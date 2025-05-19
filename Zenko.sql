CREATE DATABASE Zenko;
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

INSERT INTO Tipos_Insumo (IdTipoInsumo, Nombre, CodigoPrefijo) VALUES (1, 'Tela', 'V23');
INSERT INTO Tipos_Insumo (IdTipoInsumo, Nombre, CodigoPrefijo) VALUES (2, 'Avio', 'I18');