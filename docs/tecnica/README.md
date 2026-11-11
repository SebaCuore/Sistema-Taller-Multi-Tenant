# Técnica

Cómo está construido el sistema. Volver al [índice general](../README.md).

| Documento | Contenido | Cuándo leerlo |
|---|---|---|
| [`arquitectura.md`](arquitectura.md) | Stack a alto nivel, multi-tenancy por `taller_id`, seguridad, candidatas de hosting | Para entender las decisiones de fondo |
| [`backend.md`](backend.md) | Estructura del proyecto, convenciones de EF Core, filtro global por `taller_id`, autenticación JWT, seed, manejo de errores, ejemplo de endpoint completo, qué evitar a propósito | Antes de escribir cualquier código de backend — es el más importante |
| [`base_de_datos.md`](base_de_datos.md) | Modelo lógico: entidades, campos, relaciones y catálogo de servicios sembrado | Al tocar entidades, configuraciones o migraciones |
| [`frontend.md`](frontend.md) | Stack de frontend (React, Vite, Tailwind, shadcn/ui, TanStack Query) y guías de accesibilidad | Antes de escribir código de frontend |
