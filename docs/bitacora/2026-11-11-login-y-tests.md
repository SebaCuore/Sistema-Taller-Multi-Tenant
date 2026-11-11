# 2026-11-11 — Login de punta a punta, proyecto de tests y Vitest

**Fase 1** (cerrada, con las salvedades de abajo). Continúa de [DbContext, configuraciones y migración inicial](2026-11-11-dbcontext-y-migracion.md).

## Qué se hizo

- **Backend**: `POST /auth/login` (JWT con `sub`/`taller_id`/`rol`, 8 h, email normalizado a minúsculas, mensaje 401 genérico), JWT Bearer + `AddAuthorization`, `GlobalExceptionHandler` + `ProblemDetails`, CORS solo para `FrontendOrigin` (`http://localhost:5173` en `appsettings.json`), comando `dotnet run -- crear-admin <email> <contraseña>` (hash BCrypt calculado al momento, mínimo 8 caracteres, rechaza duplicados). `Program` quedó `public partial` para `WebApplicationFactory`.
- **Tests** (`backend/SistemaTaller.Tests/`): `SistemaTallerFactory` (Testcontainers `postgres:18`, o `TESTS_CONNECTION_STRING` con base `tests_<guid>` descartable), batería de aislamiento a nivel `DbContext` (listar, por id, `FindAsync`, `TallerId` mandado a mano se ignora, y el filtro no queda fijado al primer taller), tests de login (ok + claims, 3 casos de 401) y `TallerActual` sin claim falla explícito.
- **Frontend**: Vitest + React Testing Library (`npm test` = `vitest run`), `api/cliente.ts` (manda `Authorization: Bearer`), `api/auth.ts`, `lib/sesion.ts` (token en `localStorage`, tolerante a que falle), `LoginPage` (Pantalla 0, mensaje "Usuario o contraseña incorrectos"), `InicioPage` provisoria con "Cerrar sesión", rutas con `react-router-dom`. Base tipográfica subida a 18 px en `index.css` (el recorrido mostró 16 px).

## Verificado (comandos y resultado)

- `dotnet build`: 0 errores, 0 advertencias. `dotnet test backend/SistemaTaller.Tests`: **10/10** (con Testcontainers real, sin `TESTS_CONNECTION_STRING`).
- **Prueba de mutación**: cambié temporalmente el filtro a `Expression.Constant(tallerActual.TallerId)` (la forma que traía la doc): 3 tests de aislamiento fallaron. Se restauró el código correcto. Confirma que la batería detecta la fuga entre talleres.
- Base desde cero: `docker compose down -v && up -d` + `dotnet ef database update` (sesión anterior; no hubo migraciones nuevas).
- API real: `crear-admin` creó el admin (y rechazó el duplicado); con `curl`, login correcto devuelve token, contraseña mala da 401, el preflight CORS responde `Access-Control-Allow-Origin: http://localhost:5173`.
- `npm run lint`, `npm run build`, `npm test` (2 tests): en verde.
- **Navegador real** (Playwright + Chromium headless, API y `npm run dev` levantados), a 390 px y 1280 px: redirige a `/login`, error con contraseña incorrecta, login correcto lleva a "Sesión iniciada", el pedido posterior viaja con `Authorization: Bearer`. Campos y botón de 63 px de alto (≥ 56), fuente base 18 px, sin ícono solo. Capturas en [`capturas/fase-1/`](capturas/fase-1/): `login-*`, `login-error-*`, `inicio-*` (`mobile` y `escritorio`).
- `grep IgnoreQueryFilters|FromSqlRaw`: solo aparece en `Features/Auth/AuthEndpoints.cs` (el login).

## No verificado / salvedades

- **Un pedido sin token a un endpoint protegido → 401, y un recurso de otro taller → 404, por HTTP**: todavía no existe ningún endpoint protegido de negocio. Quedan cubiertos a nivel `DbContext` y se repiten por HTTP en la Fase 2 con `Vehiculo`.
- El usuario `admin@ejemplo.com` de la base de desarrollo local es de pruebas.
- CI de GitHub en verde para el commit `feat: add login endpoint, crear-admin, backend test suite and login screen` (corrida 43037037835: backend con los 10 tests vía Testcontainers, frontend lint + build + test, docs).
