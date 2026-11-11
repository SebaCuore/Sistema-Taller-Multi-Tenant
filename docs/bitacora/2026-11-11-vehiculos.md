# 2026-11-11 — Fase 2: Vehículos

**Fase 2** (cerrada, con las salvedades de abajo). Cubre RF-05, RF-06, RF-07 y RF-08 (esta última solo en la API).

## Qué se hizo

- **Backend** (`Features/Vehiculos/`): `GET /api/vehiculos` (con `?patente=`), `GET /api/vehiculos/{id}`, `GET /api/vehiculos/buscar-por-patente/{patente}`, `POST`, `PUT /{id}` y `GET /api/tipos-vehiculo`. Todos con `RequireAuthorization`. La patente se normaliza (mayúsculas, sin espacios ni guiones); validación con 400 (patente, tipo existente y activo, año 1900–año+1, largos). El DTO de entrada no tiene `TallerId`.
- Decisión de diseño: una patente puede tener **varios registros** (uno por alta, índice no único, como dice `base_de_datos.md`); `buscar-por-patente` devuelve el más reciente (el "último registro conocido" de RF-07).
- **Tests**: `SistemaTallerFactory.CrearClienteAutenticadoComo(taller)` (JWT firmado sin pasar por el login) y `VehiculosTests` (aislamiento HTTP: listar, 404 por id, `PUT` ajeno da 404 y no modifica, búsqueda por patente ajena, `TallerId` en el body ignorado, 401 sin token en los 6 endpoints; camino feliz de cada endpoint; 400s; normalización).
- **Frontend**: `api/` con contrato (`tipos.ts`), implementación real, mock en memoria y selector único (`vehiculos.ts`, `VITE_USAR_MOCKS=true`); TanStack Query; pantalla `NuevoVehiculoPage` (paso 1 de la Pantalla 2) con el aviso de patente conocida: muestra el contacto anterior, **no autocompleta**, y no deja guardar hasta elegir "Sí, es el mismo" o "No, cargar otro contacto". Un 401 fuera del login borra el token y vuelve a `/login`. Botón "+ Nuevo vehículo" en el inicio provisorio.

## Verificado

- `dotnet test`: **26/26** (Testcontainers). `npm run lint`, `npm run build`, `npm test` (7 tests, incluidos 5 del aviso de patente conocida): en verde.
- API real con `curl`: sin token 401; con token 200; id inexistente/ajeno 404.
- **Checkpoint en navegador real** (Playwright + Chromium, API real + `npm run dev`), a 390 y 1280 px: login → "+ Nuevo vehículo" → alta de un vehículo con contacto → "Cargar otro" → volver a escribir la misma patente (con espacio) → aparece "Esta patente ya estuvo acá" con el contacto guardado y el campo del dueño sigue vacío hasta confirmar. Los pedidos viajan con `Authorization`. Altura mínima de controles: 63 px. Capturas en [`capturas/fase-2/`](capturas/fase-2/).
- **Mock**: con el frontend levantado con `VITE_USAR_MOCKS=true` (sin API), la misma pantalla muestra el aviso para `ABC123` sin tocar `pages/`: el patrón de `frontend.md` funciona.
- `grep IgnoreQueryFilters|FromSqlRaw`: solo el login.

## No verificado / pendiente

- RF-08: no hay pantalla de edición de vehículo (solo `PUT`); el plan de esta fase no la pedía.
- Los registros de prueba creados en la base de desarrollo local (patentes `TS…`/`TD…`) quedan ahí.
- CI de GitHub en verde para el commit `feat: add new vehicle screen with known-plate confirmation and swappable api layer` (corrida 43038699670).
