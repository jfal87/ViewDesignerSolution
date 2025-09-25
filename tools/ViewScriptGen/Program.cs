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
    static int Main(string[] args)
    {
        try
        {
            var inputSpec = args.Length > 0 ? args[0] : "specs/sample_view.json";
            var outputDir = args.Length > 1 ? args[1] : "sql/generated";
            var catalogsPath = args.Length > 2 ? args[2] : "tools/ViewScriptGen/catalogs.json";

            Directory.CreateDirectory(outputDir);

            var spec = JsonSerializer.Deserialize<Spec>(File.ReadAllText(inputSpec), new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? throw new Exception("No pude deserializar el spec.json");

            var cat = JsonSerializer.Deserialize<Catalogs>(File.ReadAllText(catalogsPath), new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? throw new Exception("No pude deserializar catalogs.json");

            var sql = GenerateSql(spec, cat);
            var fileName = $"{Sanitize(spec.Vista.IdVista)}_{DateTime.UtcNow:yyyyMMddHHmmss}.sql";
            var outPath = Path.Combine(outputDir, fileName);

            sql = NormalizeLeftPadding(sql);
            sql = FormatSql(sql);

            File.WriteAllText(outPath, sql, Encoding.UTF8);
            Console.WriteLine("OK -> " + outPath);

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

        // Helpers para valores “no nulos”
        string ArchivoJsVista = string.IsNullOrWhiteSpace(v.ArchivoJs) ? "" : v.ArchivoJs;
        string JsCodeVista = "";   // siempre en blanco
        string FrenteVista = "";   // siempre en blanco

        sb.AppendLine("BEGIN TRY");
        sb.AppendLine("BEGIN TRAN");
        sb.AppendLine();

        sb.AppendLine("-- === Versionado: calcular nueva versión antes de limpiar ===");
        sb.AppendLine($@"
            DECLARE @VistaId NVARCHAR(250) = {Sql(v.IdVista)};
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
        ");

        sb.AppendLine();
        sb.AppendLine("-- Limpiar vista si existe");
        sb.AppendLine("EXEC dbo.eliminarVista @VistaId;");
        sb.AppendLine();

        sb.AppendLine("-- === Insert Vistas ===");
        sb.AppendLine($@"
            INSERT INTO dbo.Vistas
            (IdOwner, IdHistoricoCambio, Nombre, Descripcion, FechaCreacion,
             VersionActual, VistaVersionado, ArchivoJs, IdVista, TipoVista, UsaSeguridad,
             User_Create, F_Create, User_Update, F_Update, IdStringsConnectionMyVision, JsCode, Frente)
            VALUES
            (1, 1, @VistaId, {Sql(v.Descripcion)}, @NowUTC,
             @Version, @Version, {Sql(ArchivoJsVista)}, @VistaId, {Sql(v.TipoVista)}, {(v.UsaSeguridad ? 1 : 0)},
             @WinUser, @TodayUTC, @WinUser, @TodayUTC, NULL, " + Sql(JsCodeVista) + ", " + Sql(FrenteVista) + @");
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
            DECLARE @IDX_Estado INT      = ISNULL((SELECT MAX(IdEstado)          FROM dbo.Estados),0);
            DECLARE @IDX_Fila   INT      = ISNULL((SELECT MAX(IdFila)            FROM dbo.Filas),0);
            DECLARE @IDX_Area   INT      = ISNULL((SELECT MAX(IdArea)            FROM dbo.Areas),0);
            DECLARE @IDX_Extr   INT      = ISNULL((SELECT MAX(IdExtraccionDato)  FROM dbo.ExtraccionesDatos),0);
            DECLARE @IDX_SP     INT      = ISNULL((SELECT MAX(IdStoredProcedure) FROM dbo.StoredProcedure),0);
            DECLARE @IDX_Obj    INT      = ISNULL((SELECT MAX(IdObjetoDeArea)    FROM dbo.ObjetosDeArea),0);
            DECLARE @IDX_CfgObj INT      = ISNULL((SELECT MAX(Id)                FROM dbo.ConfiguracionesObjetoDeArea),0);
            DECLARE @IDX_Mapeo  INT      = ISNULL((SELECT MAX(IdMapeoParametro)  FROM dbo.MapeoParametros),0);
            DECLARE @IDX_Handler INT     = ISNULL((SELECT MAX(IdHandler)         FROM dbo.Handlers),0);
        ");

        foreach (var e in spec.Estados)
        {
            string archivoJsEstado = string.IsNullOrWhiteSpace(e.ArchivoJs) ? "" : e.ArchivoJs;

            sb.AppendLine($@"
                -- Estado: {e.Descripcion}
                SET @IDX_Estado = @IDX_Estado + 1;
                SET @IdEstado = @IDX_Estado;
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
                    SET @IdFila = @IDX_Fila;
                    INSERT INTO dbo.Filas (IdFila, IdEstado, Nombre, User_Create, F_Create, User_Update, F_Update)
                    VALUES (@IdFila, @IdEstado, {Sql(f.Descripcion)}, @WinUser, @TodayUTC, @WinUser, @TodayUTC);
                ");

                foreach (var a in f.Areas)
                {
                    sb.AppendLine($@"
                        -- Área: {a.Descripcion}
                        SET @IDX_Area = @IDX_Area + 1;
                        SET @IdArea = @IDX_Area;
                        INSERT INTO dbo.Areas (IdArea, IdFila, Nombre, User_Create, F_Create, User_Update, F_Update)
                        VALUES (@IdArea, @IdFila, {Sql(a.Descripcion)}, @WinUser, @TodayUTC, @WinUser, @TodayUTC);
                    ");

                    if (a.Extraccion is not null)
                    {
                        var tipoEjec = cat.ExecutionTypes[a.Extraccion.ExecutionType.ToLower()];
                        sb.AppendLine($@"
                            -- Extracción
                            SET @IDX_Extr = @IDX_Extr + 1;
                            SET @IdExtraccion = @IDX_Extr;
                            INSERT INTO dbo.ExtraccionesDatos (IdExtraccionDato, Descripcion, User_Create, F_Create, User_Update, F_Update)
                            VALUES (@IdExtraccion, {Sql(a.Descripcion)}, @WinUser, @TodayUTC, @WinUser, @TodayUTC);

                            -- StoredProcedure
                            SET @IDX_SP = @IDX_SP + 1;
                            SET @IdSP = @IDX_SP;
                            INSERT INTO dbo.StoredProcedure (IdStoredProcedure, Nombre, Descripcion, OrdenEjecucion,
                                                             IdStringConnection, IdExtraccionDato, IdTipoEjecucionSP,
                                                             User_Create, F_Create, User_Update, F_Update)
                            VALUES (@IdSP, {Sql(a.Extraccion.SpNombre)}, N'', 0,
                                    {(spec.Vista.StringsConnectionId ?? cat.StringsConnectionId)}, @IdExtraccion, {tipoEjec},
                                    @WinUser, @TodayUTC, @WinUser, @TodayUTC);
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

                    if (!string.IsNullOrWhiteSpace(a.ConfigObjetoJson))
                    {
                        sb.AppendLine($@"
                            -- Configuración del Objeto de Área
                            SET @IDX_CfgObj = @IDX_CfgObj + 1;
                            SET @IdCfg = @IDX_CfgObj;
                            INSERT INTO dbo.ConfiguracionesObjetoDeArea (Id, IdObjetoDeArea, Configuracion,
                                                                         User_Create, F_Create, User_Update, F_Update)
                            VALUES (@IdCfg, @IdObjetoDeArea, {Sql(a.ConfigObjetoJson)},
                                    @WinUser, @TodayUTC, @WinUser, @TodayUTC);
                        ");
                    }

                    foreach (var m in a.MapeoParametros ?? new())
                    {
                        sb.AppendLine($@"
                            -- Mapeo de Parámetros
                            SET @IDX_Mapeo = @IDX_Mapeo + 1;
                            INSERT INTO dbo.MapeoParametros (IdMapeoParametro, IdEstado, NombreArea, Parametro, Variable, ValorDefault, IdStoredProcedure, VariableRequest,
                                                             User_Create, F_Create, User_Update, F_Update)
                            VALUES (@IDX_Mapeo, @IdEstado, {Sql(a.Descripcion)}, {Sql(m.DestinoSP)}, {Sql(m.OrigenUI)}, NULL, @IdSP, NULL,
                                    @WinUser, @TodayUTC, @WinUser, @TodayUTC);
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

    static string Sql(string? s)
    {
        return s is null ? "NULL" : "N'" + s.Replace("'", "''") + "'";
    }

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
}
