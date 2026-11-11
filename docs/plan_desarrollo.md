# Plan de desarrollo

Ordena la construcción de todo el proyecto — backend y frontend juntos — en fases chicas y verticales: cada fase deja una función completa funcionando de punta a punta (base de datos → API → pantalla), no "todo el frontend primero y después todo el backend". Así cada fase termina en algo que se puede probar de verdad, en vez de un pozo largo de integración al final.

**Cómo usarlo:** una fase a la vez, en orden. Cada fase tiene su checklist (marcalo a medida que avanzás) y termina en un **checkpoint** — una acción concreta que si funciona, confirma que la fase está realmente terminada, no solo que el código compila. No pases a la fase siguiente sin pasar el checkpoint de la actual. Qué hay que comprobar antes de tildar una casilla o cerrar una fase está en `CLAUDE.md` → "Protocolo de trabajo autónomo" → "Definición de terminado"; lo verificado queda registrado en `bitacora.md`.

El orden no sigue estrictamente la numeración de `functional_requirements.md` ni de `screens.md` — sigue lo que depende de qué. Por ejemplo, el Catálogo de servicios (RF-09 a RF-13) va después de Órdenes de trabajo aunque esté numerado antes, porque `screens.md` ya lo marca como "pantalla de uso poco frecuente" y el catálogo por defecto sembrado en la Fase 1 alcanza para probar órdenes de trabajo sin necesitar esa pantalla todavía.

Documentos a mano mientras se avanza: `backend.md` (cómo se escribe cada endpoint), `frontend.md` y `screens.md` (cómo se arma cada pantalla), `functional_requirements.md` (el detalle de cada RF citado acá), `testing.md` (qué testear en cada fase) y `flujo_de_trabajo.md` (Git, commits y CI).

---

## Fase 0 — Preparar el terreno

No es una "feature" todavía, es dejar los dos proyectos arrancando antes de escribir la primera línea de lógica.

- [x] Inicializar git y crear el repositorio en GitHub siguiendo `flujo_de_trabajo.md`: `.gitignore` primero, primer commit con `docs/` y `design/`, push a GitHub.
- [x] Sumar el workflow de GitHub Actions (`.github/workflows/ci.yml`, ejemplo completo en `flujo_de_trabajo.md`) — todavía no tiene nada para compilar, pero queda listo para cuando en la Fase 1 aparezcan los primeros tests.
- [x] PostgreSQL corriendo en Docker: `docker compose up -d` desde la raíz del proyecto, usando el `docker-compose.yml` incluido (ver `backend.md` → "Cómo arrancar, paso a paso", y la guía de Docker adjunta al proyecto para quien no usó Docker antes). Se eligió Docker por sobre una instalación directa porque de todos modos hace falta para correr los tests de integración con Testcontainers desde la Fase 1 — instalarlo ahora evita repetir el paso, y además es más fácil de resetear si algo queda en un estado raro.
- [x] **Backend**: seguir `backend.md` → "Cómo arrancar, paso a paso", pasos 1 a 3 (crear el proyecto, agregar paquetes, configurar `user-secrets` con la cadena de conexión). Todavía sin entidades ni endpoints — el objetivo acá es solo `dotnet run` sirviendo la plantilla vacía sin errores.
- [x] **Frontend**: crear el proyecto real (todavía no existe — `design/prototipo.html` es solo la referencia visual):

  ```bash
  npm create vite@latest frontend -- --template react-ts
  cd frontend
  npm install

  # Tailwind CSS (Tailwind 4, plugin de Vite)
  npm install tailwindcss @tailwindcss/vite
  # reemplazar el contenido de src/index.css por: @import "tailwindcss";
  # agregar el alias "@/*": ["./src/*"] en tsconfig.json y tsconfig.app.json
  # sumar el plugin tailwindcss() en vite.config.ts (ver ejemplo en la doc oficial de shadcn/ui)

  npm install -D @types/node
  npx shadcn@latest init

  npm install react-router-dom @tanstack/react-query lucide-react
  ```

  Estructura de carpetas según `frontend.md`: `src/{components,pages,api,hooks,lib,styles}`.
- [x] Trasladar los tokens de color de `design/prototipo.html` (bloque `:root`) a la configuración de Tailwind, tal como indica `design.md`.

**Checkpoint:** `dotnet run` (backend) y `npm run dev` (frontend) levantan cada uno sin errores, en pestañas/terminales separadas. Todavía no se hablan entre sí — eso empieza en la Fase 1.

---

## Fase 1 — Base del backend + login de punta a punta

Sin esto no se puede probar nada más con datos reales: todo el resto de las fases necesita un `taller_id` autenticado.

