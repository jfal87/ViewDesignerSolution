# Handlers (resumen)

Los **Handlers** agregan comportamiento visual/lógico (ej. semáforos, formatos, acciones al hacer click). Se asocian por **Área** y se parametrizan con **JSON**.

## Ejemplos comunes
- **RowOnRender / semáforo**
  ```json
  {
    "column": "Revenue",
    "ranges": [
      { "min": 0, "max": 10000, "color": "red" },
      { "min": 10001, "max": 50000, "color": "yellow" },
      { "min": 50001, "max": null, "color": "green" }
    ]
  }
  ```
- **HeaderOnRender / título**
  ```json
  { "texto": "Ventas" }
  ```

## Buenas prácticas
- Evita JSON enormes: componlos por feature.
- Versiona cambios relevantes en la wiki.
