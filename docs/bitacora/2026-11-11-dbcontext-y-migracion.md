# 2026-11-11 — DbContext, configuraciones y migración inicial

**Fase 1** (en curso). Se tildaron 4 ítems del backend de [`plan_desarrollo.md`](../proceso/plan_desarrollo.md).

## Qué se hizo

- `Common/ITallerActual.cs` y `TallerActual.cs` (lectura perezosa del claim `taller_id`, para que el contexto pueda construirse en pedidos sin token) + `TallerFijo` para procesos sin usuario (`crear-admin`, tests, `dotnet ef`).
- `Data/TallerDbContext.cs`: 9 `DbSet`, filtro global `"FiltroTaller"` sobre toda entidad `IPerteneceATaller`, asignación de `TallerId` al guardar.
- `Data/Configuraciones/Configuraciones.cs`: una configuración por entidad, FKs con `Restrict` (salvo `OrdenTrabajo → OrdenTrabajoServicio`, `Cascade`), enums a texto, montos `numeric(12,2)`.
- Seed (`HasData`): `Taller` "San Martín", `TipoVehiculo` Auto/Moto, 7 servicios por defecto. Precios = los del prototipo (Alineación 32000, Balanceo 18000, Reparación de neumático 12000; el resto sin precio). Los flags `AplicaPosicion`/`AplicaLado` son una interpretación mía: **confirmar con el dueño del taller**.
- Migración `MigracionInicial` y `TallerDbContextFactory` (diseño, para `dotnet ef`).
- **Corrección de un bug en la doc**: `backend.md` armaba el filtro con `Expression.Constant(_tallerActual.TallerId)`; como EF cachea el modelo, eso fijaría el filtro al taller del primer pedido (fuga entre talleres). Ahora el filtro apunta a un miembro del contexto, que EF parametriza por consulta. `backend.md` corregido. Todavía no hay test que lo demuestre: es lo primero a cubrir con la batería de aislamiento.

## Verificado

- `dotnet build`: 0 errores, 0 advertencias.
- `docker compose down -v && docker compose up -d`, luego `dotnet ef database update`: aplica `MigracionInicial` sin errores.
- `psql`: 1 taller, 2 tipos de vehículo, 7 servicios con los precios esperados.
- `grep IgnoreQueryFilters|FromSqlRaw` en el código: sin apariciones.

## No verificado / pendiente

- El filtro por `taller_id` aún no tiene tests (falta el proyecto de tests). No se corrió `dotnet test`.
- Resto de la Fase 1: login, `crear-admin`, manejo global de errores, CORS, tests, Vitest y pantalla de login.
- Fase sin cerrar: los pasos de cierre (curl, navegador, CI) no aplican todavía.
