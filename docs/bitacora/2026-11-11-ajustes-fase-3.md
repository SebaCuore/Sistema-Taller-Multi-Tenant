# 2026-11-11 — Ajustes del dueño en la Fase 3

**Fase 3** (cerrada de nuevo). Implementa las decisiones de [Decisiones del dueño tras probar la Fase 3](2026-11-11-decisiones-del-dueno-fase-3.md).

## Qué se hizo

- **Backend**: `PATCH /api/ordenes/{id}/servicios/{itemId}` con `{ precioFinal }` (RF-18): 404 si la orden o el item son de otro taller, 409 si la orden está entregada, 400 si el precio falta o es negativo; devuelve el detalle con el total recalculado.
- **Frontend**: botón visible "Cambiar precio" en cada servicio (campo "Nuevo precio de … ($)", "Guardar precio"/"Cancelar", valida que sean números); "Finalizar" ahora abre la confirmación "¿Finalizar la orden de {patente}?" con "La orden pasa al historial y ya no se va a poder modificar." y los botones "Sí, finalizar" / "No, volver". El texto no dice "se borra". Contrato `ApiOrdenes.cambiarPrecio` con versión real y mock.
- **E2E** actualizado: cambia el precio de un servicio ya cargado (total 27.000 → 28.000), prueba "No, volver" y después "Sí, finalizar".

## Verificado

- `dotnet test`: **45/45** (5 nuevos: camino feliz con recálculo del total, 409 entregada, 400 con `-5` y `null`, aislamiento 404 sin modificar, 401 sin token).
- `npm run lint`, `npm run build` y `npm test` (**20 tests**; nuevos: finalizar sin confirmar no llama a la API, "No, volver", "Sí, finalizar" directo desde Ingresado, cambiar precio, precio inválido, entregada sin "Cambiar precio"): en verde.
- **E2E real** (`npm run test:e2e`, API y Postgres reales): 1 passed.
- **Navegador** a 390 y 1280 px: cambio de precio y confirmación de finalizar; al confirmar la orden sale del tablero. Controles ≥ 63 px. Capturas nuevas en [`capturas/fase-3/`](capturas/fase-3/): `cambiar-precio-*` y `confirmar-finalizar-*`.
- `grep IgnoreQueryFilters|FromSqlRaw`: solo el login.

## No verificado / pendiente

- El E2E sigue sin correr en la CI.
- Durante la sesión el contenedor de Postgres local estaba apagado; se levantó con `docker compose up -d` (los datos persisten).
- CI de GitHub: anotada abajo al terminar.
