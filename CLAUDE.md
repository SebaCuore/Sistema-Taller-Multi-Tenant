# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Estado del repositorio

Este repositorio **todavía no tiene aplicación**: contiene `docs/` (la especificación completa) y `design/prototipo.html`, un prototipo estático navegable de un solo archivo. No hay `package.json`, ni build, ni tests, ni control de versiones (no es un repo git — inicializarlo es el primer paso de la Fase 0 de `docs/plan_desarrollo.md`, siguiendo `docs/flujo_de_trabajo.md`). El idioma del proyecto es **español** — documentación, nombres de entidades y campos (`taller_id`, `patente`, `OrdenTrabajo`) están en español, y el código nuevo debe seguir esa convención.

Stack de frontend y de backend ya están decididos (ver más abajo). El plan de implementación, fase por fase (backend y frontend juntos, no todo el frontend primero), está en `docs/plan_desarrollo.md` — es el documento por donde arrancar para escribir código.

## Qué es el sistema

Gestión web para talleres mecánicos (autos y motos): vehículos, órdenes de trabajo, catálogo de servicios, stock y ventas. Nace para **un taller concreto**, pero se diseña multi-taller desde el modelo de datos.

## Documentación — orden de lectura

| Archivo | Cuándo leerlo |
|---|---|
| `docs/description.md` | Visión general, filosofía, decisiones deliberadamente pendientes |
| `docs/functional_requirements.md` | Requerimientos numerados RF-01…RF-29 (referenciar por número al implementar) |
| `docs/data_base.md` | Modelo lógico: entidades, campos, relaciones |
| `docs/architecture.md` | Stack elegido a alto nivel, multi-tenancy y seguridad |
| `docs/backend.md` | Estructura del backend, convenciones de Entity Framework Core, implementación concreta del filtro por `taller_id` |
| `docs/frontend.md` | Stack elegido y guías de accesibilidad |
| `docs/screens.md` | Pantallas, navegación y patrón responsive |
| `docs/design.md` | Dirección visual: tokens de color, tipografía, elementos propios |
| `docs/mvp.md` | Corte MVP vs. futuro |
| `docs/plan_desarrollo.md` | Plan de implementación fase por fase, con checklist — por dónde ir escribiendo código |
| `docs/testing.md` | Qué se testea, con qué herramienta y en qué momento — la batería de aislamiento por `taller_id` es prioridad máxima |
| `docs/flujo_de_trabajo.md` | Git, GitHub y GitHub Actions: ramas, commits, CI |

Al cambiar decisiones de producto o alcance, actualizar el documento correspondiente en la misma tanda — estos archivos son la fuente de verdad del proyecto, no notas históricas.

## Invariantes de arquitectura (no negociables)

- **Aislamiento por `taller_id`.** Toda tabla de negocio (`Vehiculo`, `Servicio`, `OrdenTrabajo`, `Producto`, `Venta`, `Usuario`) lleva `taller_id`. Ninguna consulta puede ejecutarse sin filtrar por el `taller_id` del usuario autenticado, y ese filtro debe imponerse **en la capa de acceso a datos**, no endpoint por endpoint. Es el requisito de seguridad más importante del sistema. Implementación concreta: global query filters de Entity Framework Core, aplicados automáticamente a toda entidad marcada — ver `docs/backend.md`.
- **El frontend nunca asume un solo taller**, aunque hoy exista un único registro en `Taller`.
- **No existe entidad Cliente.** El vehículo es la entidad central; el contacto del dueño (`contacto_nombre`, `contacto_telefono`) vive en `Vehiculo`. La `patente` es la clave de búsqueda principal.
- **Contraseñas siempre hasheadas** (BCrypt); secretos por variables de entorno; HTTPS obligatorio en producción.
- **Rol simple**: `Usuario.rol` es un texto, hoy solo `admin`. No agregar tabla de permisos en el MVP.

## Reglas de dominio fáciles de romper

