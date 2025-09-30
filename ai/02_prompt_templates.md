# Prompt Templates

## 1) Vista mínima y abrir el .sql (modo sencillo)
**Prompt**
Crea una vista mínima:
- IdVista: `vVentasDiarias`
- Un solo estado con **grilla**
- SP: `dbo.sp_VentasDiarias`

Usa los defaults del modo sencillo (sin handlers/left menu/web controls) y **crea un archivo** con el SQL en:
`sql/generated/vVentasDiarias_<YYYYMMDDHHmmss>.sql`, **ábrelo en el editor** y **no ejecutes nada**.

---

## 2) Vista mínima con parámetros de entrada
**Prompt**
Crea una vista mínima:
- IdVista: `vPedidosPorFecha`
- Grilla con SP `dbo.sp_PedidosPorFecha`
- Agrega mapeo de parámetros UI → SP:
  - `FechaDesde` → `@Desde`
  - `FechaHasta` → `@Hasta`

Es modo sencillo: 1 estado, 1 fila, 1 área, sin handlers/left menu/web controls.  
**Genera y abre** `sql/generated/vPedidosPorFecha_<YYYYMMDDHHmmss>.sql`.

---

## 3) Generar SQL desde un spec existente
**Prompt**
Lee `specs/sample_view.json` y genera el T-SQL completo siguiendo el orden:
Vistas → Estados → Filas → Áreas → ObjetosDeArea → Extracciones/StoredProcedure → Configuraciones → MapeoParametros → Handlers (si existieran).  
Incluye `BEGIN TRY/BEGIN TRAN/COMMIT` y `CATCH/ROLLBACK`.  
Crea/abre `sql/generated/sample_view_<YYYYMMDDHHmmss>.sql`. No ejecutes nada.

---

## 4) Revisar y corregir un `spec.json`
**Prompt**
Revisa `specs/sample_view.json`. Verifica:
- Tipos de objeto (solo `grilla` en modo sencillo),
- Tipo de ejecución de SP,
- Mapeo de parámetros (origen UI vs destino SP),
- Ausencia de handlers/left menu/web controls si no se pidieron.

Corrige **lo mínimo** para que pase por el generador (modo sencillo).

---

## 5) Consulta tipo wiki (comentarios)
**Prompt**
¿Cómo configuro los comentarios en una vista (según `ai/wiki/comments_config.md`)? Dame pasos exactos.

---

## 6) Frase directa (atajo)
**Prompt**
“Crear vista `vStockActual` con grilla usando el SP `dbo.sp_StockActual`.”  
**Crea y abre** `sql/generated/vStockActual_<YYYYMMDDHHmmss>.sql` (sin handlers/left menu/web controls).