# ViewDesignerSolution

## ¿Qué hay aquí?
- `ai/` : system prompt + plantillas + wiki (para “encerrar” a Copilot).
- `specs/` : definición de vistas (`spec.json`).
- `tools/ViewScriptGen/` : generador .NET que convierte `spec.json` → `sql/*.sql`.
- `sql/generated/` : salida de scripts.

## Generar SQL
```bash
dotnet run --project tools/ViewScriptGen/ViewScriptGen.csproj -- specs/sample_view.json sql/generated tools/ViewScriptGen/catalogs.json
```

Ajusta `catalogs.json` con los IDs reales de tu plataforma.

<!-- 
## Prompts útiles (Copilot)

Genera una vista con un estado que cargue la grilla desde el SP dbo.storedProcedurePrueba y agrega un WebControl Calendar para seleccionar fecha diaria.

IdVista: vTestCopilot
Estado: EstadoUnico (default)
Usa el generador tools/ViewScriptGen para crear el script SQL en sql/generated
Si falta algo, usa defaults del repositorio 

-->