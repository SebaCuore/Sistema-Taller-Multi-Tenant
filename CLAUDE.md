# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Estado del repositorio

Repo git activo. **Fase 0 de `docs/plan_desarrollo.md` terminada** (git + CI + Docker Compose + esqueletos de backend y frontend + tokens de color migrados). **Fase 1 en curso** en la rama `feature/fase-1-base-backend-login`: de lo que pide esa fase solo existen las entidades de `Data/Entidades/`. Todavía **no** hay `TallerDbContext`, ni `Data/Configuraciones/`, ni migraciones, ni `ITallerActual`, ni endpoints (`Features/` está vacío — `Program.cs` sigue siendo el "Hello World!" de la plantilla), ni proyecto de tests, ni pantalla de login (el frontend sigue mostrando la plantilla de Vite).

Dos cosas rotas hoy, a arreglar antes de seguir:
- El backend **no compila**: cinco entidades (`Servicio`, `OrdenTrabajo`, `OrdenTrabajoServicio`, `Producto`, `Venta`) implementan `IPerteneceATaller` sin el `using SistemaTaller.Api.Common;`.
- La CI **fallaría** en `dotnet test` (no existe `SistemaTaller.Tests`) y en `npm test` (no hay script `test` ni Vitest instalado). Ambos se resuelven al crear la infraestructura de tests que la Fase 1 exige de todos modos.

El idioma del proyecto es **español** — documentación, nombres de entidades, propiedades y rutas (`TallerId`, `patente`, `OrdenTrabajo`, `/api/vehiculos`) van en español, y el código nuevo debe seguir esa convención. Excepción deliberada: los **mensajes de commit van en inglés**, con prefijo (`feat`/`fix`/`docs`/`test`/`chore`).

## Qué es el sistema

Gestión web para talleres mecánicos (autos y motos): vehículos, órdenes de trabajo, catálogo de servicios, stock y ventas. Nace para **un taller concreto**, pero se diseña multi-taller desde el modelo de datos.

## Comandos

Base de datos (siempre primero, desde la raíz del repo):

```bash
docker compose up -d          # Postgres 18 en localhost:5432 (sistema_taller / postgres / devpassword)
```

Backend — todo desde `backend/SistemaTaller.Api/`:

```bash
dotnet run                    # API en http://localhost:5108 (https://localhost:7012)
dotnet build
dotnet ef migrations add NombreDescriptivo
dotnet ef database update
dotnet run -- crear-admin admin@ejemplo.com "contraseña"   # comando propio, previsto en backend.md
```

Los secretos de desarrollo van en `dotnet user-secrets` (`ConnectionStrings:Default`, `Jwt:Key`), nunca en `appsettings.json` — el `UserSecretsId` ya está en el `.csproj`.

Tests de backend (cuando exista `backend/SistemaTaller.Tests/` — xUnit + `WebApplicationFactory` + `Testcontainers.PostgreSql`, requiere Docker corriendo):

```bash
dotnet test                                          # toda la suite
dotnet test --filter FullyQualifiedName~Aislamiento  # solo la batería de taller_id
dotnet test --filter "FullyQualifiedName~AislamientoTallerTests.ListarNoDevuelveDatosDeOtroTaller"
```

Frontend — desde `frontend/`:

```bash
npm run dev       # Vite en http://localhost:5173 (el CORS del backend se abre para ese origen)
npm run build     # tsc -b && vite build — es lo que corre la CI
npm run lint
npx shadcn@latest add <componente>
```

Prototipo visual: `python -m http.server --directory design` (Chrome no permite automatizar `file://`).

## Documentación — orden de lectura

| Archivo | Cuándo leerlo |
|---|---|
| `docs/plan_desarrollo.md` | **Empezar acá para escribir código**: fases verticales con checklist y checkpoint. Una rama por fase |
| `docs/description.md` | Visión general, filosofía, decisiones deliberadamente pendientes |
| `docs/functional_requirements.md` | Requerimientos RF-01…RF-29 (referenciar por número al implementar) |
| `docs/data_base.md` | Modelo lógico: entidades, campos, relaciones, catálogo sembrado |
| `docs/architecture.md` | Stack a alto nivel, multi-tenancy y seguridad |
| `docs/backend.md` | El más denso y el más importante: estructura, convenciones EF Core, filtro por `taller_id`, auth JWT, ejemplo de endpoint completo |
| `docs/frontend.md` | Stack y guías de accesibilidad |
| `docs/screens.md` | Pantallas, navegación y patrón responsive |
| `docs/design.md` | Dirección visual: tokens de color, tipografía, elementos propios |
| `docs/mvp.md` | Corte MVP vs. futuro |
| `docs/testing.md` | Qué se testea, con qué y cuándo — la batería de aislamiento por `taller_id` es prioridad máxima |
| `docs/flujo_de_trabajo.md` | Git, GitHub y GitHub Actions: ramas, commits, PRs, CI |

