BEGIN TRY
    BEGIN TRAN

    -- === Versionado: calcular nueva versión antes de limpiar ===

    DECLARE @VistaId NVARCHAR(250) = N'vTestCopilot';
    DECLARE @PrevVersion DECIMAL(10,2) = NULL;

    -- Obtener versión previa (si existe) de forma segura (sin TRY_CONVERT)
    SELECT TOP (1)
    @PrevVersion =
    CASE
    WHEN VersionActual IS NULL THEN NULL
    WHEN ISNUMERIC(REPLACE(VersionActual, ',', '.')) = 1
    THEN CONVERT(DECIMAL(10,2), REPLACE(VersionActual, ',', '.'))
ELSE NULL
END
FROM dbo.Vistas WITH (NOLOCK)
WHERE IdVista = @VistaId
ORDER BY ISNULL(F_Create, FechaCreacion) DESC;

DECLARE @Version NVARCHAR(10);
IF @PrevVersion IS NULL
SET @Version = N'1.0';
ELSE
BEGIN
    -- incrementa 0.01 y normaliza a NVARCHAR
    SET @PrevVersion = CONVERT(DECIMAL(10,2), ROUND(@PrevVersion + 0.01, 2));
    SET @Version     = CONVERT(NVARCHAR(10), @PrevVersion);
END;

-- Auditoría
DECLARE @NowUTC   DATETIME2 = SYSUTCDATETIME();
DECLARE @TodayUTC DATE      = CAST(@NowUTC AS date);
DECLARE @WinUser  NVARCHAR(200) = SUSER_SNAME();

-- Limpiar vista si existe
EXEC dbo.eliminarVista @VistaId;

-- === Insert Vistas ===

INSERT INTO dbo.Vistas
(IdOwner, IdHistoricoCambio, Nombre, Descripcion, FechaCreacion,
VersionActual, VistaVersionado, ArchivoJs, IdVista, TipoVista, UsaSeguridad,
User_Create, F_Create, User_Update, F_Update, IdStringsConnectionMyVision, JsCode, Frente)
VALUES
(1, 1, @VistaId, N'Vista diaria con Calendar -> Grid - Vista Generada desde GitHub Copilot', @NowUTC,
@Version, @Version, N'', @VistaId, N'go', 0,
@WinUser, @TodayUTC, @WinUser, @TodayUTC, NULL, N'', N'');

-- === Backup de favoritos (si existen) ===
IF OBJECT_ID('dbo.FavoritosEngineNCA','U') IS NOT NULL
BEGIN
    IF OBJECT_ID('tempdb..#FavVista') IS NOT NULL DROP TABLE #FavVista;
    SELECT fe.Usuario, fe.IdVista
    INTO #FavVista
    FROM dbo.FavoritosEngineNCA fe WITH (NOLOCK)
    WHERE fe.IdVista = @VistaId;
END

IF OBJECT_ID('dbo.FavoritosEstadoEngineNCA','U') IS NOT NULL
BEGIN
    IF OBJECT_ID('tempdb..#FavEstado') IS NOT NULL DROP TABLE #FavEstado;
    SELECT fee.Usuario, e.Descripcion AS NombreEstado
    INTO #FavEstado
    FROM dbo.FavoritosEstadoEngineNCA fee WITH (NOLOCK)
    JOIN dbo.Estados e ON e.IdEstado = fee.IdEstado
    WHERE e.IdVista = @VistaId;
END

-- === Variables de trabajo / índices ===

DECLARE @IdEstado INT, @IdFila INT, @IdArea INT, @IdExtraccion INT, @IdSP INT, @IdObjetoDeArea INT, @IdCfg INT, @IdMapeo INT, @AccionId INT;
DECLARE @IDX_Estado INT      = ISNULL((SELECT MAX(IdEstado)          FROM dbo.Estados),0);
DECLARE @IDX_Fila   INT      = ISNULL((SELECT MAX(IdFila)            FROM dbo.Filas),0);
DECLARE @IDX_Area   INT      = ISNULL((SELECT MAX(IdArea)            FROM dbo.Areas),0);
DECLARE @IDX_Extr   INT      = ISNULL((SELECT MAX(IdExtraccionDato)  FROM dbo.ExtraccionesDatos),0);
DECLARE @IDX_SP     INT      = ISNULL((SELECT MAX(IdStoredProcedure) FROM dbo.StoredProcedure),0);
DECLARE @IDX_Obj    INT      = ISNULL((SELECT MAX(IdObjetoDeArea)    FROM dbo.ObjetosDeArea),0);
DECLARE @IDX_CfgObj INT      = ISNULL((SELECT MAX(Id)                FROM dbo.ConfiguracionesObjetoDeArea),0);
DECLARE @IDX_Mapeo  INT      = ISNULL((SELECT MAX(IdMapeoParametro)  FROM dbo.MapeoParametros),0);
DECLARE @IDX_Handler INT     = ISNULL((SELECT MAX(IdHandler)         FROM dbo.Handlers),0);