**Backend** (detalle completo en `backend.md`):
- [x] Las 9 entidades en `Data/Entidades/` (incluidos los enums).
- [ ] `TallerDbContext` completo, con los `DbSet` y el filtro global por `taller_id`.
- [ ] Una clase de configuración por entidad en `Data/Configuraciones/`.
- [ ] Migración inicial con el seed de `Taller`, `TipoVehiculo` (Auto/Moto) y el catálogo por defecto de `Servicio` (`data_base.md`).
- [ ] `dotnet ef database update` corrido contra Postgres local.
- [ ] Autenticación: `POST /auth/login` completo (JWT con claims `sub`/`taller_id`/`rol`), `ITallerActual`/`TallerActual`, manejo global de errores (`IExceptionHandler`), CORS habilitado para `http://localhost:5173`.
- [ ] Primer usuario admin creado con `dotnet run -- crear-admin ...`.

**Testing (no se pasa a la Fase 2 sin esto — ver `testing.md`):**
- [ ] `SistemaTallerFactory` (`WebApplicationFactory` + `Testcontainers.PostgreSql`) — la pieza de infraestructura de test que reutiliza el resto del proyecto.
- [ ] Las tres pruebas de aislamiento por `taller_id`: listar no filtra datos de otro taller, pedir por id un recurso de otro taller da 404, un `TallerId` mandado a mano en el body de un alta se ignora.
- [ ] La factory acepta `TESTS_CONNECTION_STRING` para correr sin Docker (ver `testing.md`).
- [ ] Frontend: Vitest + React Testing Library instalados, con script `"test": "vitest"` en `package.json` (la CI lo empieza a correr sola en cuanto existe).

**Frontend:**
- [ ] Pantalla 0 (Login) de `screens.md`, conectada de verdad a `POST /auth/login` — sin mocks acá, es la primera pantalla que tiene sentido contra la API real.
- [ ] Guardar el token recibido (memoria/`localStorage` del lado del navegador real, no del prototipo) y mandarlo en el header `Authorization` del resto de los pedidos.
- [ ] Mensaje de error simple si falla ("Usuario o contraseña incorrectos", sin detalle técnico — ya especificado en `screens.md`).

**Checkpoint:** iniciar sesión desde el frontend real con el usuario creado por `crear-admin`, ver en las herramientas de red del navegador que el token viaja en los pedidos siguientes, y los tests de aislamiento de `taller_id` en verde (local y en el Pull Request, vía GitHub Actions).

---

## Fase 2 — Vehículos (RF-05 a RF-08)

**Prerrequisito:** Fase 1 terminada.

**Backend:**
- [ ] `POST /api/vehiculos` (alta).
- [ ] `GET /api/vehiculos/buscar-por-patente/{patente}` (para RF-07, patente conocida).
- [ ] `PUT /api/vehiculos/{id}` (edición, RF-08).

**Frontend:**
- [ ] Pantalla 2 (Nuevo vehículo), paso 1 solo — datos del vehículo, sin servicios todavía (eso es Fase 3).
- [ ] Aviso de patente conocida con confirmación explícita (RF-07) — nunca autocompletar en silencio.
- [ ] Reemplazar el mock de este flujo por la llamada real a la API (esto valida que el patrón `api/` de `frontend.md`, intercambiable entre mock y real, funciona como se pensó).

**Testing:**
- [ ] Repetir la batería de aislamiento de la Fase 1 para `Vehiculo` (mismo fixture, distinta entidad).
- [ ] Test de integración del camino feliz de cada endpoint nuevo.

**Checkpoint:** dar de alta un vehículo desde el frontend, y volver a escribir la misma patente para ver aparecer el aviso de "ya estuvo acá" con el contacto guardado.

---

## Fase 3 — Taller / Órdenes de trabajo (RF-14 a RF-21)

El corazón del sistema. Usa el catálogo de servicios ya sembrado en la Fase 1 — no hace falta la pantalla de Catálogo todavía.

**Prerrequisito:** Fase 2 terminada (una orden siempre cuelga de un vehículo).

**Backend:**
- [ ] `GET /api/ordenes` (listado de activas, con filtro por estado — RF-14).
- [ ] `POST /api/ordenes` (alta de orden con uno o varios servicios, posición/lado donde aplique — RF-15 a RF-17).
- [ ] `GET /api/ordenes/{id}` (detalle).
- [ ] `POST /api/ordenes/{id}/servicios` y `DELETE /api/ordenes/{id}/servicios/{itemId}` (agregar/quitar servicio en curso — RF-19).
- [ ] `PATCH /api/ordenes/{id}/estado` (cambio de estado, opcional saltear intermedios — RF-20).
- [ ] `POST /api/ordenes/{id}/finalizar` (cierre, pasa a historial — RF-21).

**Frontend:**
- [ ] Pantalla 1 (Taller) — tablero con filtro por estado.
- [ ] Pantalla 2, paso 2 (elegir servicios al dar de alta la orden).
- [ ] Pantalla 3 (Detalle de orden en curso) — agregar/quitar servicio con confirmación antes de quitar (acción destructiva), cambio de estado, finalizar.

**Testing:**
- [ ] Batería de aislamiento para `OrdenTrabajo` y `OrdenTrabajoServicio` (esta última tiene `taller_id` propio — ver el fix en `data_base.md` — vale la pena un test que confirme específicamente que se completa solo).
- [ ] Test de componente (Vitest + RTL) de la confirmación antes de quitar un servicio.
- [ ] El primer test end-to-end (Playwright) del proyecto: login → alta de vehículo → orden con dos servicios → finalizar. Es el mismo flujo del checkpoint de abajo, ya escrito como test permanente.

