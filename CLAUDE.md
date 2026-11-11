# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Estado del repositorio

Repo git activo; se trabaja directo sobre `main` (ver "Protocolo de trabajo autónomo", más abajo). **Fase 0 de [`plan_desarrollo.md`](docs/proceso/plan_desarrollo.md) terminada** (git + CI + Docker Compose + esqueletos de backend y frontend + tokens de color migrados). **Fase 1 en curso**: existen las entidades, `TallerDbContext` con el filtro global por `taller_id`, las configuraciones, la migración inicial con seed (aplicada con éxito sobre un Postgres vacío) y `ITallerActual`. Todavía **no** hay endpoints (`Features/` está vacío — `Program.cs` solo registra el contexto y un "Hello World!"), ni `POST /auth/login`, ni `crear-admin`, ni proyecto de tests, ni Vitest, ni pantalla de login (el frontend sigue mostrando la plantilla de Vite).

La CI (`.github/workflows/ci.yml`) corre en cada push a `main`: backend `restore` + `build`; frontend `npm ci` + `build` + `lint`; docs `scripts/verificar_docs.py`. Los pasos de test **se saltean** mientras no existan `backend/SistemaTaller.Tests/` y el script `test` de `frontend/package.json` — crearlos es parte de la Fase 1, y desde ese momento la CI los corre sola, sin tocar el workflow.

**La bitácora ([`docs/bitacora/`](docs/bitacora/README.md))** tiene el registro de cada sesión (qué se hizo, qué se verificó, qué quedó pendiente) y, arriba de todo, lo que está **esperando decisión** del dueño. Leer su índice y la última entrada antes de empezar.

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

Sin Docker: definir `TESTS_CONNECTION_STRING` apuntando a un Postgres disponible (por ejemplo `Host=localhost;Username=postgres;Password=devpassword`) y la factory de tests lo usa en vez de Testcontainers (ver [`testing.md`](docs/proceso/testing.md)).

Frontend — desde `frontend/`:

```bash
npm run dev       # Vite en http://localhost:5173 (el CORS del backend se abre para ese origen)
npm run build     # tsc -b && vite build — es lo que corre la CI
npm run lint
npx shadcn@latest add <componente>
```

Prototipo visual: `python -m http.server --directory docs/diseno/prototipos` (Chrome no permite automatizar `file://`).

## Documentación

Toda la documentación vive en `docs/`, organizada en subcarpetas por tema. **El punto de entrada es [`docs/README.md`](docs/README.md)**: índice general con cada documento, qué contiene y cuándo leerlo. Cada subcarpeta tiene además su propio `README.md` con el índice de esa carpeta.

| Carpeta | Qué hay |
|---|---|
| [`docs/producto/`](docs/producto/README.md) | Qué es el sistema, requerimientos RF-01…RF-29 (referenciar por número al implementar), corte MVP vs. futuro, decisiones deliberadamente pendientes |
| [`docs/tecnica/`](docs/tecnica/README.md) | Arquitectura y multi-tenancy, modelo de datos, frontend, y [`backend.md`](docs/tecnica/backend.md) — el más denso y el más importante: estructura, convenciones EF Core, filtro por `taller_id`, auth JWT, ejemplo de endpoint completo |
| [`docs/diseno/`](docs/diseno/README.md) | Dirección visual (tokens de color, tipografía), pantallas y navegación, prototipos HTML |
| [`docs/proceso/`](docs/proceso/README.md) | [`plan_desarrollo.md`](docs/proceso/plan_desarrollo.md) (**empezar acá para escribir código**: fases con checklist y checkpoint), testing (la batería de aislamiento por `taller_id` es prioridad máxima), Git y CI |
| [`docs/bitacora/`](docs/bitacora/README.md) | Una entrada por sesión de trabajo: qué se hizo, qué se verificó, qué quedó pendiente o esperando decisión |

Para programar: fase en curso de `plan_desarrollo.md` → los documentos que esa fase cita → `backend.md` / `pantallas.md` para cada endpoint o pantalla.

### Reglas para mantener la documentación navegable

