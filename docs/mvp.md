# MVP y Roadmap

Este documento separa explícitamente qué entra en el primer lanzamiento (MVP) y qué queda para fases futuras, para que "escalable a futuro" no quede como una idea vaga.

## Criterio general

El MVP prioriza: (1) un flujo de trabajo diario completo para un único taller, (2) seguro para exponer en la web, (3) con un modelo de datos que no haya que romper cuando se sume más de un taller o más funcionalidades. Todo lo que no es imprescindible para operar el taller desde el día uno queda para después.

## Incluido en el MVP

- Login de usuarios (sin roles granulares).
- Gestión de vehículos (patente, tipo, marca/modelo/año, contacto del dueño).
- Autocompletado de contacto al reingresar una patente conocida (con confirmación).
- Catálogo de servicios por taller, con lista por defecto, activar/desactivar y agregar servicios propios.
- Sección "Taller": vehículos en curso por estado (ingresado / en proceso / listo para retirar / entregado), con registro de estados intermedios opcional.
- Alta de orden de trabajo con uno o varios servicios, cada uno con posición/lado (si aplica) y precio editable.
- Historial de trabajos por patente.
- Stock de productos: alta, edición, baja, consulta (a nivel básico).
- Ventas de productos de mostrador, con descuento automático de stock (permitiendo stock negativo).

## Pendiente de definición técnica (no funcional, pero necesario antes de programar)

- **Detalle del módulo Stock/Producto.** Campos adicionales, categorías, comportamiento fino de la venta — a definir en una sesión propia.
- **Plataforma de hosting concreta.** El stack (ASP.NET Core + PostgreSQL, ver `backend.md`) no ata a ninguna en particular; candidatas relevadas en `architecture.md` (Render, Railway, Fly.io, Azure App Service). Se elige más cerca del momento de desplegar.

Ya resuelto — ver [`architecture.md`](./architecture.md) y [`backend.md`](./backend.md): stack de backend (ASP.NET Core sobre .NET 10), ORM (Entity Framework Core 10) y motor de base de datos (PostgreSQL).

## Fuera del MVP — fases futuras

| Funcionalidad | Por qué se posterga |
|---|---|
| Facturación electrónica (AFIP) | Complejidad propia (certificados, homologación, WSFE) que no debe bloquear un lanzamiento simple y seguro. |
| Notificaciones automáticas al cliente (WhatsApp/SMS/email) ante cambio de estado | Hoy alcanza con el contacto manual; automatizarlo es una mejora, no un bloqueante. |
| Portal de cliente (ver estado de su vehículo online) | No hay entidad Cliente en el MVP; requeriría diseño propio. |
| Sistema de roles granular (Empleado, contador, etc.) | El MVP funciona con un único rol (`admin`); el campo `rol` ya está pensado para extenderse sin romper el esquema ni el token de autenticación. |
| Onboarding de nuevos talleres como producto (multi-tenant real, planes/suscripciones) | El modelo de datos ya soporta varios talleres (`taller_id`), pero falta todo lo operativo/comercial para ofrecerlo como producto. |
| Presupuesto previo a aprobar el trabajo (separado de la orden final) | El MVP registra precio directo en la orden; un flujo de presupuesto es una mejora posterior. |
| Detalle avanzado de Stock/Producto (categorías, proveedores, códigos, alertas de stock bajo) | Se define en una sesión de trabajo propia, a pedido del dueño del proyecto. |

## Próximos pasos

El orden fase por fase, con checklist, está en [`plan_desarrollo.md`](./plan_desarrollo.md) — arranca por la base del backend + login, sigue por Vehículos y Órdenes de trabajo (el corazón del sistema), y deja Stock/Ventas para después de cerrar su detalle con el dueño del taller. En paralelo, sigue pendiente entrevistar al dueño del primer taller con esta propuesta (`description.md` + `functional_requirements.md` + el prototipo navegable en `design/prototipo.html` son los documentos más útiles para esa charla) para validar el enfoque y resolver el detalle de Stock/Producto que bloquea esa fase.
