
# 02_prompt_templates.md (Guía de usuario + plantillas)
```md
# Guía rápida — ViewDesigner Copilot (Agente) + Plantillas

Este repo está pensado para trabajar **solo con GitHub Copilot** (chat/agent) y, opcionalmente, **Copilot en la terminal (CLI)**.  
Sigue estos pasos y usa las plantillas del final.

---

## 0) Prerrequisitos
- **VS Code** actualizado.
- Extensiones:
  - **GitHub Copilot** (autocompletado).
  - **GitHub Copilot Chat** (chat/agents).
- Inicia sesión en GitHub desde VS Code (status bar, esquina inferior izquierda) y verifica que tu cuenta tenga acceso a Copilot.

> Tip: abre este repo en la **raíz** (carpeta que contiene `ai/`, `specs/`, `tools/`, `sql/`).

---

## 1) Activar Copilot (modo agente) en VS Code
1. Abre el panel **Copilot Chat** (ícono de Copilot o `Ctrl+I`).
2. En el cuadro de entrada:
   - Elige el **agente** `@workspace` (o el que uses por defecto).
   - **Añade contexto** con el ícono de clip o escribiendo `#` y seleccionando archivos.
3. **Fija** (pin) los archivos de contexto para toda la conversación.

### Archivos que debes adjuntar como contexto (siempre)
- `ai/wiki/01_system_prompt.md`  ← reglas del agente (**obligatorio**)
- `ai/wiki/02_prompt_templates.md` ← esta guía
- `tools/ViewScriptGen/Program.cs`
- `tools/ViewScriptGen/catalogs.json`
- (Opcional) el `spec.json` que vayas a usar en `specs/*.json`

### Mensaje inicial recomendado
> **Actúa como ViewDesigner Copilot en MODO RESTRINGIDO.**  
> Usa y obedece `ai/wiki/01_system_prompt.md`. Si pido algo fuera del repo, responde **“Falta evidencia en la wiki.”**  
> Trabaja con `tools/ViewScriptGen/*` para generar el SQL y abrirlo en VS Code.

---

## 2) Copilot en la terminal (CLI) — opcional
Si prefieres pedirle cosas a Copilot desde la terminal:

1. Instala **GitHub CLI** (`gh`) y autentícate:
   - `gh auth login`
2. Instala **Copilot en la CLI**:
   - `gh extension install github/gh-copilot`
3. Prueba:
   - `gh copilot -h`
   - `gh copilot explain "qué hace este archivo Program.cs"`
   - `gh copilot suggest "comando para compilar y ejecutar esta app .NET"`
   - `gh copilot generate "README con instrucciones de uso"`

> Nota: la CLI **no** reemplaza al generador. Úsala como asistente de línea de comandos.

---

## 3) Ejecutar el generador (desde VS Code)
- **Modo frase (vista mínima)**  
  Terminal en la raíz del repo:
  ```bash
  dotnet run --project tools/ViewScriptGen "crear vista vVentasDiarias con grilla usando el SP dbo.sp_VentasDiarias"