- **Nada suelto**: todo documento nuevo va en la subcarpeta de `docs/` que le corresponde. En la raíz del repo solo quedan `README.md` y este `CLAUDE.md`. Si aparece un tema nuevo que va a tener varios documentos, se crea una subcarpeta con su propio `README.md`.
- **Todo indexado**: al crear, mover, renombrar o borrar un documento, en el mismo commit se actualiza el `README.md` de su carpeta y [`docs/README.md`](docs/README.md). Cada entrada del índice lleva el link y una línea de qué contiene o cuándo leerlo.
- **Nombres**: en español, en minúsculas con `_`, sin tildes ni espacios, y únicos dentro de `docs/` (una búsqueda por nombre encuentra un solo archivo).
- **Referencias entre documentos con links Markdown relativos** (por ejemplo `[backend.md](../tecnica/backend.md)`), no con el nombre suelto, para que se puedan seguir con un click en GitHub o en el editor.
- **Un tema, un lugar**: si algo ya está documentado, se enlaza en vez de repetirlo. Estos documentos son la fuente de verdad del proyecto, no notas históricas: al cambiar una decisión de producto o de alcance se actualiza el documento correspondiente en la misma tanda (vale también para este `CLAUDE.md` y para los checklists del plan).
- **Comprobación automática**: `python scripts/verificar_docs.py` revisa que no haya links rotos, que cada documento figure en el índice de su carpeta y que cada carpeta con documentos tenga su `README.md`. Corre también en la CI (job `docs`); si falla, se arregla como cualquier otro rojo.

## Invariantes de arquitectura (no negociables)

- **Aislamiento por `taller_id`.** Toda tabla de negocio lleva `TallerId` e implementa `IPerteneceATaller` (`Common/IPerteneceATaller.cs`). El filtro se impone **en la capa de acceso a datos**, no endpoint por endpoint: `TallerDbContext.OnModelCreating` recorre el modelo y aplica un global query filter con nombre (`"FiltroTaller"`, EF Core 10) a toda entidad que implemente la interfaz, y `SaveChangesAsync` asigna el `TallerId` a las entidades nuevas. Consecuencia práctica: **una entidad nueva solo tiene que implementar `IPerteneceATaller`** y queda filtrada sola; nunca se filtra a mano en un endpoint, nunca se lee el `taller_id` de un DTO o query string — sale del claim del JWT vía `ITallerActual` (scoped, igual que el `DbContext`). Código exacto en [`backend.md`](docs/tecnica/backend.md) → "El filtro por `taller_id`".
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

Convenciones que se rompen fácil (detalle en [`backend.md`](docs/tecnica/backend.md)):

- Entidades **POCO limpias**, sin Data Annotations — la configuración vive en `Data/Configuraciones/` (una `IEntityTypeConfiguration<T>` por entidad).
- IDs `Guid` generados con `Guid.CreateVersion7()`; excepción: los `HasData` sembrados necesitan GUIDs fijos.
- Montos siempre `decimal` con `.HasColumnType("numeric(12,2)")`.
- Fechas siempre UTC (`DateTime.UtcNow`); Npgsql exige `Kind = Utc` para `timestamptz` o revienta en runtime. La conversión a hora de Argentina es cosa del frontend.
- Enums de C# mapeados a texto con `.HasConversion<string>()`.
- `DeleteBehavior.Restrict` por defecto; única excepción `OrdenTrabajo → OrdenTrabajoServicio` (`Cascade`).
- Ciclo al agregar algo: entidad → configuración → `dotnet ef migrations add` → `database update` → recién ahí el endpoint.
- Todo endpoint lleva `.RequireAuthorization()` salvo `POST /auth/login`.

**Sin** Clean Architecture multi-proyecto, **sin** MediatR, **sin** Repository/UnitOfWork sobre el `DbContext`, **sin** ASP.NET Core Identity completo, **sin** AutoMapper — descartados a propósito, no por olvido ([`backend.md`](docs/tecnica/backend.md) → "Qué evitar a propósito"). No reintroducirlos.

## Frontend (React 19 + TS + Vite + Tailwind 4 + shadcn/ui)