**Checkpoint:** flujo completo desde el frontend — alta de vehículo → orden con dos servicios → marcar "listo para retirar" → finalizar — y que la orden desaparezca del tablero de "Taller".

---

## Fase 4 — Historial de servicios (RF-22, RF-23)

Son consultas de solo lectura sobre lo que ya existe desde la Fase 3 — fase corta.

**Backend:**
- [ ] `GET /api/historial/servicios?patente=...` (RF-22, RF-23).

**Frontend:**
- [ ] Pantalla 4 (Historial → Servicios), con buscador por patente, tarjetas en mobile / tabla en escritorio.

**Testing:**
- [ ] Test de integración: buscar por patente de otro taller no devuelve resultados (mismo espíritu que la batería de aislamiento, aplicado a una consulta en vez de un CRUD).

**Checkpoint:** buscar por la patente usada en la Fase 3 y ver la orden finalizada con sus servicios y precios.

---

## Fase 5 — Catálogo de servicios / Ajustes (RF-09 a RF-13)

Se puede hacer en cualquier momento después de la Fase 1 (el catálogo ya está sembrado y funcionando) — va acá porque `screens.md` la marca como de uso poco frecuente, no porque bloquee algo.

**Backend:**
- [ ] `GET /api/servicios` (listado del taller).
- [ ] `PATCH /api/servicios/{id}` (activar/desactivar — RF-10, nunca `DELETE`).
- [ ] `POST /api/servicios` (agregar servicio propio — RF-11 a RF-13).

**Frontend:**
- [ ] Pantalla 8 (Catálogo de servicios, dentro de Ajustes).

**Testing:**
- [ ] Test de integración: un servicio desactivado no aparece en el selector de servicios de una orden nueva, pero una orden vieja que ya lo tenía lo sigue mostrando.

**Checkpoint:** desactivar un servicio del catálogo y confirmar que ya no aparece como opción al dar de alta una orden nueva (Fase 3), pero sigue visible en órdenes viejas que ya lo usaban.

---

## Fase 6 — Stock y Ventas (RF-24 a RF-29)

**Prerrequisito bloqueante, no técnico:** el detalle fino de `Producto`/`Venta` (categorías, proveedores, códigos) sigue pendiente de la charla con el dueño del taller (`mvp.md`). No conviene escribir el backend de esto hasta cerrar esa charla — el esqueleto mínimo ya definido en `data_base.md` alcanza para lo funcional, pero conviene confirmar que no falta nada antes de dar por cerrada la fase.

**Backend:**
- [ ] `GET/POST/PUT/DELETE /api/productos` (RF-24, RF-25).
- [ ] `POST /api/ventas` con descuento de stock vía `ExecuteUpdateAsync` (RF-26 a RF-29), incluyendo el flag `sinStockSuficiente` en vez de bloquear la venta (RF-28).
- [ ] `GET /api/historial/ventas`.

**Frontend:**
- [ ] Pantalla 7 (Stock).
- [ ] Pantalla 5 (Ventas), con el aviso suave de stock en 0.
- [ ] Pantalla 6 (Historial → Ventas).

**Testing:**
- [ ] Batería de aislamiento para `Producto` y `Venta`.
- [ ] Unit test puro de la regla de stock negativo (RF-28): dado cantidad y stock actual, ¿arma bien el flag `sinStockSuficiente`? (sin base de datos — ver `testing.md`).
- [ ] Test de integración del descuento atómico de stock con `ExecuteUpdateAsync`.

**Checkpoint:** cargar un producto, venderlo hasta dejarlo en stock negativo, y confirmar que la venta se registra igual con el aviso, no con un error.

---

## Fase 7 — Endurecer y mostrarlo en serio

Ya no es "una feature" — es la puesta a punto antes de que el dueño real lo use con datos reales.

- [ ] Toda la suite de tests (`dotnet test`, `npm test`, el/los E2E de Playwright) en verde en `main`, no solo en la rama de la última fase.
- [ ] Elegir la plataforma de hosting (candidatas en `architecture.md`: Render, Railway, Fly.io, Azure App Service) y desplegar backend + base de datos.
- [ ] Variables de entorno de producción (cadena de conexión, clave JWT) — nunca las de desarrollo reusadas.
- [ ] HTTPS confirmado de punta a punta (frontend y backend).
- [ ] Backup automático de la base de datos configurado (`architecture.md`).
- [ ] Probar el sistema completo en el celular, en el taller si es posible, con el dueño mirando — no solo en el escritorio de desarrollo.
- [ ] Revisar junto con el dueño si algo de lo que quedó "fuera del MVP" (`mvp.md`) pasó a ser urgente después de ver el sistema funcionando de verdad.

**Checkpoint:** el dueño del taller carga un vehículo real, con datos reales, desde su propio celular.
