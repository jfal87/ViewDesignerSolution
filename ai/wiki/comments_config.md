# Configuración de Comentarios (Guía rápida)

> Esta guía resume el flujo típico para habilitar comentarios en una vista del Engine.

1. Verifica que estén creadas las tablas/índices de comentarios y suscripciones (según script de inicialización de plataforma).
2. Asegura que la vista tenga un **IdVista** estable; los comentarios suelen asociarse por vista/objeto.
3. Si la plataforma requiere banderas de habilitación, inclúyelas en la configuración de la **Vista** o **Estado**.
4. En el front, valida que el archivo JS de la vista registre los manejadores de UI (abrir panel de comentarios, enviar, listar, etc.).
5. Considera permisos/seguridad: un usuario sin permiso no debe ver botones ni invocar endpoints de comentarios.

**Tips**
- Mantén un **IdVista** consistente al regenerar metadata; si cambia, mapea/rehidrata referencias.
- Documenta en el JS de la vista cómo se inicializa el módulo de comentarios.