- Tailwind 4 se configura **en CSS, no en un `tailwind.config.js`**: `frontend/src/index.css` tiene el bloque `@theme inline` y los tokens en `:root` / `.dark`. Los tokens del prototipo ya están migrados ahí (`--primary` = azul de patente `#0A3FA8`, `--listo`, `--proceso`, `--destructive`, `--spacing-tap: 56px`, fuentes Archivo/Alegreya Sans). Cambiar la paleta = editar ese archivo, nada más.
- shadcn/ui está inicializado con estilo `base-nova` sobre `@base-ui/react` (no Radix); alias `@/` → `src/`.
- `src/{api,hooks,pages,styles}` existen vacíos en la copia local, esperando la Fase 1 (Git no versiona carpetas vacías: en un clon nuevo hay que crearlas). **`api/` debe poder alternar entre mocks y la API real sin tocar `pages/`**, servido a través de TanStack Query.
- [`prototipo.html`](docs/diseno/prototipos/prototipo.html) es la referencia visual (HTML/CSS plano, 7 pantallas, datos simulados). [`prototipo-v1.html`](docs/diseno/prototipos/prototipo-v1.html) es solo referencia de comparación, no fuente de verdad.

## Accesibilidad — restricciones duras de UI

El público son mecánicos, muchos de edad avanzada y usando el celular con las manos ocupadas. Aplican a **toda** pantalla nueva, en mobile y en escritorio por igual (más espacio no significa interfaz más densa):

- Altura táctil mínima 48–56px (`--spacing-tap`); tipografía base 18px en mobile, nunca menos de 14px.
- Alto contraste; paleta reducida centralizada en `src/index.css`.
- Listas/tarjetas en mobile, tablas solo en escritorio.
- Nunca un ícono solo — siempre con texto al lado.
- Ningún gesto (swipe, long-press) como única vía de interacción: siempre un botón visible equivalente.
- Confirmación antes de acciones destructivas.
- Mobile-first: formularios de un paso a la vez en pantalla completa; en escritorio, modal.

## Protocolo de trabajo autónomo (agente)

Decisión del dueño (2026-11-11): el proyecto lo avanza un agente sin supervisión directa, que **commitea y pushea directo a `main`**; el dueño revisa el producto y la bitácora. Esto reemplaza el flujo de rama por fase + Pull Request de [`flujo_de_trabajo.md`](docs/proceso/flujo_de_trabajo.md) (queda documentado ahí como alternativa). Como la CI ya no actúa de barrera antes de `main`, la verificación de abajo es la que la reemplaza — no es opcional.

### 1. Al empezar cada sesión

- `git pull` sobre `main`. El repo se edita también desde Windows (`core.autocrlf=true`); si trabajás desde Linux sobre esa misma carpeta, pasá `-c core.autocrlf=true` a cada comando `git` para no ver todos los archivos como modificados.
- Leer la última entrada de [`bitacora/`](docs/bitacora/README.md), la sección "Estado del repositorio" y la fase en curso de [`plan_desarrollo.md`](docs/proceso/plan_desarrollo.md) (la primera con casillas sin tildar).
- Confirmar que la última corrida de CI en `main` está en verde: `gh run list --branch main --limit 1`. Si está en rojo, arreglar eso antes que cualquier otra cosa.

### 2. Mientras se trabaja

- Una fase a la vez y en orden; dentro de la fase, de a un ítem del checklist.
- Commits chicos con prefijo en inglés (`feat`/`fix`/`docs`/`test`/`chore`). Nunca commitear secretos (`user-secrets`, `.env`, claves JWT reales).
- Push a `main` después de cada bloque coherente que compile y pase los tests — no acumular horas de trabajo sin pushear. Cada push dispara la CI.
- Después de cada push, seguir la corrida hasta que termine (`gh run watch`, o `gh run list --branch main --limit 1`). **Si la CI queda en rojo, lo siguiente es arreglarla**: no se suma trabajo nuevo sobre un `main` roto.
- Nunca `git push --force` a `main`, ni reescribir historial ya pusheado.

### 3. Definición de "terminado" (verificación obligatoria)

Un ítem del checklist se tilda solo cuando se **comprobó ejecutándolo** que funciona — que compile no alcanza. Para cerrar una fase tiene que pasar todo esto, y quedar anotado en la bitácora con los comandos usados y su resultado:

