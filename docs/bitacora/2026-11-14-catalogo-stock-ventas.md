# 2026-11-14 — Fases 5 y 6 (Catálogo, Stock, Ventas) y preparación del despliegue

**Fases 5 y 6 cerradas. Fase 7 preparada hasta donde no hace falta una decisión del dueño.**

Pedido del dueño en esta sesión: *terminar de desarrollar el proyecto y dejarlo funcionando completo, con las soluciones más sencillas y eficaces*. Por eso la Fase 6, que esperaba la charla con el dueño del taller, se hizo con el esqueleto mínimo ya documentado (producto = nombre, precio y cantidad).

## Qué se hizo

### Fase 5 — Catálogo de servicios (RF-09 a RF-13)

- **Backend** (`Features/Servicios/`): `POST /api/servicios` (servicio propio: nombre único en el taller, Auto/Moto/Ambos, si pide posición y lado, precio de referencia opcional) y `PATCH /api/servicios/{id}` (activar/desactivar y corregir o quitar el precio de referencia). No existe `DELETE`.
- **Frontend**: pantalla **Ajustes** (`/ajustes`): catálogo con estado en texto ("Activo"/"Desactivado"), botones "Desactivar"/"Activar", "Cambiar precio" y "+ Agregar servicio"; "Cerrar sesión" pasó a vivir acá.

### Fase 6 — Stock y Ventas (RF-24 a RF-29)

- **Backend**: `Features/Stock/` — `GET/POST/PUT/DELETE /api/productos`. El `DELETE` es **baja lógica** (`Producto.Activo`, migración `AgregarActivoAProductoEIndiceDeVentas`): el producto sale del stock pero las ventas viejas lo siguen mostrando. `Features/Ventas/` — `POST /api/ventas` (precio precargado con el del producto y editable; se congela en `PrecioUnitario`; descuento con `ExecuteUpdateAsync` dentro de una transacción; **nunca bloquea** por falta de stock y devuelve `sinStockSuficiente` y `stockRestante`) y `GET /api/historial/ventas`.
- **Frontend**: **Stock** (tarjetas en mobile / tabla en escritorio, alta y edición, eliminar con confirmación), **Ventas** ("+ Nueva venta": buscar producto, cantidad, precio editable, aviso suave si no hay stock, confirmación de venta registrada) e **Historial de ventas** (pestañas Servicios / Ventas dentro de Historial). La navegación quedó completa: Taller / Ventas / Stock / Historial + Ajustes.

### Fase 7 — preparación (sin elegir hosting)

- `Dockerfile` de **imagen única**: la API sirve también el frontend compilado (un servicio, un dominio, sin CORS). `docker-compose.produccion.yml` para ensayarla con su propio Postgres.
- `Program.cs`: migraciones al iniciar (`MigrarAlIniciar=true`) o con el comando `migrar`; cabeceras del proxy, HSTS y redirección a https fuera de desarrollo; la API no arranca si `Jwt:Key` tiene menos de 32 caracteres; CORS solo si se define `FrontendOrigin`.
- Documento nuevo: [`despliegue.md`](../proceso/despliegue.md) (variables, pasos, ensayo, backups, qué falta decidir).

## Verificado (comandos y resultado)

- `dotnet build`: 0 advertencias, 0 errores. `dotnet test`: **72/72** con Testcontainers. Nuevos: servicio desactivado fuera del selector pero visible en órdenes viejas y rechazado en órdenes nuevas; reactivar y cambiar/quitar precio; alta de servicio y sus 400; catálogo aislado por taller; ABM de productos y sus 400; aislamiento de productos y ventas (404/400, `tallerId` del body ignorado); venta que descuenta y congela precio; precio editado; **venta sin stock registrada con stock negativo y aviso**; **20 ventas simultáneas descuentan exacto** (100 → 60); historial de ventas por taller con productos dados de baja; 401 sin token en los 8 endpoints nuevos; regla pura `SinStockSuficiente`.
- **Base desde cero**: en una base nueva (`verificacion_desde_cero`, para no borrar los datos locales), `dotnet ef database update` aplicó las 2 migraciones: 1 taller, 2 tipos de vehículo, 7 servicios.
- `npm run lint`, `npm run build`, `npm test` (**32 tests**; nuevos: aviso de stock en 0 con la venta confirmable, pedir más de lo que hay, confirmación antes de eliminar un producto, alta de producto, desactivar servicio, alta de servicio, precio "A definir").
- **E2E** (`npm run test:e2e`, API y Postgres reales): **3 passed** — flujo del taller (Fases 1 a 4), catálogo (checkpoint Fase 5: servicio propio usado en una orden, desactivado, ya no se ofrece pero la orden vieja lo muestra) y stock/ventas (checkpoint Fase 6: producto con stock 1, dos ventas hasta dejarlo en −2, aviso y no error, historial con el precio de cada venta, baja del producto conservando las ventas).
- **Navegador** a 390 y 1280 px en todas las pantallas nuevas: controles ≥ 63 px de alto, texto ≥ 18 px, sin scroll horizontal, sin íconos solos. Capturas en [`capturas/fase-5/`](capturas/fase-5/) y [`capturas/fase-6/`](capturas/fase-6/).
- **Ensayo de producción** (`docker compose -f docker-compose.produccion.yml up -d --build`, base vacía, secretos aleatorios descartables): migraciones aplicadas al iniciar, `crear-admin` dentro del contenedor, `/` y `/historial` devuelven la web, `/api/ordenes` sin token 401, ruta de API inexistente 404, login correcto e incorrecto, proceso sin privilegios de root, y **los 3 E2E pasando contra `http://localhost:8080`**. `pg_dump` + `pg_restore` a una base nueva: datos restaurados. El ensayo se desmontó al terminar (`down -v`).
- `grep IgnoreQueryFilters|FromSqlRaw`: solo el login.

## No verificado / pendiente

- **Despliegue real**: no se desplegó en ninguna plataforma (falta elegirla). HTTPS, HSTS y la redirección solo se pueden comprobar del todo detrás del proxy de la plataforma.
- La imagen Docker no se construye en la CI; el E2E tampoco corre en la CI (decisión previa).
- En la base de desarrollo local quedaron servicios de prueba "Servicio E2E …" desactivados y productos de prueba dados de baja (los crean los E2E). No molestan en las pantallas de uso diario, pero los servicios se ven en Ajustes.
- Los historiales devuelven como máximo los 200 registros más recientes (sin paginación).
- `Producto` no tiene categorías, proveedores ni códigos: quedan para después de la charla con el dueño del taller.

## Esperando decisión

- **Plataforma de hosting**, cuentas, secretos de producción, dominio y usuario administrador real (pasos en [`despliegue.md`](../proceso/despliegue.md)).
- **Precios de referencia y posición/lado de cada servicio**: confirmarlos con el dueño del taller; se corrigen desde Ajustes.
- **Campos extra de Producto/Venta**, si el dueño del taller los necesita.
