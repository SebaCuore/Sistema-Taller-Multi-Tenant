# Sistema Taller — Descripción general

## ¿Qué es?

Sistema de gestión web para talleres mecánicos (autos y motos): gestión de vehículos, órdenes de trabajo, catálogo de servicios y stock/ventas de productos.

El sistema nace para resolver la operación diaria de **un taller específico** (mecánica general, gomería — balanceos, alineaciones, reparación de tren delantero —, servicio de motos y venta de productos), pero se diseña desde el día uno para poder ofrecerse en el futuro a **múltiples talleres** como producto (SaaS), sin necesitar un rediseño del modelo de datos.

## Filosofía del proyecto

- **MVP simple y seguro primero.** Un conjunto acotado de funcionalidades, bien hechas y seguras para exponer en la web, antes que un sistema grande a medio terminar.
- **Escalable sin sobre-diseñar.** Las decisiones que son baratas de tomar ahora y caras de cambiar después (por ejemplo, el aislamiento multi-taller en la base de datos) se resuelven ya. Las que se pueden posponer sin costo (por ejemplo, la plataforma de hosting exacta, o el detalle fino de Stock) se dejan explícitamente pendientes.
- **Lo que importa es el vehículo, no el cliente como entidad.** El negocio gira en torno al vehículo que se atiende; el contacto del dueño se guarda como dato simple asociado al vehículo, no como una entidad de cliente con historial propio.

## A quién sirve

- **Dueño/empleados del taller**: cargan vehículos, gestionan órdenes de trabajo, consultan historial, gestionan stock y ventas.
- (Futuro) **Otros talleres**: el mismo sistema, con sus datos completamente aislados entre sí (`taller_id`).
- (Futuro) **Clientes finales**: hoy no tienen acceso al sistema; solo se los contacta manualmente con el teléfono cargado.

## Alcance del primer lanzamiento (MVP)

Ver el detalle completo en [`requerimientos_funcionales.md`](requerimientos_funcionales.md) y el corte MVP vs. futuro en [`mvp.md`](mvp.md). En resumen, el MVP cubre:

- Login de usuarios (sin sistema de roles granular todavía).
- Alta y gestión de vehículos, con datos de contacto del dueño.
- Catálogo de servicios configurable por taller (con valores por defecto).
- Órdenes de trabajo: sección "Taller" con vehículos en curso, servicios aplicados y estado.
- Historial de trabajos realizados, con búsqueda por patente.
- Stock de productos y ventas simples (mostrador).

## Documentos de este proyecto

El índice completo de la documentación, con qué contiene cada documento y cuándo leerlo, está en [`docs/README.md`](../README.md).

## Decisiones pendientes (a propósito)

Estas decisiones se dejaron abiertas de forma deliberada, no por omisión — se van a resolver en una sesión de definición propia más adelante:

- Plataforma de hosting concreta (frontend y backend ya definidos — ver [`frontend.md`](../tecnica/frontend.md) y [`backend.md`](../tecnica/backend.md)).
- Detalle de campos y comportamiento del módulo Stock/Producto.
- Validación de esta propuesta con el dueño del primer taller.