Al cambiar decisiones de producto o alcance, actualizar el documento correspondiente en la misma tanda — estos archivos son la fuente de verdad del proyecto, no notas históricas. Vale también para este `CLAUDE.md` y para los checklists de `plan_desarrollo.md`.

## Invariantes de arquitectura (no negociables)

- **Aislamiento por `taller_id`.** Toda tabla de negocio lleva `TallerId` e implementa `IPerteneceATaller` (`Common/IPerteneceATaller.cs`). El filtro se impone **en la capa de acceso a datos**, no endpoint por endpoint: `TallerDbContext.OnModelCreating` recorre el modelo y aplica un global query filter con nombre (`"FiltroTaller"`, EF Core 10) a toda entidad que implemente la interfaz, y `SaveChangesAsync` asigna el `TallerId` a las entidades nuevas. Consecuencia práctica: **una entidad nueva solo tiene que implementar `IPerteneceATaller`** y queda filtrada sola; nunca se filtra a mano en un endpoint, nunca se lee el `taller_id` de un DTO o query string — sale del claim del JWT vía `ITallerActual` (scoped, igual que el `DbContext`). Código exacto en `docs/backend.md` → "El filtro por `taller_id`".
- **`IgnoreQueryFilters()` y `FromSqlRaw` esquivan el filtro.** Única aparición legítima en el MVP: la búsqueda del usuario en `POST /auth/login`, donde todavía no hay `taller_id` conocido. En cualquier otro lugar es un bug de seguridad.
- **El frontend nunca asume un solo taller**, aunque hoy exista un único registro en `Taller`.
- **No existe entidad Cliente.** El vehículo es la entidad central; el contacto del dueño (`ContactoNombre`, `ContactoTelefono`) vive en `Vehiculo`. La `Patente` es la clave de búsqueda principal (índice compuesto `(TallerId, Patente)`).
- **Contraseñas siempre hasheadas** (BCrypt); secretos por variables de entorno / user-secrets; HTTPS obligatorio en producción.
- **Rol simple**: `Usuario.Rol`, hoy solo `Admin`. No agregar tabla de permisos en el MVP.

## Reglas de dominio fáciles de romper

- **Patente conocida (RF-07)**: al ingresar una patente ya registrada se *sugiere* el contacto anterior, pero **siempre con confirmación explícita** — el vehículo pudo cambiar de dueño. Nunca autocompletar en silencio.
- **Estados intermedios opcionales (RF-20)**: una orden puede pasar de `Ingresado` directo a `Entregado`. Es el flujo normal para trabajos rápidos; no debe dar error ni pedir explicación.
- **Precios editables**: `Servicio.PrecioReferencia` solo precarga `OrdenTrabajoServicio.PrecioFinal`; el empleado lo edita a mano. `Venta.PrecioUnitario` congela el precio del momento, independiente de cambios posteriores en `Producto.Precio`.
- **Stock negativo permitido (RF-28)**: vender con stock 0 o negativo no se bloquea; devuelve un flag `sinStockSuficiente` y el frontend muestra una advertencia suave. El descuento de stock va con `ExecuteUpdateAsync` (atómico), no leyendo y reescribiendo.
- **`Posicion` / `Lado`** solo se piden si el `Servicio` los declara (`AplicaPosicion`, `AplicaLado`); `Lado` en la práctica aplica solo a autos.
- **Servicios y `TipoVehiculo` se desactivan, no se borran** (`Activo`): nunca exponer `DELETE`, solo `PATCH`. Un servicio desactivado desaparece del selector pero sigue visible en órdenes viejas que ya lo usaban.

## Backend (ASP.NET Core / .NET 10 / EF Core 10 / PostgreSQL)

