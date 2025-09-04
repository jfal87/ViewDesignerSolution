BEGIN TRY
BEGIN TRAN

-- Limpiar vista si existe
EXEC dbo.eliminarVista N'TestVistaLocal';

-- === Insert Vistas ===

                        DECLARE @VistaId NVARCHAR(250) = N'TestVistaLocal';
                        INSERT INTO dbo.Vistas
                            (IdOwner, IdHistoricoCambio, Nombre, Descripcion, FechaCreacion, VersionActual, ArchivoJs, IdVista, TipoVista, UsaSeguridad, IdStringsConnectionMyVision)
                        VALUES
                            (1, NULL, N'TestVistaLocal', N'Vista de prueba generada por ViewScriptGen', SYSUTCDATETIME(), N'1.0', N'js/TestVistaLocal.js', @VistaId, N'go', 1, 14);
                        
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
                        

                            -- Estado: EstadoInicial
                            SET @IDX_Estado = @IDX_Estado + 1;
                            SET @IdEstado = @IDX_Estado;
                            INSERT INTO dbo.Estados (IdEstado, [Default], Descripcion, IdVista, ArchivoJs, LeftMenuFijo)
                            VALUES (@IdEstado, 1, N'EstadoInicial', @VistaId, NULL, 0);
                            

                                INSERT INTO dbo.ConfiguracionesEstados (IdEstado, Configuracion)
                                VALUES (@IdEstado, N'{"leftMenu":true}');
                                

                                -- Fila: Fila_1
                                SET @IDX_Fila = @IDX_Fila + 1;
                                SET @IdFila = @IDX_Fila;
                                INSERT INTO dbo.Filas (IdFila, IdEstado, Nombre)
                                VALUES (@IdFila, @IdEstado, N'Fila_1');
                                

                                    -- Área: Area_Chart
                                    SET @IDX_Area = @IDX_Area + 1;
                                    SET @IdArea = @IDX_Area;
                                    INSERT INTO dbo.Areas (IdArea, IdFila, Nombre)
                                    VALUES (@IdArea, @IdFila, N'Area_Chart');
                                    

                                        -- Extracción
                                        SET @IDX_Extr = @IDX_Extr + 1;
                                        SET @IdExtraccion = @IDX_Extr;
                                        INSERT INTO dbo.ExtraccionesDatos (IdExtraccionDato, Descripcion)
                                        VALUES (@IdExtraccion, N'Area_Chart');

                                        -- StoredProcedure
                                        SET @IDX_SP = @IDX_SP + 1;
                                        SET @IdSP = @IDX_SP;
                                        INSERT INTO dbo.StoredProcedure (IdStoredProcedure, Nombre, Descripcion, OrdenEjecucion, IdStringConnection, IdExtraccionDato, IdTipoEjecucionSP)
                                        VALUES (@IdSP, N'dbo.sp_TestVistaLocal_Chart', N'', 0, 14, @IdExtraccion, 1);
                                        

                                    -- Objeto de Área
                                    SET @IDX_Obj = @IDX_Obj + 1;
                                    SET @IdObjetoDeArea = @IDX_Obj;
                                    INSERT INTO dbo.ObjetosDeArea (IdObjetoDeArea, IdArea, IdExtraccionDato, IdTipoObjetoDeArea, Nombre, UsaPaginador)
                                    VALUES (@IdObjetoDeArea, @IdArea, @IdExtraccion, 1, N'Area_Chart', 0);
                                    

                                        -- Configuración del Objeto de Área
                                        SET @IDX_CfgObj = @IDX_CfgObj + 1;
                                        SET @IdCfg = @IDX_CfgObj;
                                        INSERT INTO dbo.ConfiguracionesObjetoDeArea (Id, IdObjetoDeArea, Configuracion)
                                        VALUES (@IdCfg, @IdObjetoDeArea, N'{"seriesClickNavigateTo":"EstadoDetalle"}');
                                        

                                        -- Mapeo de Parámetros
                                        SET @IDX_Mapeo = @IDX_Mapeo + 1;
                                        INSERT INTO dbo.MapeoParametros (IdMapeoParametro, IdEstado, NombreArea, Parametro, Variable, ValorDefault, IdStoredProcedure, VariableRequest)
                                        VALUES (@IDX_Mapeo, @IdEstado, N'Area_Chart', N'@Periodo', N'comboPeriodo', NULL, @IdSP, NULL);
                                        

                                        -- Handler
                                        SET @IDX_Handler = @IDX_Handler + 1;
                                        SELECT @AccionId = IdAccionHandler FROM dbo.AccionesHandler WHERE UPPER(Accion) = UPPER(N'setTitulo');
                                        IF @AccionId IS NOT NULL
                                        BEGIN
                                            INSERT INTO dbo.Handlers (IdHandler, IdObjetoDeArea, IdTipoHandler, IdAccionHandler, Parametros, IdsAplicarAccion, orden)
                                            VALUES (@IDX_Handler, @IdObjetoDeArea, 3, @AccionId, N'{"texto":"Ventas"}', N'All', 1);
                                        END
                                        

                            -- Estado: EstadoDetalle
                            SET @IDX_Estado = @IDX_Estado + 1;
                            SET @IdEstado = @IDX_Estado;
                            INSERT INTO dbo.Estados (IdEstado, [Default], Descripcion, IdVista, ArchivoJs, LeftMenuFijo)
                            VALUES (@IdEstado, 0, N'EstadoDetalle', @VistaId, N'js/TestVistaLocal_Detalle.js', 0);
                            

                                -- Fila: Fila_1
                                SET @IDX_Fila = @IDX_Fila + 1;
                                SET @IdFila = @IDX_Fila;
                                INSERT INTO dbo.Filas (IdFila, IdEstado, Nombre)
                                VALUES (@IdFila, @IdEstado, N'Fila_1');
                                

                                    -- Área: Area_Grid
                                    SET @IDX_Area = @IDX_Area + 1;
                                    SET @IdArea = @IDX_Area;
                                    INSERT INTO dbo.Areas (IdArea, IdFila, Nombre)
                                    VALUES (@IdArea, @IdFila, N'Area_Grid');
                                    

                                        -- Extracción
                                        SET @IDX_Extr = @IDX_Extr + 1;
                                        SET @IdExtraccion = @IDX_Extr;
                                        INSERT INTO dbo.ExtraccionesDatos (IdExtraccionDato, Descripcion)
                                        VALUES (@IdExtraccion, N'Area_Grid');

                                        -- StoredProcedure
                                        SET @IDX_SP = @IDX_SP + 1;
                                        SET @IdSP = @IDX_SP;
                                        INSERT INTO dbo.StoredProcedure (IdStoredProcedure, Nombre, Descripcion, OrdenEjecucion, IdStringConnection, IdExtraccionDato, IdTipoEjecucionSP)
                                        VALUES (@IdSP, N'dbo.sp_TestVistaLocal_Grid', N'', 0, 14, @IdExtraccion, 1);
                                        

                                    -- Objeto de Área
                                    SET @IDX_Obj = @IDX_Obj + 1;
                                    SET @IdObjetoDeArea = @IDX_Obj;
                                    INSERT INTO dbo.ObjetosDeArea (IdObjetoDeArea, IdArea, IdExtraccionDato, IdTipoObjetoDeArea, Nombre, UsaPaginador)
                                    VALUES (@IdObjetoDeArea, @IdArea, @IdExtraccion, 2, N'Area_Grid', 0);
                                    

                                        -- Configuración del Objeto de Área
                                        SET @IDX_CfgObj = @IDX_CfgObj + 1;
                                        SET @IdCfg = @IDX_CfgObj;
                                        INSERT INTO dbo.ConfiguracionesObjetoDeArea (Id, IdObjetoDeArea, Configuracion)
                                        VALUES (@IdCfg, @IdObjetoDeArea, N'{"paging":true}');
                                        

                                        -- Mapeo de Parámetros
                                        SET @IDX_Mapeo = @IDX_Mapeo + 1;
                                        INSERT INTO dbo.MapeoParametros (IdMapeoParametro, IdEstado, NombreArea, Parametro, Variable, ValorDefault, IdStoredProcedure, VariableRequest)
                                        VALUES (@IDX_Mapeo, @IdEstado, N'Area_Grid', N'@Periodo', N'comboPeriodo', NULL, @IdSP, NULL);
                                        

                                        -- Handler
                                        SET @IDX_Handler = @IDX_Handler + 1;
                                        SELECT @AccionId = IdAccionHandler FROM dbo.AccionesHandler WHERE UPPER(Accion) = UPPER(N'semaforo');
                                        IF @AccionId IS NOT NULL
                                        BEGIN
                                            INSERT INTO dbo.Handlers (IdHandler, IdObjetoDeArea, IdTipoHandler, IdAccionHandler, Parametros, IdsAplicarAccion, orden)
                                            VALUES (@IDX_Handler, @IdObjetoDeArea, 1, @AccionId, N'{"column":"Revenue","ranges":[{"min":0,"max":10000,"color":"red"},{"min":10001,"max":50000,"color":"yellow"},{"min":50001,"max":null,"color":"green"}]}', N'All', 1);
                                        END
                                        
COMMIT
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    DECLARE @Msg NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@Msg, 16, 1);
END CATCH
