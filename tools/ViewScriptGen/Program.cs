using System.Text;
using System.Text.Json;
using System.Linq;

record Catalogs(
    int StringsConnectionId,
    Dictionary<string, int> ObjectTypes,
    Dictionary<string, int> ExecutionTypes,
    Dictionary<string, int> HandlerTypes
);

record SpecVista(
    string IdVista,
    string Descripcion,
    string? ArchivoJs,
    string? JsCode,
    string TipoVista,
    bool UsaSeguridad,
    int? StringsConnectionId
);

record SpecEstado(
    string Descripcion,
    bool Default,
    string? ArchivoJs,
    string? ConfigEstadosJson,
    List<SpecFila>? Filas
);

record SpecFila(
    string Descripcion,
    List<SpecArea> Areas
);

record SpecArea(
    string Descripcion,
    string TipoObjeto,
    SpecExtraccion? Extraccion,
    string? ConfigObjetoJson,
    List<SpecMapeoParametro>? MapeoParametros,
    List<SpecHandler>? Handlers
);

record SpecExtraccion(
    string SpNombre,
    string ExecutionType
);

record SpecMapeoParametro(string OrigenUI, string DestinoSP);
record SpecHandler(string HandlerType, string? Accion, string? ConfigJson, int? Orden);

record Spec(
    SpecVista Vista,
    List<SpecEstado> Estados,
    List<SpecLeftMenu>? LeftMenu,
    List<SpecWebControl>? WebControls
);

record SpecLeftMenu(string Solapa, List<string> Items);
record SpecWebControl(string Tipo, string Nombre, string? ItemsSp);

class Program
{
    // JSON default obligatorio para GRILLA (modo mínimo)
    const string DefaultGridConfigJson =
        "{\"TituloArea\":\"[]\",\"BotonAyuda\":\"[]\",\"Render\":\"1\",\"RelacionLM\":\"\",\"CantRegistrosLoopExcel\":\"20\",\"MaximoTextoAncho\":\"50\",\"ColumnTotalsRenderMode\":\"RENDER_TIT\",\"ExportarExcel\":\"1\",\"Renglones\":\"50\",\"columnasExcelDescripcion\":\"true\",\"IdColumnaCodigo\":\"0\",\"IdColumnaDescripcion\":\"1\",\"Decimales\":\"0\",\"ExpandedRowLevels\":\"3\",\"SeparadorDecimal\":\",\",\"SeparadorMiles\":\".\",\"TitulosExcel\":\"\",\"AnchoColumnas\":\"0\",\"FixedColumnsMobile\":\"\",\"RowGroupSeparator\":\"false\",\"ColumnGroupSeparator\":\"false\",\"EnableHeader\":\"true\",\"EnableReference\":\"true\",\"EnableBody\":\"true\",\"TitulosFijos\":\"false\",\"AggregationTypes\":\"1\",\"ColumnAggregationType\":\"1\",\"MostrarCeros\":\"false\",\"IndizarPorColumna\":\"false\",\"VisualizarCampos\":\"false\",\"ResaltarRegistros\":\"false\",\"BotonesArea\":{\"Boton1\":{\"ID\":\"Expandir\",\"ToolTip\":\"Expandir\"},\"Boton2\":{\"ID\":\"Excel Sin Formato\",\"ToolTip\":\"Excel Sin Formato\"}},\"Hashtag\":\"\", \"UnidadAltoFila\": \"817\"}";