1. **Backend**: `dotnet build` sin errores y `dotnet test` en verde, incluida la batería de aislamiento por `taller_id` de las entidades de la fase.
2. **Base de datos desde cero**: con Postgres vacío (`docker compose down -v && docker compose up -d`, o una base nueva si no hay Docker), `dotnet ef database update` aplica todas las migraciones sin errores y el seed queda cargado.
3. **API levantada de verdad**: `dotnet run` y probar con `curl` los endpoints de la fase — login → token → cada endpoint nuevo con el token; un pedido sin token tiene que dar 401 y uno sobre un recurso de otro taller, 404.
4. **Frontend**: `npm run lint`, `npm run build` y `npm test` en verde.
5. **Recorrido en navegador**: con la API y `npm run dev` levantados, recorrer el checkpoint de la fase en un navegador real (Playwright headless sirve) a 390px de ancho (mobile) y a 1280px (escritorio). Guardar capturas en `docs/bitacora/capturas/fase-N/` y revisar en ellas las restricciones de la sección "Accesibilidad" de este archivo.
6. **Revisión contra las reglas**: releer los RF de la fase, "Invariantes de arquitectura" y "Reglas de dominio fáciles de romper"; `grep -rn "IgnoreQueryFilters\|FromSqlRaw" backend/` no debe mostrar nada fuera de `POST /auth/login`.
7. **CI en verde** en GitHub para el último commit de la fase.
8. **Checkpoint** de la fase en [`plan_desarrollo.md`](docs/proceso/plan_desarrollo.md) cumplido tal como está escrito.

Recién entonces: tildar las casillas en [`plan_desarrollo.md`](docs/proceso/plan_desarrollo.md), actualizar "Estado del repositorio" en este archivo, escribir la entrada de la bitácora, commit `docs:` y push.

Si un paso no se puede ejecutar en el entorno (por ejemplo, no hay Docker o no hay navegador), **no se da por pasado**: se usa la alternativa documentada (Postgres local + `TESTS_CONNECTION_STRING`, ver [`testing.md`](docs/proceso/testing.md)) o se anota en la bitácora como **no verificado**, y la fase queda abierta.

### 4. Cuándo parar y dejarle la decisión al dueño

Agregar a la bitácora una sección **"Esperando decisión"** con la pregunta concreta, y no avanzar sobre ese punto, cuando:

- Se llega a la **Fase 6** (Stock y Ventas): bloqueada hasta que el dueño hable con el dueño del taller sobre `Producto`/`Venta`. La Fase 5 sí se puede hacer antes.
- Se llega a la **Fase 7**: elegir hosting, crear cuentas, gastar plata y cargar secretos de producción los decide el dueño.
- Una tarea depende de algo de "Pendiente de definición", o los documentos se contradicen entre sí.
- Haría falta algo irreversible o fuera del repositorio: reescribir historial, cambiar la configuración de GitHub, crear servicios externos o pagos.
- Algo del entorno no funciona y no se resuelve en dos o tres intentos razonables (Docker, .NET SDK, Postgres, credenciales de GitHub o `gh`).

En una sesión interactiva (Claude Code en la terminal del dueño), además de anotarlo en la bitácora hay que **frenar y avisarle ahí mismo**: qué se intentó, qué está bloqueando y qué se necesita de él. Si queda trabajo que no depende de lo bloqueado, se puede seguir con eso mientras tanto.

### 5. Bitácora ([`docs/bitacora/`](docs/bitacora/README.md))

- **Un archivo por sesión**: `docs/bitacora/AAAA-MM-DD-tema-corto.md` (si hay dos el mismo día, sufijo `-2`). Contiene: fase, qué se hizo (commits), qué se verificó y cómo (comandos y resultado), qué quedó **no verificado** o pendiente, y lo que esté esperando decisión.
- **Índice**: cada sesión nueva se agrega arriba de la tabla de [`docs/bitacora/README.md`](docs/bitacora/README.md), y la sección "Esperando decisión" de ese índice se mantiene al día (agregar lo nuevo, sacar lo resuelto): es lo primero que mira el dueño.
- **Capturas** en `docs/bitacora/capturas/fase-N/`, enlazadas desde la entrada de la sesión.
- En español, escrita para alguien que no vio la sesión.

## Pendiente de definición (no inventar)

Plataforma de hosting (candidatas en [`arquitectura.md`](docs/tecnica/arquitectura.md): Render, Railway, Fly.io, Azure App Service) y el detalle fino de `Producto`/`Venta` (categorías, proveedores, códigos) siguen deliberadamente sin decidir — esto último bloquea la Fase 6 hasta hablar con el dueño del taller. Si una tarea depende de algo pendiente, preguntar en vez de asumir.
