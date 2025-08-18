
/****** Object:  UserDefinedTableType [dbo].[InsumoConCantidadList]    Script Date: 18/8/2025 08:39:38 ******/
CREATE TYPE [dbo].[InsumoConCantidadList] AS TABLE(
	[CodigoInsumo] [nvarchar](20) NULL,
	[Cantidad] [decimal](10, 2) NULL
)
GO
/****** Object:  Table [dbo].[Insumos]    Script Date: 18/8/2025 08:39:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Insumos](
	[CodigoInsumo] [nvarchar](20) NOT NULL,
	[IdTipoInsumo] [int] NOT NULL,
	[Descripcion] [nvarchar](255) NULL,
	[Costo] [decimal](10, 2) NULL,
	[FechaRegistro] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[CodigoInsumo] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ProductoInsumo]    Script Date: 18/8/2025 08:39:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ProductoInsumo](
	[IdProductoInsumo] [int] IDENTITY(1,1) NOT NULL,
	[IdProducto] [int] NOT NULL,
	[CodigoInsumo] [nvarchar](20) NOT NULL,
	[Cantidad] [decimal](10, 2) NOT NULL,
 CONSTRAINT [PK__Producto__EBE036107724CB3F] PRIMARY KEY CLUSTERED 
(
	[IdProductoInsumo] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Productos]    Script Date: 18/8/2025 08:39:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Productos](
	[IdProducto] [int] IDENTITY(1,1) NOT NULL,
	[VarianteCodigo] [nvarchar](20) NOT NULL,
	[VarianteNombre] [nvarchar](100) NOT NULL,
	[ModeloCodigo] [nvarchar](20) NOT NULL,
	[ModeloNombre] [nvarchar](100) NOT NULL,
 CONSTRAINT [PK_Productos] PRIMARY KEY CLUSTERED 
(
	[IdProducto] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Tipos_Insumo]    Script Date: 18/8/2025 08:39:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Tipos_Insumo](
	[IdTipoInsumo] [int] NOT NULL,
	[Nombre] [nvarchar](50) NOT NULL,
	[CodigoPrefijo] [varchar](5) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[IdTipoInsumo] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[Insumos]  WITH CHECK ADD FOREIGN KEY([IdTipoInsumo])
REFERENCES [dbo].[Tipos_Insumo] ([IdTipoInsumo])
GO
ALTER TABLE [dbo].[ProductoInsumo]  WITH CHECK ADD  CONSTRAINT [FK_ProductoInsumo_Insumo] FOREIGN KEY([CodigoInsumo])
REFERENCES [dbo].[Insumos] ([CodigoInsumo])
GO
ALTER TABLE [dbo].[ProductoInsumo] CHECK CONSTRAINT [FK_ProductoInsumo_Insumo]
GO
ALTER TABLE [dbo].[ProductoInsumo]  WITH CHECK ADD  CONSTRAINT [FK_ProductoInsumo_Producto] FOREIGN KEY([IdProducto])
REFERENCES [dbo].[Productos] ([IdProducto])
GO
ALTER TABLE [dbo].[ProductoInsumo] CHECK CONSTRAINT [FK_ProductoInsumo_Producto]
GO
/****** Object:  StoredProcedure [dbo].[InsertarInsumo]    Script Date: 18/8/2025 08:39:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Stored procedure for Insumos (this is for the *other* upload page)
CREATE PROCEDURE [dbo].[InsertarInsumo]
    @CodigoInsumo NVARCHAR(20),
    @IdTipoInsumo INT,
    @Descripcion NVARCHAR(255) = NULL,
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
/****** Object:  StoredProcedure [dbo].[ObtenerOInsertarTipoInsumoPorCodigo]    Script Date: 18/8/2025 08:39:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Create Stored Procedures with new logic

-- Stored procedure to classify insumos (STRICT version)
CREATE PROCEDURE [dbo].[ObtenerOInsertarTipoInsumoPorCodigo]
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
/****** Object:  StoredProcedure [dbo].[ObtenerReporteFinal]    Script Date: 18/8/2025 08:39:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Stored procedure for the final report (updated for new schema)
CREATE PROCEDURE [dbo].[ObtenerReporteFinal]
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
        ProductoInsumo pi ON p.IdProducto = pi.IdProducto
    JOIN
        Insumos i ON pi.CodigoInsumo = i.CodigoInsumo
    ORDER BY
        p.VarianteCodigo, i.CodigoInsumo;
END;
GO
/****** Object:  StoredProcedure [dbo].[ReemplazarInsumosPorProducto]    Script Date: 18/8/2025 08:39:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


-- Stored procedure to replace the recipe for a product
CREATE PROCEDURE [dbo].[ReemplazarInsumosPorProducto]
    @VarianteCodigo NVARCHAR(20),
    @Insumos dbo.InsumoConCantidadList READONLY
AS
BEGIN
	DECLARE @IdProducto int
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
    SELECT @IdProducto = IdProducto From Productos WHERE VarianteCodigo = @VarianteCodigo;
	DELETE FROM ProductoInsumo WHERE IdProducto = @IdProducto;
	-- Insert new recipe
    INSERT INTO ProductoInsumo (IdProducto, CodigoInsumo, Cantidad)
    SELECT @IdProducto, CodigoInsumo, Cantidad FROM @Insumos;

    COMMIT TRANSACTION;
END;
GO
/****** Object:  StoredProcedure [dbo].[UpsertProducto]    Script Date: 18/8/2025 08:39:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Stored procedure to upsert products with the new schema
CREATE PROCEDURE [dbo].[UpsertProducto]
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
