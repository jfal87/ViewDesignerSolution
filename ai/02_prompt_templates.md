# Prompt Templates

## 1) Generar `spec.json` a partir de requisitos
**Prompt**
Quiero una vista `TestVistaLocal` con 2 estados:  
- Inicial: gráfico (click navega a “EstadoDetalle”).  
- Detalle: grilla con semáforo en la columna `Revenue`.  
SPs: Chart = `dbo.sp_TestVistaLocal_Chart`, Grid = `dbo.sp_TestVistaLocal_Grid`.  
Crea `specs/sample_view.json` válido para nuestro generador.

## 2) Revisar y corregir un `spec.json`
**Prompt**
Revisa `specs/sample_view.json`. Verifica tipos de objeto, ejecución de SP, mapeo de parámetros y handlers. Ajusta lo mínimo para que pase por el generador `tools/ViewScriptGen`.

## 3) Generar SQL de metadata desde un `spec.json`
**Prompt**
Lee `specs/sample_view.json` y genera el T‑SQL de metadata completo siguiendo el orden: Vistas → Estados → Filas → Áreas → ObjetosDeArea → Extracciones/StoredProcedure → Configuraciones → MapeoParametros → Handlers → LeftMenu/WebControls. Incluye `BEGIN TRY/BEGIN TRAN/COMMIT` y `CATCH/ROLLBACK`.

## 4) Consulta tipo wiki
**Prompt**
¿Cómo configuro los comentarios en una vista (según `ai/wiki/comments_config.md`)? Dame los pasos exactos.