-- Estado: EstadoUnico
SET @IDX_Estado = @IDX_Estado + 1;
SET @IdEstado = @IDX_Estado;
INSERT INTO dbo.Estados (IdEstado, [Default], Descripcion, IdVista, ArchivoJs, LeftMenuFijo,
User_Create, F_Create, User_Update, F_Update)
VALUES (@IdEstado, 1, N'EstadoUnico', @VistaId, N'', 0,
@WinUser, @TodayUTC, @WinUser, @TodayUTC);

INSERT INTO dbo.ConfiguracionesEstados (IdEstado, Configuracion, User_Create, F_Update)
VALUES (@IdEstado, N'{"calendarFormat":"yyyyMMdd"}', @WinUser, @TodayUTC);

-- Fila: Fila_1
SET @IDX_Fila = @IDX_Fila + 1;
SET @IdFila = @IDX_Fila;
INSERT INTO dbo.Filas (IdFila, IdEstado, Nombre, User_Create, F_Create, User_Update, F_Update)
VALUES (@IdFila, @IdEstado, N'Fila_1', @WinUser, @TodayUTC, @WinUser, @TodayUTC);

-- Área: Area_Grid
SET @IDX_Area = @IDX_Area + 1;
SET @IdArea = @IDX_Area;
INSERT INTO dbo.Areas (IdArea, IdFila, Nombre, User_Create, F_Create, User_Update, F_Update)
VALUES (@IdArea, @IdFila, N'Area_Grid', @WinUser, @TodayUTC, @WinUser, @TodayUTC);

-- Extracción
SET @IDX_Extr = @IDX_Extr + 1;
SET @IdExtraccion = @IDX_Extr;
INSERT INTO dbo.ExtraccionesDatos (IdExtraccionDato, Descripcion, User_Create, F_Create, User_Update, F_Update)
VALUES (@IdExtraccion, N'Area_Grid', @WinUser, @TodayUTC, @WinUser, @TodayUTC);

-- StoredProcedure
SET @IDX_SP = @IDX_SP + 1;
SET @IdSP = @IDX_SP;
INSERT INTO dbo.StoredProcedure (IdStoredProcedure, Nombre, Descripcion, OrdenEjecucion,
IdStringConnection, IdExtraccionDato, IdTipoEjecucionSP,
User_Create, F_Create, User_Update, F_Update)
VALUES (@IdSP, N'dbo.storedProcedurePrueba', N'', 0,
14, @IdExtraccion, 1,
@WinUser, @TodayUTC, @WinUser, @TodayUTC);

-- Objeto de Área
SET @IDX_Obj = @IDX_Obj + 1;
SET @IdObjetoDeArea = @IDX_Obj;
INSERT INTO dbo.ObjetosDeArea (IdObjetoDeArea, IdArea, IdExtraccionDato, IdTipoObjetoDeArea, Nombre, UsaPaginador,
User_Create, F_Create, User_Update, F_Update)
VALUES (@IdObjetoDeArea, @IdArea, @IdExtraccion, 2, N'Area_Grid', 0,
@WinUser, @TodayUTC, @WinUser, @TodayUTC);

-- Configuración del Objeto de Área
SET @IDX_CfgObj = @IDX_CfgObj + 1;
SET @IdCfg = @IDX_CfgObj;
INSERT INTO dbo.ConfiguracionesObjetoDeArea (Id, IdObjetoDeArea, Configuracion,
User_Create, F_Create, User_Update, F_Update)
VALUES (@IdCfg, @IdObjetoDeArea, N'{"paging":true}',
@WinUser, @TodayUTC, @WinUser, @TodayUTC);

-- Mapeo de Parámetros
SET @IDX_Mapeo = @IDX_Mapeo + 1;
INSERT INTO dbo.MapeoParametros (IdMapeoParametro, IdEstado, NombreArea, Parametro, Variable, ValorDefault, IdStoredProcedure, VariableRequest,
User_Create, F_Create, User_Update, F_Update)
VALUES (@IDX_Mapeo, @IdEstado, N'Area_Grid', N'@Fecha', N'calendarFecha', NULL, @IdSP, NULL,
@WinUser, @TodayUTC, @WinUser, @TodayUTC);

-- === Restore de favoritos (si existían) ===
IF OBJECT_ID('tempdb..#FavVista') IS NOT NULL
BEGIN
    INSERT INTO dbo.FavoritosEngineNCA (Usuario, IdVista, User_Create, F_Create, User_Update, F_Update)
    SELECT f.Usuario, @VistaId, @WinUser, @TodayUTC, @WinUser, @TodayUTC
    FROM #FavVista f;
END

IF OBJECT_ID('tempdb..#FavEstado') IS NOT NULL
BEGIN
    INSERT INTO dbo.FavoritosEstadoEngineNCA (Usuario, IdEstado, User_Create, F_Create, User_Update, F_Update)
    SELECT f.Usuario, e.IdEstado, @WinUser, @TodayUTC, @WinUser, @TodayUTC
    FROM #FavEstado f
    JOIN dbo.Estados e ON e.Descripcion = f.NombreEstado AND e.IdVista = @VistaId;
END

COMMIT
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    DECLARE @Msg NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@Msg, 16, 1);
END CATCH