- **Patente conocida (RF-07)**: al ingresar una patente ya registrada se *sugiere* el contacto anterior, pero **siempre con confirmación explícita** — el vehículo pudo cambiar de dueño. Nunca autocompletar en silencio.
- **Estados intermedios opcionales (RF-20)**: una orden puede pasar de `ingresado` directo a `entregado`. Ese es el flujo normal para trabajos rápidos; no debe dar error ni pedir explicación.
- **Precios editables**: `Servicio.precio_referencia` solo precarga `OrdenTrabajoServicio.precio_final`; el empleado lo edita a mano. `Venta.precio_unitario` congela el precio del momento, independiente de cambios posteriores en `Producto.precio`.
- **Stock negativo permitido (RF-28)**: vender con stock 0 o negativo no se bloquea; a lo sumo una advertencia suave.
- **`posicion` / `lado`** solo se piden si el `Servicio` los declara (`aplica_posicion`, `aplica_lado`); `lado` en la práctica aplica solo a autos.
- **Servicios se desactivan, no se borran** (`activo`), igual que `TipoVehiculo`.

## Stack de frontend (decidido)

React + TypeScript + Vite + Tailwind CSS + shadcn/ui + React Router + TanStack Query + lucide-react. Se descartó Next.js (app interna detrás de login, sin SEO ni SSR).

Estructura propuesta en `docs/frontend.md`: `src/{components,pages,api,hooks,lib,styles}`. `api/` debe poder alternar entre mocks y la API real sin tocar `pages/`.

`design/prototipo.html` es la referencia visual: HTML/CSS plano, sin dependencias, con las 7 pantallas y datos simulados. Los tokens de color viven en su bloque `:root` y son los que hay que trasladar a la config de Tailwind. Para verlo en el navegador hace falta servirlo por HTTP (`python -m http.server --directory design`) — Chrome no permite automatizar `file://`. La versión anterior del prototipo queda en `design/prototipo-v1.html` solo como referencia de comparación, no como fuente de verdad.

## Stack de backend (decidido)

ASP.NET Core sobre **.NET 10** (LTS), con **Entity Framework Core 10** y **PostgreSQL**. Autenticación por JWT emitido a mano sobre la tabla `Usuario` (sin ASP.NET Core Identity completo). Detalle de estructura de proyecto, convenciones de entidades, migraciones y — lo más importante — la implementación exacta del filtro global por `taller_id`, en `docs/backend.md`.

Un solo proyecto Web API organizado por función (`Features/Vehiculos`, `Features/OrdenesTrabajo`, etc.), no por capa técnica horizontal. Sin patrón Repository/UnitOfWork envolviendo el `DbContext` — se considera sobre-ingeniería para este tamaño de proyecto (ver `docs/backend.md`, sección "Qué evitar a propósito").

## Accesibilidad — restricciones duras de UI

El público son mecánicos, muchos de edad avanzada y usando el celular con las manos ocupadas. Estas guías aplican a **toda** pantalla nueva, en mobile y en escritorio por igual (más espacio no significa interfaz más densa):

- Altura táctil mínima 48–56px; tipografía base 18px en mobile, nunca menos de 14px.
- Alto contraste; paleta reducida centralizada como tokens de Tailwind en un único archivo.
- Listas/tarjetas en mobile, tablas solo en escritorio.
- Nunca un ícono solo — siempre con texto al lado.
- Ningún gesto (swipe, long-press) como única vía de interacción: siempre un botón visible equivalente.
- Confirmación antes de acciones destructivas.
- Mobile-first: formularios de un paso a la vez en pantalla completa; en escritorio, modal.

## Pendiente de definición (no inventar)

Plataforma de hosting concreta (candidatas relevadas en `docs/architecture.md`: Render, Railway, Fly.io, Azure App Service) y el detalle fino de `Producto`/`Venta` (categorías, proveedores, códigos) siguen deliberadamente sin decidir. Backend, ORM y motor de base de datos **ya están decididos** (ASP.NET Core / .NET 10 / EF Core / PostgreSQL — ver `docs/backend.md`). Si una tarea depende de algo que sigue pendiente, preguntar en vez de asumir.
