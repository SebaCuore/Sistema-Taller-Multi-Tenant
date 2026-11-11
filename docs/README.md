# Documentación — Sistema Taller

Índice general de la documentación. Cada carpeta tiene además su propio índice (`README.md`) con más detalle. Las reglas para mantenerla ordenada están en [`CLAUDE.md`](../CLAUDE.md) → "Documentación".

## ¿Por dónde empiezo?

| Si querés… | Leé |
|---|---|
| Entender qué es el sistema y para quién | [`descripcion.md`](producto/descripcion.md) |
| Saber en qué estado está, qué se hizo y qué está esperando decisión | [`bitacora/`](bitacora/README.md) |
| Programar la próxima fase | [`plan_desarrollo.md`](proceso/plan_desarrollo.md) |
| Ver cómo se va a ver | [`prototipo.html`](diseno/prototipos/prototipo.html) (abrirlo en el navegador) |

## [`producto/`](producto/README.md) — qué se construye y por qué

| Documento | Contenido |
|---|---|
| [`descripcion.md`](producto/descripcion.md) | Visión general, filosofía, a quién sirve y decisiones pendientes a propósito |
| [`requerimientos_funcionales.md`](producto/requerimientos_funcionales.md) | Requerimientos RF-01…RF-29, por módulo |
| [`mvp.md`](producto/mvp.md) | Qué entra en el primer lanzamiento y qué queda para fases futuras |

## [`tecnica/`](tecnica/README.md) — cómo está construido

| Documento | Contenido |
|---|---|
| [`arquitectura.md`](tecnica/arquitectura.md) | Stack a alto nivel, estrategia multi-taller (`taller_id`) y seguridad |
| [`backend.md`](tecnica/backend.md) | El más denso: estructura, convenciones de EF Core, filtro por `taller_id`, auth JWT, ejemplo de endpoint completo |
| [`base_de_datos.md`](tecnica/base_de_datos.md) | Modelo de datos: entidades, campos, relaciones y catálogo sembrado |
| [`frontend.md`](tecnica/frontend.md) | Stack de frontend y guías de accesibilidad |

## [`diseno/`](diseno/README.md) — cómo se ve y cómo se usa

| Documento | Contenido |
|---|---|
| [`diseno_visual.md`](diseno/diseno_visual.md) | Paleta, tipografía y elementos visuales propios |
| [`pantallas.md`](diseno/pantallas.md) | Pantallas de la primera versión, navegación y patrón responsive |
| [`prototipo.html`](diseno/prototipos/prototipo.html) | Prototipo navegable — referencia visual vigente |
| [`prototipo-v1.html`](diseno/prototipos/prototipo-v1.html) | Versión anterior del prototipo, solo para comparar |

## [`proceso/`](proceso/README.md) — cómo se trabaja

| Documento | Contenido |
|---|---|
| [`plan_desarrollo.md`](proceso/plan_desarrollo.md) | Fases verticales con checklist y checkpoint — empezar acá para programar |
| [`testing.md`](proceso/testing.md) | Qué se testea, con qué herramienta y cuándo |
| [`flujo_de_trabajo.md`](proceso/flujo_de_trabajo.md) | Git, commits y GitHub Actions |

## [`bitacora/`](bitacora/README.md) — qué se hizo

Una entrada por sesión de trabajo, la más nueva arriba, con lo verificado, lo pendiente y lo que espera una decisión. Las capturas de cada fase quedan en `bitacora/capturas/`.
