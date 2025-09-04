BEGIN TRY
BEGIN TRAN

-- Limpiar vista si existe
EXEC dbo.eliminarVista N'TestVistaLocal';

-- === Insert Vistas ===

INSERT INTO dbo.Vistas
    (TipoVista, UsaSeguridad, Descripcion, Observacion, VersionActual, ArchivoJs, IdVista, Estilo, IdStringsConnectionMyVision, JsCode)
VALUES
    (1, 1, N'Vista de prueba generada por ViewScriptGen', N'', N'1.0', N'js/TestVistaLocal.js', N'TestVistaLocal', N'go',
     14, NULL);

-- === Estados / Filas / Áreas ===
DECLARE @IdEstado INT, @IdFila INT, @IdArea INT, @IdExtraccion INT, @IdSP INT;

-- Estado: EstadoInicial
SET @IdEstado = 100000;
INSERT INTO dbo.Estados (IdEstado, [Default], Descripcion, IdVista, ArchivoJs, Eliminado)
VALUES (@IdEstado, 1, N'EstadoInicial', N'TestVistaLocal', NULL, 0);

INSERT INTO dbo.ConfiguracionesEstados (IdEstado, Configuracion)
VALUES (@IdEstado, N'{"leftMenu":true}');


-- Fila: Fila_1
SET @IdFila = 200000;
INSERT INTO dbo.Filas (IdFila, IdEstado, Descripcion)
VALUES (@IdFila, @IdEstado, N'Fila_1');


-- Area: Area_Chart
SET @IdArea = 300000;
INSERT INTO dbo.Areas (IdArea, IdFila, Descripcion)
VALUES (@IdArea, @IdFila, N'Area_Chart');


-- StoredProcedure
SET @IdSP = 500000;
IF NOT EXISTS(SELECT 1 FROM dbo.StoredProcedure WHERE Nombre = N'dbo.sp_TestVistaLocal_Chart')
BEGIN
    INSERT INTO dbo.StoredProcedure (IdStoredProcedure, Nombre, Eliminado)
    VALUES (@IdSP, N'dbo.sp_TestVistaLocal_Chart', 0);
END
ELSE
BEGIN
    SELECT @IdSP = IdStoredProcedure FROM dbo.StoredProcedure WHERE Nombre = N'dbo.sp_TestVistaLocal_Chart';
END

-- ExtraccionDatos
SET @IdExtraccion = 400000;
INSERT INTO dbo.ExtraccionesDatos (IdExtraccionDatos, IdStoredProcedure, IdTipoEjecucionSP, Eliminado)
VALUES (@IdExtraccion, @IdSP, 1, 0);


INSERT INTO dbo.ObjetosDeArea (IdArea, IdTipoObjetoDeArea, IdExtraccionDato, Eliminado)
VALUES (@IdArea, 1, @IdExtraccion, 0);


INSERT INTO dbo.ConfiguracionesObjetoDeArea (IdArea, Configuracion)
VALUES (@IdArea, N'{"seriesClickNavigateTo":"EstadoDetalle"}');


INSERT INTO dbo.MapeoParametros (IdArea, NombreParametro, ValorParametro)
VALUES (@IdArea, N'@Periodo', N'comboPeriodo');


INSERT INTO dbo.Handlers (IdArea, IdTipoHandler, Accion, Orden, Configuracion)
VALUES (@IdArea, 3, N'setTitulo', 1, N'{"texto":"Ventas"}');


-- Estado: EstadoDetalle
SET @IdEstado = 100001;
INSERT INTO dbo.Estados (IdEstado, [Default], Descripcion, IdVista, ArchivoJs, Eliminado)
VALUES (@IdEstado, 0, N'EstadoDetalle', N'TestVistaLocal', N'js/TestVistaLocal_Detalle.js', 0);

-- Fila: Fila_1
SET @IdFila = 200001;
INSERT INTO dbo.Filas (IdFila, IdEstado, Descripcion)
VALUES (@IdFila, @IdEstado, N'Fila_1');


