# 2026-11-11 — Fase 4: Historial de servicios

**Fase 4** (cerrada). Cubre RF-22 y RF-23, y la edición de vehículo (RF-08) en la pantalla, como indica `pantallas.md`. Antes se implementaron los ajustes del dueño a la Fase 3 (ver [su entrada](2026-11-11-ajustes-fase-3.md)).

## Qué se hizo

- **Backend**: `GET /api/historial/servicios?patente=` (`Features/Historial/`): órdenes `Entregado`, la más reciente primero (máximo 200), con fecha de ingreso y entrega, servicios (posición/lado y precio) y total. La patente se normaliza y acepta parte de ella. Un servicio desactivado sigue apareciendo.
- **Frontend**: contrato `ApiHistorial` (real + mock); `Layout` con el contenedor de navegación de `pantallas.md` (barra inferior en mobile, lateral en escritorio, siempre con texto; hoy Taller e Historial, Ventas y Stock llegan con la Fase 6; "Cerrar sesión" provisorio hasta tener Ajustes en la Fase 5); `HistorialPage` (buscador, **tarjetas en mobile / tabla en escritorio ordenable** por fecha, patente y total con botones visibles en el encabezado); el detalle histórico reutiliza la pantalla de la orden (solo lectura) con "Volver al historial"; `EditarVehiculoPage` (marca, modelo, año, contacto; patente y tipo no se editan) con `GET /api/vehiculos/{id}` y `PUT`.
- **E2E** ampliado: tras finalizar, historial → buscar por patente → ver servicios y total → detalle → editar marca del vehículo.

## Verificado

- `dotnet test`: **51/51** (6 nuevos del historial: fecha/servicios/posición/precios, excluye órdenes en curso, búsqueda normalizada y parcial, **aislamiento** por taller, servicio desactivado visible, 401).
- `npm run lint`, `npm run build`, `npm test` (**24 tests**; 4 nuevos de `HistorialPage`: datos mostrados, búsqueda, mensaje de patente sin servicios, orden por columna): en verde.
- **E2E real** (API y Postgres reales): 1 passed (flujo completo incluido el historial).
- **Navegador** a 390 y 1280 px: tablero con navegación, historial buscando "AJ", edición de vehículo. Controles ≥ 63 px, texto ≥ 20 px. Capturas en [`capturas/fase-4/`](capturas/fase-4/).
- `grep IgnoreQueryFilters|FromSqlRaw`: solo el login.

## No verificado / pendiente

- El E2E sigue sin correr en la CI.
- El historial no pagina: devuelve como máximo las 200 órdenes más recientes.
- Contra una base con mucho historial no se midió rendimiento.
- Ajustes (⚙) y "Catálogo de servicios" llegan en la Fase 5.
- CI de GitHub en verde (corrida 43048058237).