    static int Main(string[] args)
    {
        try
        {
            // Modo archivo (.json) o modo frase
            bool hasArg = args.Length > 0;
            bool isJsonArg = hasArg && args[0].EndsWith(".json", StringComparison.OrdinalIgnoreCase) && File.Exists(args[0]);

            var inputSpecPath = isJsonArg ? args[0] : "specs/sample_view.json";
            var outputDir = args.Length > 1 ? args[1] : "sql/generated";
            var catalogsPath = args.Length > 2 ? args[2] : "tools/ViewScriptGen/catalogs.json";

            Directory.CreateDirectory(outputDir);

            // Cargar catálogos
            var cat = JsonSerializer.Deserialize<Catalogs>(File.ReadAllText(catalogsPath), new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? throw new Exception("No pude deserializar catalogs.json");

            // Obtener Spec
            Spec spec;
            if (isJsonArg)
            {
                spec = JsonSerializer.Deserialize<Spec>(File.ReadAllText(inputSpecPath), new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? throw new Exception("No pude deserializar el spec.json");
            }
            else
            {
                var phrase = hasArg ? args[0] : "";
                spec = SpecFromPhrase(phrase);
            }

            // Normaliza / valida (modo sencillo)
            spec = NormalizeAndGuard(spec, cat);

            // Genera SQL
            var sql = GenerateSql(spec, cat);
            var fileName = $"{Sanitize(spec.Vista.IdVista)}_{DateTime.UtcNow:yyyyMMddHHmmss}.sql";
            var outPath = Path.Combine(outputDir, fileName);

            sql = NormalizeLeftPadding(sql);
            sql = FormatSql(sql);

            File.WriteAllText(outPath, sql, Encoding.UTF8);
            Console.WriteLine("OK -> " + outPath);

            // Abre el archivo SOLO en VS Code (misma ventana)
            TryOpenInEditor(outPath);

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("ERROR: " + ex.Message);
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    static string GenerateSql(Spec spec, Catalogs cat)
    {
        var v = spec.Vista;
        var sb = new StringBuilder();

        // Helpers
        string ArchivoJsVista = string.IsNullOrWhiteSpace(v.ArchivoJs) ? "" : v.ArchivoJs;
        string JsCodeVista = "";   // siempre en blanco
        string FrenteVista = "";   // siempre en blanco

        // endurecer el batch
        sb.AppendLine("SET XACT_ABORT ON; SET NOCOUNT ON;");
        sb.AppendLine("BEGIN TRY");
        sb.AppendLine("BEGIN TRAN");
        sb.AppendLine();

        sb.AppendLine("-- === Versionado y auditoría previa ===");
        sb.AppendLine($@"
            DECLARE @VistaId NVARCHAR(250) = {Sql(v.IdVista)};
            DECLARE @PrevVersion DECIMAL(10,2) = NULL;

            -- Valores de auditoría previos (si la vista ya existe)
            DECLARE @OldUserCreate NVARCHAR(200) = NULL;
            DECLARE @OldFCreate    DATE          = NULL;
            IF EXISTS (SELECT 1 FROM dbo.Vistas WITH (NOLOCK) WHERE IdVista = @VistaId)
            BEGIN
                SELECT TOP (1)
                    @OldUserCreate = User_Create,
                    @OldFCreate    = F_Create
                FROM dbo.Vistas WITH (NOLOCK)
                WHERE IdVista = @VistaId
                ORDER BY ISNULL(F_Create, FechaCreacion) DESC;

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
            END

            DECLARE @Version NVARCHAR(10);
            IF @PrevVersion IS NULL
                SET @Version = N'1.0';
            ELSE
            BEGIN
                SET @PrevVersion = CONVERT(DECIMAL(10,2), ROUND(@PrevVersion + 0.01, 2));
                SET @Version     = CONVERT(NVARCHAR(10), @PrevVersion);
            END;

            -- Auditoría runtime
            DECLARE @NowUTC   DATETIME2 = SYSUTCDATETIME();
            DECLARE @TodayUTC DATE      = CAST(@NowUTC AS date);
            DECLARE @WinUser  NVARCHAR(200) = SUSER_SNAME();
        ");

        sb.AppendLine();
        sb.AppendLine("-- Limpiar vista si existe");
        sb.AppendLine("EXEC dbo.eliminarVista @VistaId;");
        sb.AppendLine();

        sb.AppendLine("-- === Insert Vistas (preservando User_Create/F_Create si existían) ===");
        sb.AppendLine($@"
            INSERT INTO dbo.Vistas
            (IdOwner, IdHistoricoCambio, Nombre, Descripcion, FechaCreacion,
             VersionActual, VistaVersionado, ArchivoJs, IdVista, TipoVista, UsaSeguridad,
             User_Create, F_Create, User_Update, F_Update, IdStringsConnectionMyVision, JsCode, Frente)
            VALUES
            (1, 1, @VistaId, {Sql(v.Descripcion)}, @NowUTC,
             @Version, @Version, {Sql(ArchivoJsVista)}, @VistaId, {Sql(v.TipoVista)}, {(v.UsaSeguridad ? 1 : 0)},
             ISNULL(@OldUserCreate, @WinUser), ISNULL(@OldFCreate, @TodayUTC), @WinUser, @TodayUTC, NULL, {Sql(JsCodeVista)}, {Sql(FrenteVista)});
        ");

        sb.AppendLine(@"
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
        ");

        sb.AppendLine("-- === Variables de trabajo / índices ===");
        sb.AppendLine(@"
            DECLARE @IdEstado INT, @IdFila INT, @IdArea INT, @IdExtraccion INT, @IdSP INT, @IdObjetoDeArea INT, @IdCfg INT, @IdMapeo INT, @AccionId INT;
            DECLARE @IDX_Estado  INT = ISNULL((SELECT MAX(IdEstado)          FROM dbo.Estados),0);
            DECLARE @IDX_Fila    INT = ISNULL((SELECT MAX(IdFila)            FROM dbo.Filas),0);
            DECLARE @IDX_Area    INT = ISNULL((SELECT MAX(IdArea)            FROM dbo.Areas),0);
            DECLARE @IDX_Extr    INT = ISNULL((SELECT MAX(IdExtraccionDato)  FROM dbo.ExtraccionesDatos),0);
            DECLARE @IDX_SP      INT = ISNULL((SELECT MAX(IdStoredProcedure) FROM dbo.StoredProcedure),0);
            DECLARE @IDX_Obj     INT = ISNULL((SELECT MAX(IdObjetoDeArea)    FROM dbo.ObjetosDeArea),0);
            DECLARE @IDX_CfgObj  INT = ISNULL((SELECT MAX(Id)                FROM dbo.ConfiguracionesObjetoDeArea),0);
            DECLARE @IDX_Mapeo   INT = ISNULL((SELECT MAX(IdMapeoParametro)  FROM dbo.MapeoParametros),0);
            DECLARE @IDX_Handler INT = ISNULL((SELECT MAX(IdHandler)         FROM dbo.Handlers),0);
            DECLARE @IDX_ParamSP INT = ISNULL((SELECT MAX(IdParametroSP)     FROM dbo.ParametrosSP),0);
        ");

        foreach (var e in spec.Estados)
        {
            string archivoJsEstado = string.IsNullOrWhiteSpace(e.ArchivoJs) ? "" : e.ArchivoJs;

            sb.AppendLine($@"
                -- Estado: {e.Descripcion}
                SET @IDX_Estado = @IDX_Estado + 1;
                SET @IdEstado   = @IDX_Estado;
                INSERT INTO dbo.Estados (IdEstado, [Default], Descripcion, IdVista, ArchivoJs, LeftMenuFijo,
                                         User_Create, F_Create, User_Update, F_Update)
                VALUES (@IdEstado, {(e.Default ? 1 : 0)}, {Sql(e.Descripcion)}, @VistaId, {Sql(archivoJsEstado)}, 0,
                        @WinUser, @TodayUTC, @WinUser, @TodayUTC);
            ");

            if (!string.IsNullOrWhiteSpace(e.ConfigEstadosJson))
            {
                sb.AppendLine($@"
                    INSERT INTO dbo.ConfiguracionesEstados (IdEstado, Configuracion, User_Create, F_Update)
                    VALUES (@IdEstado, {Sql(e.ConfigEstadosJson)}, @WinUser, @TodayUTC);
                ");
            }

            foreach (var f in e.Filas ?? new())
            {
                sb.AppendLine($@"
                    -- Fila: {f.Descripcion}
                    SET @IDX_Fila = @IDX_Fila + 1;
                    SET @IdFila   = @IDX_Fila;
                    INSERT INTO dbo.Filas (IdFila, IdEstado, Nombre, Ancho, Altura, User_Create, F_Create, User_Update, F_Update)
                    VALUES (@IdFila, @IdEstado, {Sql(f.Descripcion)}, 0, 0, @WinUser, @TodayUTC, @WinUser, @TodayUTC);
                ");

                foreach (var a in f.Areas)
                {
                    sb.AppendLine($@"
                        -- Área: {a.Descripcion}
                        SET @IDX_Area = @IDX_Area + 1;
                        SET @IdArea   = @IDX_Area;
                        INSERT INTO dbo.Areas (IdArea, IdFila, Nombre, Ancho, Altura, User_Create, F_Create, User_Update, F_Update)
                        VALUES (@IdArea, @IdFila, {Sql(a.Descripcion)}, 0, 0, @WinUser, @TodayUTC, @WinUser, @TodayUTC);
                    ");

                    if (a.Extraccion is not null)
                    {
                        var tipoEjec = cat.ExecutionTypes[a.Extraccion.ExecutionType.ToLower()];
                        sb.AppendLine($@"
                            -- Extracción
                            SET @IDX_Extr = @IDX_Extr + 1;
                            SET @IdExtraccion = @IDX_Extr;
                            INSERT INTO dbo.ExtraccionesDatos (IdExtraccionDato, Descripcion, User_Create, F_Create, User_Update, F_Update)
                            VALUES (@IdExtraccion, {Sql(a.Extraccion.SpNombre)}, @WinUser, @TodayUTC, @WinUser, @TodayUTC);

                            -- StoredProcedure
                            SET @IDX_SP = @IDX_SP + 1;
                            SET @IdSP   = @IDX_SP;
                            INSERT INTO dbo.StoredProcedure (IdStoredProcedure, Nombre, Descripcion, OrdenEjecucion,
                                                             IdStringConnection, IdExtraccionDato, IdTipoEjecucionSP,
                                                             User_Create, F_Create, User_Update, F_Update)
                            VALUES (@IdSP, {Sql(a.Extraccion.SpNombre)}, N'', 0,
                                    {(spec.Vista.StringsConnectionId ?? cat.StringsConnectionId)}, @IdExtraccion, {tipoEjec},
                                    @WinUser, @TodayUTC, @WinUser, @TodayUTC);

                            -- ====== Descubrir parámetros del SP y pre-cargar ParametrosSP / MapeoParametros (blancos) ======
                            DECLARE @ProcObjectId INT = OBJECT_ID({Sql(a.Extraccion.SpNombre)});
                            IF @ProcObjectId IS NOT NULL
                            BEGIN
                                IF OBJECT_ID('tempdb..#params') IS NOT NULL DROP TABLE #params;

                                SELECT
                                    ROW_NUMBER() OVER (ORDER BY prm.parameter_id) AS rn,
                                    prm.name AS ParamName,
                                    t.name   AS TypeName,
                                    prm.max_length AS MaxLen
                                INTO #params
                                FROM sys.parameters prm
                                JOIN sys.types t ON prm.user_type_id = t.user_type_id
                                WHERE prm.object_id = @ProcObjectId
                                  AND prm.is_output = 0;

                                -- ParametrosSP
                                INSERT INTO dbo.ParametrosSP
                                    (IdParametroSP, IdStoredProcedure, Nombre, UsaComillas, EsMyVision,
                                     User_Create, F_Create, User_Update, F_Update, TipoParametro)
                                SELECT
                                    @IDX_ParamSP + rn,
                                    @IdSP,
                                    RIGHT(ParamName, CASE WHEN LEFT(ParamName,1)='@' THEN LEN(ParamName)-1 ELSE LEN(ParamName) END),
                                    CASE 
                                        WHEN TypeName IN ('bit','tinyint','smallint','int','bigint','decimal','numeric','smallmoney','money','real','float') THEN 0
                                        ELSE 1
                                    END,
                                    0, @WinUser, @TodayUTC, @WinUser, @TodayUTC,
                                    TypeName
                                FROM #params;

                                SET @IDX_ParamSP = @IDX_ParamSP + (SELECT COUNT(*) FROM #params);

                                -- MapeoParametros (en blanco)
                                INSERT INTO dbo.MapeoParametros
                                    (IdMapeoParametro, IdEstado, NombreArea, Parametro, Variable, ValorDefault,
                                     IdStoredProcedure, VariableRequest, User_Create, F_Create, User_Update, F_Update)
                                SELECT
                                    @IDX_Mapeo + rn,
                                    @IdEstado,
                                    {Sql(a.Descripcion)},
                                    RIGHT(ParamName, CASE WHEN LEFT(ParamName,1)='@' THEN LEN(ParamName)-1 ELSE LEN(ParamName) END),
                                    N'', NULL, @IdSP, NULL,
                                    @WinUser, @TodayUTC, @WinUser, @TodayUTC
                                FROM #params;

                                SET @IDX_Mapeo = @IDX_Mapeo + (SELECT COUNT(*) FROM #params);

                                DROP TABLE #params;
                            END
                        ");
                    }
                    else
                    {
                        sb.AppendLine("SET @IdExtraccion = NULL; SET @IdSP = NULL;");
                    }

                    var tipoObj = cat.ObjectTypes[a.TipoObjeto.ToLower()];
                    sb.AppendLine($@"
                        -- Objeto de Área
                        SET @IDX_Obj = @IDX_Obj + 1;
                        SET @IdObjetoDeArea = @IDX_Obj;
                        INSERT INTO dbo.ObjetosDeArea (IdObjetoDeArea, IdArea, IdExtraccionDato, IdTipoObjetoDeArea, Nombre, UsaPaginador,
                                                       User_Create, F_Create, User_Update, F_Update)
                        VALUES (@IdObjetoDeArea, @IdArea, @IdExtraccion, {tipoObj}, {Sql(a.Descripcion)}, 0,
                                @WinUser, @TodayUTC, @WinUser, @TodayUTC);
                    ");

                    // Siempre usar JSON default para grilla (modo mínimo)
                    sb.AppendLine($@"
                        -- Configuración del Objeto de Área (default grilla)
                        SET @IDX_CfgObj = @IDX_CfgObj + 1;
                        SET @IdCfg = @IDX_CfgObj;
                        INSERT INTO dbo.ConfiguracionesObjetoDeArea (Id, IdObjetoDeArea, Configuracion,
                                                                     User_Create, F_Create, User_Update, F_Update)
                        VALUES (@IdCfg, @IdObjetoDeArea, {Sql(DefaultGridConfigJson)},
                                @WinUser, @TodayUTC, @WinUser, @TodayUTC);
                    ");

                    // Si el spec trajera mapeos explícitos, agréguelos solo si no existen aún
                    foreach (var m in a.MapeoParametros ?? new())
                    {
                        var dest = (m.DestinoSP ?? "").Trim();
                        var destNoAt = dest.TrimStart('@');

                        sb.AppendLine($@"
                            -- Mapeo adicional desde spec (idempotente)
                            IF NOT EXISTS (
                                SELECT 1 FROM dbo.MapeoParametros
                                WHERE IdStoredProcedure = @IdSP AND IdEstado = @IdEstado
                                  AND NombreArea = {Sql(a.Descripcion)} AND Parametro = {Sql(destNoAt)}
                            )
                            BEGIN
                                -- Asegurar ParametrosSP
                                IF NOT EXISTS (
                                    SELECT 1 FROM dbo.ParametrosSP
                                    WHERE IdStoredProcedure = @IdSP AND Nombre = {Sql(destNoAt)}
                                )
                                BEGIN
                                    SET @IDX_ParamSP = @IDX_ParamSP + 1;
                                    INSERT INTO dbo.ParametrosSP
                                        (IdParametroSP, IdStoredProcedure, Nombre, UsaComillas, EsMyVision, User_Create, F_Create, User_Update, F_Update, TipoParametro)
                                    VALUES
                                        (@IDX_ParamSP, @IdSP, {Sql(destNoAt)}, 1, 0, @WinUser, @TodayUTC, @WinUser, @TodayUTC, N'');
                                END

                                SET @IDX_Mapeo = @IDX_Mapeo + 1;
                                INSERT INTO dbo.MapeoParametros
                                    (IdMapeoParametro, IdEstado, NombreArea, Parametro, Variable, ValorDefault, IdStoredProcedure, VariableRequest, User_Create, F_Create, User_Update, F_Update)
                                VALUES
                                    (@IDX_Mapeo, @IdEstado, {Sql(a.Descripcion)}, {Sql(destNoAt)}, {Sql(m.OrigenUI ?? "")}, NULL, @IdSP, NULL, @WinUser, @TodayUTC, @WinUser, @TodayUTC);
                            END
                        ");
                    }

                    foreach (var h in a.Handlers ?? new())
                    {
                        var t = cat.HandlerTypes[h.HandlerType];
                        var orden = h.Orden ?? 1;
                        sb.AppendLine($@"
                            -- Handler
                            SET @IDX_Handler = @IDX_Handler + 1;
                            SELECT @AccionId = IdAccionHandler FROM dbo.AccionesHandler WHERE UPPER(Accion) = UPPER({Sql(h.Accion ?? "")});
                            IF @AccionId IS NOT NULL
                            BEGIN
                                INSERT INTO dbo.Handlers (IdHandler, IdObjetoDeArea, IdTipoHandler, IdAccionHandler, Parametros, IdsAplicarAccion, Orden,
                                                          User_Create, F_Create, User_Update, F_Update)
                                VALUES (@IDX_Handler, @IdObjetoDeArea, {t}, @AccionId, {Sql(h.ConfigJson)}, N'All', {orden},
                                        @WinUser, @TodayUTC, @WinUser, @TodayUTC);
                            END
                        ");
                    }
                }
            }
        }

        sb.AppendLine(@"
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
        ");

        sb.AppendLine("COMMIT");
        sb.AppendLine("END TRY");
        sb.AppendLine("BEGIN CATCH");
        sb.AppendLine("    IF @@TRANCOUNT > 0 ROLLBACK;");
        sb.AppendLine("    DECLARE @Msg NVARCHAR(4000) = ERROR_MESSAGE();");
        sb.AppendLine("    RAISERROR(@Msg, 16, 1);");
        sb.AppendLine("END CATCH");

        return sb.ToString();
    }

    static string Sql(string? s) => s is null ? "NULL" : "N'" + s.Replace("'", "''") + "'";

    static string Sanitize(string s)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(s.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
    }

    static string FormatSql(string sql)
    {
        var lines = sql.Replace("\r\n", "\n").Split('\n');
        var outLines = new List<string>(lines.Length);
        int indent = 0;

        bool Decreases(string s) =>
            s.TrimStart().StartsWith("END", StringComparison.OrdinalIgnoreCase) ||
            s.TrimStart().StartsWith("ELSE", StringComparison.OrdinalIgnoreCase) ||
            s.TrimStart().StartsWith("END CATCH", StringComparison.OrdinalIgnoreCase);

        bool Increases(string s) =>
            s.TrimEnd().EndsWith("BEGIN", StringComparison.OrdinalIgnoreCase) ||
            s.TrimStart().StartsWith("BEGIN TRY", StringComparison.OrdinalIgnoreCase) ||
            s.TrimStart().StartsWith("BEGIN CATCH", StringComparison.OrdinalIgnoreCase) ||
            (s.TrimStart().StartsWith("IF ", StringComparison.OrdinalIgnoreCase) && s.TrimEnd().EndsWith("BEGIN", StringComparison.OrdinalIgnoreCase));

        foreach (var raw in lines)
        {
            var s = raw.TrimEnd();

            if (string.IsNullOrWhiteSpace(s))
            {
                if (outLines.Count == 0 || string.IsNullOrWhiteSpace(outLines[^1])) continue;
                outLines.Add("");
                continue;
            }

            if (Decreases(s)) indent = Math.Max(0, indent - 1);
            outLines.Add(new string(' ', indent * 4) + s);
            if (Increases(s)) indent++;
        }

        for (int i = outLines.Count - 1; i >= 1; i--)
            if (string.IsNullOrWhiteSpace(outLines[i]) && string.IsNullOrWhiteSpace(outLines[i - 1]))
                outLines.RemoveAt(i);

        return string.Join(Environment.NewLine, outLines) + Environment.NewLine;
    }

    static string NormalizeLeftPadding(string sql)
    {
        var lines = sql.Replace("\r\n", "\n").Split('\n');
        var sb = new StringBuilder(lines.Length * 64);
        foreach (var line in lines)
            sb.AppendLine(line.TrimStart());
        return sb.ToString();
    }

    static int Token4(string seed) => (Math.Abs(seed.GetHashCode()) % 9000) + 1000;

    static Spec NormalizeAndGuard(Spec s, Catalogs cat)
    {
        if (s?.Vista == null || string.IsNullOrWhiteSpace(s.Vista.IdVista))
            throw new Exception("Falta IdVista.");

        // token estable de 4 dígitos por IdVista
        var tok = Token4(s.Vista.IdVista);

        // Estructura 1-1-1-1
        if (s.Estados == null || s.Estados.Count == 0) s = s with { Estados = new List<SpecEstado>() };
        if (s.Estados.Count > 1) throw new Exception("Modo sencillo: solo 1 estado permitido.");

        var estado = s.Estados.Count == 1 ? s.Estados[0] : new SpecEstado($"Pantalla{tok}", true, "", null, new List<SpecFila>());
        var fila   = (estado.Filas is { Count: > 0 }) ? estado.Filas[0] : new SpecFila($"Pantalla{tok}F0", new List<SpecArea>());
        var area   = (fila.Areas  is { Count: > 0 }) ? fila.Areas[0]   : new SpecArea($"area_{tok}", "grilla", null, null, null, null);

        // tipo obligatorio grilla
        var tipo = (area.TipoObjeto ?? "").Trim().ToLower();
        if (tipo is "table" or "grid" or "tabla") tipo = "grilla";
        if (tipo != "grilla") throw new Exception("Modo sencillo: el tipo de objeto debe ser 'grilla'.");

        // SP obligatorio
        if (area.Extraccion == null || string.IsNullOrWhiteSpace(area.Extraccion.SpNombre))
            throw new Exception("Falta el nombre de Stored Procedure para la grilla.");

        string exec = (area.Extraccion.ExecutionType ?? "siempre").ToLower();
        if (!cat.ExecutionTypes.ContainsKey(exec)) exec = "siempre";

        // En modo mínimo, forzar defaults y NO aceptar ConfigObjetoJson custom
        area = area with
        {
            TipoObjeto = "grilla",
            Extraccion = new SpecExtraccion(area.Extraccion.SpNombre, exec),
            ConfigObjetoJson = null,          // fuerza JSON default
            MapeoParametros  = (area.MapeoParametros is { Count: > 0 }) ? area.MapeoParametros : null,
            Handlers         = null           // sin handlers por default
        };

        fila   = new SpecFila($"Pantalla{tok}F0", new List<SpecArea> { area }); // nombre estilo Designer
        estado = estado with { Descripcion = $"Pantalla{tok}", Default = true, ArchivoJs = "", Filas = new List<SpecFila> { fila } };

        var v = s.Vista;
        s = s with
        {
            Vista = new SpecVista(
                v.IdVista,
                string.IsNullOrWhiteSpace(v.Descripcion) ? $"Vista mínima: {v.IdVista}" : v.Descripcion,
                "",  // ArchivoJs
                "",  // JsCode
                string.IsNullOrWhiteSpace(v.TipoVista) ? "site" : v.TipoVista, // default 'site'
                v.UsaSeguridad,
                v.StringsConnectionId ?? cat.StringsConnectionId
            ),
            Estados = new List<SpecEstado> { estado },
            LeftMenu = null,
            WebControls = null
        };

        return s;
    }

    static Spec SpecFromPhrase(string phrase)
    {
        if (string.IsNullOrWhiteSpace(phrase))
            throw new Exception("No recibí una frase. Ejemplo: Crea una vista mínima: IdVista vVentasDiarias, grilla, SP dbo.sp_VentasDiarias");

        // IdVista: "IdVista <id>" o "vista <id>"
        var mId1 = System.Text.RegularExpressions.Regex.Match(phrase, @"\bIdVista\s+([A-Za-z0-9_]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        var mId2 = System.Text.RegularExpressions.Regex.Match(phrase, @"\bvista\s+([A-Za-z0-9_]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        var idVista = mId1.Success ? mId1.Groups[1].Value : (mId2.Success ? mId2.Groups[1].Value : null);

        // SP: "SP <schema.obj>"
        var mSp = System.Text.RegularExpressions.Regex.Match(phrase, @"\bSP\s+([A-Za-z0-9_\.\[\]]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        var sp = mSp.Success ? mSp.Groups[1].Value : null;

        if (string.IsNullOrWhiteSpace(idVista)) throw new Exception("Falta IdVista en la frase. Ej: IdVista vVentasDiarias");
        if (string.IsNullOrWhiteSpace(sp))      throw new Exception("Falta SP en la frase. Ej: SP dbo.sp_VentasDiarias");

        var tok = Token4(idVista);

        var vista = new SpecVista(idVista, $"Vista mínima: {idVista}", "", "", "site", false, null);
        var area  = new SpecArea($"area_{tok}", "grilla", new SpecExtraccion(sp, "siempre"), null, null, null);
        var fila  = new SpecFila($"Pantalla{tok}F0", new List<SpecArea> { area });
        var est   = new SpecEstado($"Pantalla{tok}", true, "", null, new List<SpecFila> { fila });

        return new Spec(vista, new List<SpecEstado> { est }, null, null);
    }

    static void TryOpenInEditor(string path)
    {
        try
        {
            // Abrir SIEMPRE en la ventana actual de VS Code
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "code",
                Arguments = "--reuse-window -g \"" + path + "\"",
                UseShellExecute = false
            };
            System.Diagnostics.Process.Start(psi);
        }
        catch
        {
            // Nada de fallback (no abrir SSMS/ADS). Solo informamos ruta.
            Console.WriteLine("Archivo generado: " + path);
        }
    }
}