-- Area: Area_Grid
SET @IdArea = 300001;
INSERT INTO dbo.Areas (IdArea, IdFila, Descripcion)
VALUES (@IdArea, @IdFila, N'Area_Grid');


-- StoredProcedure
SET @IdSP = 500001;
IF NOT EXISTS(SELECT 1 FROM dbo.StoredProcedure WHERE Nombre = N'dbo.sp_TestVistaLocal_Grid')
BEGIN
    INSERT INTO dbo.StoredProcedure (IdStoredProcedure, Nombre, Eliminado)
    VALUES (@IdSP, N'dbo.sp_TestVistaLocal_Grid', 0);
END
ELSE
BEGIN
    SELECT @IdSP = IdStoredProcedure FROM dbo.StoredProcedure WHERE Nombre = N'dbo.sp_TestVistaLocal_Grid';
END

-- ExtraccionDatos
SET @IdExtraccion = 400001;
INSERT INTO dbo.ExtraccionesDatos (IdExtraccionDatos, IdStoredProcedure, IdTipoEjecucionSP, Eliminado)
VALUES (@IdExtraccion, @IdSP, 1, 0);


INSERT INTO dbo.ObjetosDeArea (IdArea, IdTipoObjetoDeArea, IdExtraccionDato, Eliminado)
VALUES (@IdArea, 2, @IdExtraccion, 0);


INSERT INTO dbo.ConfiguracionesObjetoDeArea (IdArea, Configuracion)
VALUES (@IdArea, N'{"paging":true}');


INSERT INTO dbo.MapeoParametros (IdArea, NombreParametro, ValorParametro)
VALUES (@IdArea, N'@Periodo', N'comboPeriodo');


INSERT INTO dbo.Handlers (IdArea, IdTipoHandler, Accion, Orden, Configuracion)
VALUES (@IdArea, 1, N'semaforo', 1, N'{"column":"Revenue","ranges":[{"min":0,"max":10000,"color":"red"},{"min":10001,"max":50000,"color":"yellow"},{"min":50001,"max":null,"color":"green"}]}');


DECLARE @IdLM INT = (SELECT ISNULL(MAX(IdLeftMenu),0)+1 FROM dbo.LeftMenus);
INSERT INTO dbo.LeftMenus (IdLeftMenu, IdVista, Eliminado) VALUES (@IdLM, N'TestVistaLocal', 0);
DECLARE @IdSolapa INT = (SELECT ISNULL(MAX(IdSolapaLeftMenu),0)+1 FROM dbo.SolapasLeftMenu);
INSERT INTO dbo.SolapasLeftMenu (IdSolapaLeftMenu, IdLeftMenu, Descripcion) VALUES (@IdSolapa, @IdLM, N'General');


DECLARE @IdItem INT = (SELECT ISNULL(MAX(IdItemLeftMenu),0)+1 FROM dbo.ItemsLeftMenu);
INSERT INTO dbo.ItemsLeftMenu (IdItemLeftMenu, IdSolapaLeftMenu, Descripcion)
VALUES (@IdItem, @IdSolapa, N'Periodo');


DECLARE @IdItem INT = (SELECT ISNULL(MAX(IdItemLeftMenu),0)+1 FROM dbo.ItemsLeftMenu);
INSERT INTO dbo.ItemsLeftMenu (IdItemLeftMenu, IdSolapaLeftMenu, Descripcion)
VALUES (@IdItem, @IdSolapa, N'Gerencia');


DECLARE @IdWC INT = (SELECT ISNULL(MAX(IdWebControl),0)+1 FROM dbo.WebControls);
INSERT INTO dbo.WebControls (IdWebControl, IdVista, Tipo, Nombre)
VALUES (@IdWC, N'TestVistaLocal', N'Combo', N'comboPeriodo');


INSERT INTO dbo.ItemsWebControl (IdWebControl, Descripcion, StoredProcedureItems)
VALUES (@IdWC, N'Items', N'dbo.sp_ItemsPeriodo');

COMMIT
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    DECLARE @Msg NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@Msg, 16, 1);
END CATCH