Un solo proyecto Web API con **minimal APIs**, organizado **por función** (`Features/Vehiculos/`, `Features/OrdenesTrabajo/`…), no por capa técnica. Regla para ubicar un archivo nuevo: dominio/base de datos → `Data/`; operación que el frontend llama → `Features/<Área>/`; infraestructura compartida → `Common/`.

Convenciones que se rompen fácil (detalle en `docs/backend.md`):

- Entidades **POCO limpias**, sin Data Annotations — la configuración vive en `Data/Configuraciones/` (una `IEntityTypeConfiguration<T>` por entidad).
- IDs `Guid` generados con `Guid.CreateVersion7()`; excepción: los `HasData` sembrados necesitan GUIDs fijos.
- Montos siempre `decimal` con `.HasColumnType("numeric(12,2)")`.
- Fechas siempre UTC (`DateTime.UtcNow`); Npgsql exige `Kind = Utc` para `timestamptz` o revienta en runtime. La conversión a hora de Argentina es cosa del frontend.
- Enums de C# mapeados a texto con `.HasConversion<string>()`.
- `DeleteBehavior.Restrict` por defecto; única excepción `OrdenTrabajo → OrdenTrabajoServicio` (`Cascade`).
- Ciclo al agregar algo: entidad → configuración → `dotnet ef migrations add` → `database update` → recién ahí el endpoint.
- Todo endpoint lleva `.RequireAuthorization()` salvo `POST /auth/login`.

**Sin** Clean Architecture multi-proyecto, **sin** MediatR, **sin** Repository/UnitOfWork sobre el `DbContext`, **sin** ASP.NET Core Identity completo, **sin** AutoMapper — descartados a propósito, no por olvido (`docs/backend.md` → "Qué evitar a propósito"). No reintroducirlos.

## Frontend (React 19 + TS + Vite + Tailwind 4 + shadcn/ui)

- Tailwind 4 se configura **en CSS, no en un `tailwind.config.js`**: `frontend/src/index.css` tiene el bloque `@theme inline` y los tokens en `:root` / `.dark`. Los tokens del prototipo ya están migrados ahí (`--primary` = azul de patente `#0A3FA8`, `--listo`, `--proceso`, `--destructive`, `--spacing-tap: 56px`, fuentes Archivo/Alegreya Sans). Cambiar la paleta = editar ese archivo, nada más.
- shadcn/ui está inicializado con estilo `base-nova` sobre `@base-ui/react` (no Radix); alias `@/` → `src/`.
- `src/{api,hooks,pages,styles}` existen vacíos, esperando la Fase 1. **`api/` debe poder alternar entre mocks y la API real sin tocar `pages/`**, servido a través de TanStack Query.
- `design/prototipo.html` es la referencia visual (HTML/CSS plano, 7 pantallas, datos simulados). `design/prototipo-v1.html` es solo referencia de comparación, no fuente de verdad.

## Accesibilidad — restricciones duras de UI

El público son mecánicos, muchos de edad avanzada y usando el celular con las manos ocupadas. Aplican a **toda** pantalla nueva, en mobile y en escritorio por igual (más espacio no significa interfaz más densa):

- Altura táctil mínima 48–56px (`--spacing-tap`); tipografía base 18px en mobile, nunca menos de 14px.
- Alto contraste; paleta reducida centralizada en `src/index.css`.
- Listas/tarjetas en mobile, tablas solo en escritorio.
- Nunca un ícono solo — siempre con texto al lado.
- Ningún gesto (swipe, long-press) como única vía de interacción: siempre un botón visible equivalente.
- Confirmación antes de acciones destructivas.
- Mobile-first: formularios de un paso a la vez en pantalla completa; en escritorio, modal.

## Flujo de trabajo

Una rama por fase (`feature/fase-N-nombre`), commits chicos con prefijo en inglés, y al llegar al checkpoint de la fase: push, Pull Request a `main`, esperar el verde de GitHub Actions, "Squash and merge". Nunca se programa directo sobre `main`. Detalle en `docs/flujo_de_trabajo.md`.

## Pendiente de definición (no inventar)

Plataforma de hosting (candidatas en `docs/architecture.md`: Render, Railway, Fly.io, Azure App Service) y el detalle fino de `Producto`/`Venta` (categorías, proveedores, códigos) siguen deliberadamente sin decidir — esto último bloquea la Fase 6 hasta hablar con el dueño del taller. Si una tarea depende de algo pendiente, preguntar en vez de asumir.
