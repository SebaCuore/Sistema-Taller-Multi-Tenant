# 2026-11-11 — Fase 3: Taller / Órdenes de trabajo

**Fase 3** (cerrada, con las salvedades de abajo). Cubre RF-14 a RF-21.

## Qué se hizo

- **Backend**: `Features/OrdenesTrabajo/` — `GET /api/ordenes` (en curso, o `?estado=`), `GET /{id}`, `POST` (vehículo + ≥1 servicio; `UsuarioId` y `TallerId` salen del token), `POST /{id}/servicios`, `DELETE /{id}/servicios/{itemId}`, `PATCH /{id}/estado` (Ingresado/EnProceso/ListoParaRetirar; Entregado se rechaza con 400 → usar `finalizar`), `POST /{id}/finalizar` (desde cualquier estado en curso, sin pasos intermedios). Una orden Entregada responde 409 a cualquier modificación. `Features/Servicios/`: `GET /api/servicios?soloActivos=` (solo lectura; el resto del catálogo es la Fase 5). Posición y lado solo se aceptan si el servicio los declara; el precio precarga con `PrecioReferencia` y se congela en `PrecioFinal`.
- **Dos bugs encontrados por los tests y corregidos**: (1) sin `MapInboundClaims = false` el claim `sub` desaparecía y el alta de órdenes daba 500; (2) los enums salían como número en JSON (ahora texto, `JsonStringEnumConverter` global). Documentado en `backend.md`.
- **Tests de backend** (`OrdenesTests`, 14): aislamiento de `OrdenTrabajo` y `OrdenTrabajoServicio` por HTTP (listar, 404 por id, borrar/cambiar estado/finalizar ajenos dan 404 y no modifican, no se puede crear sobre vehículo/servicio de otro taller, `TallerId` del body ignorado, el item hereda `taller_id` solo, 401 sin token en los 8 endpoints), precarga y edición de precio, total, finalizar directo, filtro por estado, 409 en entregadas, precio congelado aunque cambie el de referencia. La factory ahora copia el catálogo a cada taller de prueba (el catálogo es por taller) y crea un usuario real por taller.
- **Frontend**: contrato `ApiOrdenes` con implementación real y mock (`VITE_USAR_MOCKS=true`); `TallerPage` (tablero: tarjetas, filtros por estado, vacío "No hay vehículos en el taller ahora", botón fijo "+ Nuevo vehículo"); paso 2 del alta (`SelectorServicios`: solo servicios activos que aplican al tipo, posición/lado donde corresponde, precio editable); `OrdenDetallePage` (servicios, agregar, **confirmación antes de quitar**, estados opcionales, "Finalizar" que vuelve al tablero; entregada = solo lectura). Estados siempre con texto además del color. Reemplaza a la `InicioPage` provisoria.
- **Playwright**: `frontend/e2e/flujo-taller.spec.ts` + `playwright.config.ts`, script `npm run test:e2e`.

## Verificado

- `dotnet build`: 0 advertencias, 0 errores. `dotnet test`: **40/40** (Testcontainers real).
- `npm run lint`, `npm run build`, `npm test` (**16 tests**: confirmación antes de quitar, "No, dejarlo", finalizar directo sin estados intermedios, entregada solo lectura, aviso de patente conocida, `tiempoTranscurrido`): en verde.
- **E2E real** (`npm run test:e2e`, API + Postgres reales, Chromium a 390 px): login → alta de vehículo → orden con Balanceo (precio de referencia) y Rotación (precio editado a mano) → total $ 27.000 → "Pasar a Listo para retirar" → Finalizar → la orden desaparece del tablero. 1 passed.
- **Recorrido en navegador** a 390 y 1280 px: alta con servicios, tablero, detalle, diálogo de confirmación y finalizar (la tarjeta sale del tablero). Controles ≥ 63 px de alto, texto ≥ 20 px, sin íconos solos. Capturas en [`capturas/fase-3/`](capturas/fase-3/).
- `grep IgnoreQueryFilters|FromSqlRaw`: solo el login.

## No verificado / pendiente

- **El E2E no corre en la CI** (necesita API y base de datos); correrlo es manual (`npm run test:e2e`). Sumarlo a la CI queda como mejora.
- El precio de un servicio ya cargado en una orden no se puede editar después (solo al agregarlo); el plan no pedía un endpoint para eso. RF-18 se cumple al cargar el servicio.
- "Finalizar" no pide confirmación (la pantalla lo describe como el flujo normal de trabajos rápidos); la orden entregada queda bloqueada.
- La pantalla de edición de datos del vehículo sigue pendiente (llega con el historial, Fase 4, según `pantallas.md`).
- Quedan órdenes de prueba en la base de desarrollo local.
- CI de GitHub: anotada abajo al terminar.
