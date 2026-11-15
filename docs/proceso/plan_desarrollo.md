# Plan de desarrollo

Ordena la construcción de todo el proyecto — backend y frontend juntos — en fases chicas y verticales: cada fase deja una función completa funcionando de punta a punta (base de datos → API → pantalla), no "todo el frontend primero y después todo el backend". Así cada fase termina en algo que se puede probar de verdad, en vez de un pozo largo de integración al final.

**Cómo usarlo:** una fase a la vez, en orden. Cada fase tiene su checklist (marcalo a medida que avanzás) y termina en un **checkpoint** — una acción concreta que si funciona, confirma que la fase está realmente terminada, no solo que el código compila. No pases a la fase siguiente sin pasar el checkpoint de la actual. Qué hay que comprobar antes de tildar una casilla o cerrar una fase está en [`CLAUDE.md`](../../CLAUDE.md) → "Protocolo de trabajo autónomo" → "Definición de terminado"; lo verificado queda registrado en [`bitacora/`](../bitacora/README.md).

El orden no sigue estrictamente la numeración de [`requerimientos_funcionales.md`](../producto/requerimientos_funcionales.md) ni de [`pantallas.md`](../diseno/pantallas.md) — sigue lo que depende de qué. Por ejemplo, el Catálogo de servicios (RF-09 a RF-13) va después de Órdenes de trabajo aunque esté numerado antes, porque [`pantallas.md`](../diseno/pantallas.md) ya lo marca como "pantalla de uso poco frecuente" y el catálogo por defecto sembrado en la Fase 1 alcanza para probar órdenes de trabajo sin necesitar esa pantalla todavía.

Documentos a mano mientras se avanza: [`backend.md`](../tecnica/backend.md) (cómo se escribe cada endpoint), [`frontend.md`](../tecnica/frontend.md) y [`pantallas.md`](../diseno/pantallas.md) (cómo se arma cada pantalla), [`requerimientos_funcionales.md`](../producto/requerimientos_funcionales.md) (el detalle de cada RF citado acá), [`testing.md`](testing.md) (qué testear en cada fase) y [`flujo_de_trabajo.md`](flujo_de_trabajo.md) (Git, commits y CI).

---

## Fase 0 — Preparar el terreno

No es una "feature" todavía, es dejar los dos proyectos arrancando antes de escribir la primera línea de lógica.

- [x] Inicializar git y crear el repositorio en GitHub siguiendo [`flujo_de_trabajo.md`](flujo_de_trabajo.md): `.gitignore` primero, primer commit con `docs/` y `design/`, push a GitHub.
- [x] Sumar el workflow de GitHub Actions (`.github/workflows/ci.yml`, ejemplo completo en [`flujo_de_trabajo.md`](flujo_de_trabajo.md)) — todavía no tiene nada para compilar, pero queda listo para cuando en la Fase 1 aparezcan los primeros tests.
- [x] PostgreSQL corriendo en Docker: `docker compose up -d` desde la raíz del proyecto, usando el `docker-compose.yml` incluido (ver [`backend.md`](../tecnica/backend.md) → "Cómo arrancar, paso a paso", y la guía de Docker adjunta al proyecto para quien no usó Docker antes). Se eligió Docker por sobre una instalación directa porque de todos modos hace falta para correr los tests de integración con Testcontainers desde la Fase 1 — instalarlo ahora evita repetir el paso, y además es más fácil de resetear si algo queda en un estado raro.
- [x] **Backend**: seguir [`backend.md`](../tecnica/backend.md) → "Cómo arrancar, paso a paso", pasos 1 a 3 (crear el proyecto, agregar paquetes, configurar `user-secrets` con la cadena de conexión). Todavía sin entidades ni endpoints — el objetivo acá es solo `dotnet run` sirviendo la plantilla vacía sin errores.
- [x] **Frontend**: crear el proyecto real (todavía no existe — [`prototipo.html`](../diseno/prototipos/prototipo.html) es solo la referencia visual):

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

  Estructura de carpetas según [`frontend.md`](../tecnica/frontend.md): `src/{components,pages,api,hooks,lib,styles}`.
- [x] Trasladar los tokens de color de [`prototipo.html`](../diseno/prototipos/prototipo.html) (bloque `:root`) a la configuración de Tailwind, tal como indica [`diseno_visual.md`](../diseno/diseno_visual.md).

**Checkpoint:** `dotnet run` (backend) y `npm run dev` (frontend) levantan cada uno sin errores, en pestañas/terminales separadas. Todavía no se hablan entre sí — eso empieza en la Fase 1.

---

## Fase 1 — Base del backend + login de punta a punta

Sin esto no se puede probar nada más con datos reales: todo el resto de las fases necesita un `taller_id` autenticado.

**Backend** (detalle completo en [`backend.md`](../tecnica/backend.md)):
- [x] Las 9 entidades en `Data/Entidades/` (incluidos los enums).
- [x] `TallerDbContext` completo, con los `DbSet` y el filtro global por `taller_id`.
- [x] Una clase de configuración por entidad en `Data/Configuraciones/`.
- [x] Migración inicial con el seed de `Taller`, `TipoVehiculo` (Auto/Moto) y el catálogo por defecto de `Servicio` ([`base_de_datos.md`](../tecnica/base_de_datos.md)).
- [x] `dotnet ef database update` corrido contra Postgres local.
- [x] Autenticación: `POST /auth/login` completo (JWT con claims `sub`/`taller_id`/`rol`), `ITallerActual`/`TallerActual`, manejo global de errores (`IExceptionHandler`), CORS habilitado para `http://localhost:5173`.
- [x] Primer usuario admin creado con `dotnet run -- crear-admin ...`.

**Testing (no se pasa a la Fase 2 sin esto — ver [`testing.md`](testing.md)):**
- [x] `SistemaTallerFactory` (`WebApplicationFactory` + `Testcontainers.PostgreSql`) — la pieza de infraestructura de test que reutiliza el resto del proyecto.
- [x] Las tres pruebas de aislamiento por `taller_id` (a nivel `DbContext`; la versión HTTP se suma con los endpoints de `Vehiculo` en la Fase 2): listar no filtra datos de otro taller, pedir por id un recurso de otro taller da 404, un `TallerId` mandado a mano en el body de un alta se ignora.
- [x] La factory acepta `TESTS_CONNECTION_STRING` para correr sin Docker (ver [`testing.md`](testing.md)).
- [x] Frontend: Vitest + React Testing Library instalados, con script `"test": "vitest run"` en `package.json` (la CI lo empieza a correr sola en cuanto existe).

**Frontend:**
- [x] Pantalla 0 (Login) de [`pantallas.md`](../diseno/pantallas.md), conectada de verdad a `POST /auth/login` — sin mocks acá, es la primera pantalla que tiene sentido contra la API real.
- [x] Guardar el token recibido (memoria/`localStorage` del lado del navegador real, no del prototipo) y mandarlo en el header `Authorization` del resto de los pedidos.
- [x] Mensaje de error simple si falla ("Usuario o contraseña incorrectos", sin detalle técnico — ya especificado en [`pantallas.md`](../diseno/pantallas.md)).

**Checkpoint:** iniciar sesión desde el frontend real con el usuario creado por `crear-admin`, ver en las herramientas de red del navegador que el token viaja en los pedidos siguientes, y los tests de aislamiento de `taller_id` en verde (local y en el Pull Request, vía GitHub Actions).

---

## Fase 2 — Vehículos (RF-05 a RF-08)

**Prerrequisito:** Fase 1 terminada.

**Backend:**
- [x] `POST /api/vehiculos` (alta).
- [x] `GET /api/vehiculos/buscar-por-patente/{patente}` (para RF-07, patente conocida).
- [x] `PUT /api/vehiculos/{id}` (edición, RF-08). Además `GET /api/vehiculos`, `GET /api/vehiculos/{id}` y `GET /api/tipos-vehiculo`. La pantalla de edición todavía no existe (RF-08 cubierto solo en la API).

**Frontend:**
- [x] Pantalla 2 (Nuevo vehículo), paso 1 solo — datos del vehículo, sin servicios todavía (eso es Fase 3).
- [x] Aviso de patente conocida con confirmación explícita (RF-07) — nunca autocompletar en silencio.
- [x] Reemplazar el mock de este flujo por la llamada real a la API (esto valida que el patrón `api/` de [`frontend.md`](../tecnica/frontend.md), intercambiable entre mock y real, funciona como se pensó).

**Testing:**
- [x] Repetir la batería de aislamiento de la Fase 1 para `Vehiculo` (mismo fixture, distinta entidad).
- [x] Test de integración del camino feliz de cada endpoint nuevo.

**Checkpoint:** dar de alta un vehículo desde el frontend, y volver a escribir la misma patente para ver aparecer el aviso de "ya estuvo acá" con el contacto guardado.

---

## Fase 3 — Taller / Órdenes de trabajo (RF-14 a RF-21)

El corazón del sistema. Usa el catálogo de servicios ya sembrado en la Fase 1 — no hace falta la pantalla de Catálogo todavía.

**Prerrequisito:** Fase 2 terminada (una orden siempre cuelga de un vehículo).

**Backend:**
- [x] `GET /api/servicios` (solo lectura, para el selector; el resto del catálogo es la Fase 5).
- [x] `GET /api/ordenes` (listado de activas, con filtro por estado — RF-14).
- [x] `POST /api/ordenes` (alta de orden con uno o varios servicios, posición/lado donde aplique — RF-15 a RF-17).
- [x] `GET /api/ordenes/{id}` (detalle).
- [x] `POST /api/ordenes/{id}/servicios` y `DELETE /api/ordenes/{id}/servicios/{itemId}` (agregar/quitar servicio en curso — RF-19).
- [x] `PATCH /api/ordenes/{id}/estado` (cambio de estado, opcional saltear intermedios — RF-20).
- [x] `POST /api/ordenes/{id}/finalizar` (cierre, pasa a historial — RF-21).

**Frontend:**
- [x] Pantalla 1 (Taller) — tablero con filtro por estado.
- [x] Pantalla 2, paso 2 (elegir servicios al dar de alta la orden).
- [x] Pantalla 3 (Detalle de orden en curso) — agregar/quitar servicio con confirmación antes de quitar (acción destructiva), cambio de estado, finalizar.

**Testing:**
- [x] Batería de aislamiento para `OrdenTrabajo` y `OrdenTrabajoServicio` (esta última tiene `taller_id` propio — ver el fix en [`base_de_datos.md`](../tecnica/base_de_datos.md) — vale la pena un test que confirme específicamente que se completa solo).
- [x] Test de componente (Vitest + RTL) de la confirmación antes de quitar un servicio.
- [x] El primer test end-to-end (Playwright, `frontend/e2e/`, `npm run test:e2e`; necesita API y base levantadas, no corre en la CI) del proyecto: login → alta de vehículo → orden con dos servicios → finalizar. Es el mismo flujo del checkpoint de abajo, ya escrito como test permanente.

**Ajustes pedidos por el dueño al probar la Fase 3 (2026-11-11)** — hacer antes de la Fase 4:
- [x] **Confirmación obligatoria al finalizar** (RF-21): diálogo "¿Finalizar la orden de {patente}?" con el texto "La orden pasa al historial y ya no se va a poder modificar." y los botones "Sí, finalizar" / "No, volver" (mismo estilo que la confirmación de quitar servicio). Test de componente: sin confirmar no se llama a la API; "No, volver" deja la orden como estaba.
- [x] **Precio editable en cualquier momento** (RF-18): `PATCH /api/ordenes/{id}/servicios/{itemId}` con `{ precioFinal }` — 404 si la orden es de otro taller, 409 si está entregada, 400 si el precio es negativo. En el detalle de la orden, cada servicio permite cambiar su precio (botón visible "Cambiar precio", no tocar el número) y el total se recalcula. Tests: integración (camino feliz, 409, 400, aislamiento, 401 sin token) y componente.
- [x] Actualizar el E2E para que confirme al finalizar y cambie el precio de un servicio ya cargado.

**Checkpoint:** flujo completo desde el frontend — alta de vehículo → orden con dos servicios → cambiar el precio de uno ya cargado → marcar "listo para retirar" → finalizar (confirmando) — y que la orden desaparezca del tablero de "Taller".

---

## Fase 4 — Historial de servicios (RF-22, RF-23)

Son consultas de solo lectura sobre lo que ya existe desde la Fase 3 — fase corta.

**Backend:**
- [x] `GET /api/historial/servicios?patente=...` (RF-22, RF-23).

**Frontend:**
- [x] Pantalla 4 (Historial → Servicios), con buscador por patente, tarjetas en mobile / tabla en escritorio. Incluye el contenedor de navegación (barra inferior en mobile, lateral en escritorio; hoy solo Taller e Historial), el detalle histórico de solo lectura (la misma pantalla de la orden, ya entregada) y la edición de datos del vehículo (RF-08) con el botón "Editar datos del vehículo".

**Testing:**
- [x] Test de integración: buscar por patente de otro taller no devuelve resultados (mismo espíritu que la batería de aislamiento, aplicado a una consulta en vez de un CRUD).

**Checkpoint:** buscar por la patente usada en la Fase 3 y ver la orden finalizada con sus servicios y precios.

---

## Fase 5 — Catálogo de servicios / Ajustes (RF-09 a RF-13)

Se puede hacer en cualquier momento después de la Fase 1 (el catálogo ya está sembrado y funcionando) — va acá porque [`pantallas.md`](../diseno/pantallas.md) la marca como de uso poco frecuente, no porque bloquee algo.

**Backend:**
- [x] `GET /api/servicios` (listado del taller).
- [x] `PATCH /api/servicios/{id}` (activar/desactivar — RF-10, nunca `DELETE`). También corrige el precio de referencia (`precioReferencia` / `sinPrecio`).
- [x] `POST /api/servicios` (agregar servicio propio — RF-11 a RF-13).

**Frontend:**
- [x] Pantalla 8 (Catálogo de servicios, dentro de Ajustes).

**Testing:**
- [x] Test de integración: un servicio desactivado no aparece en el selector de servicios de una orden nueva, pero una orden vieja que ya lo tenía lo sigue mostrando.

**Checkpoint:** desactivar un servicio del catálogo y confirmar que ya no aparece como opción al dar de alta una orden nueva (Fase 3), pero sigue visible en órdenes viejas que ya lo usaban.

---

## Fase 6 — Stock y Ventas (RF-24 a RF-29)

**Decisión del dueño (2026-11-14):** terminar el proyecto con la solución más sencilla. La fase se hizo con el esqueleto mínimo de [`base_de_datos.md`](../tecnica/base_de_datos.md) (nombre, precio, cantidad); categorías, proveedores y códigos quedan para después de la charla con el dueño del taller y se agregan con una migración. Texto original del bloqueo: **Prerrequisito bloqueante, no técnico:** el detalle fino de `Producto`/`Venta` (categorías, proveedores, códigos) sigue pendiente de la charla con el dueño del taller ([`mvp.md`](../producto/mvp.md)). No conviene escribir el backend de esto hasta cerrar esa charla — el esqueleto mínimo ya definido en [`base_de_datos.md`](../tecnica/base_de_datos.md) alcanza para lo funcional, pero conviene confirmar que no falta nada antes de dar por cerrada la fase.

**Backend:**
- [x] `GET/POST/PUT/DELETE /api/productos` (RF-24, RF-25). El `DELETE` es una baja lógica (`Producto.Activo = false`): el producto sale del stock pero las ventas viejas lo siguen mostrando.
- [x] `POST /api/ventas` con descuento de stock vía `ExecuteUpdateAsync` (RF-26 a RF-29), incluyendo el flag `sinStockSuficiente` en vez de bloquear la venta (RF-28).
- [x] `GET /api/historial/ventas`.

**Frontend:**
- [x] Pantalla 7 (Stock).
- [x] Pantalla 5 (Ventas), con el aviso suave de stock en 0.
- [x] Pantalla 6 (Historial → Ventas).

**Testing:**
- [x] Batería de aislamiento para `Producto` y `Venta`.
- [x] Unit test puro de la regla de stock negativo (RF-28): dado cantidad y stock actual, ¿arma bien el flag `sinStockSuficiente`? (sin base de datos — ver [`testing.md`](testing.md)).
- [x] Test de integración del descuento atómico de stock con `ExecuteUpdateAsync`.

**Checkpoint:** cargar un producto, venderlo hasta dejarlo en stock negativo, y confirmar que la venta se registra igual con el aviso, no con un error.

---

## Fase 6.5 — Puesta a punto para el piloto

Agregada el 2026-11-15, después de revisar el proyecto con vistas a probarlo con un primer taller. Son los huecos que hay que cerrar **antes de cargar datos reales**: ninguno es una función nueva del taller, todos son seguridad y operación. Va antes de la Fase 7 porque no depende de elegir hosting y porque la Fase 7 termina con datos reales adentro. Lo que hace falta para ofrecerlo a más talleres está al final del documento, en "Después del piloto".

**Decisiones tomadas por defecto** (las más sencillas; si el dueño prefiere otra cosa, se cambia antes de empezar el ítem):

- La sesión dura **30 días** en vez de 8 horas. Sin refresh tokens: solo se alarga el vencimiento del JWT. Costo aceptado: un token robado vale hasta que vence, y cambiar la contraseña no cierra las sesiones abiertas (cambiar `Jwt__Key` sí las cierra todas).
- El email pasa a ser **único en todo el sistema**, no solo dentro de un taller. Es lo que el login ya asume.
- En el piloto el taller usa **un solo usuario compartido**. Alta de empleados y roles quedan para después.

**Backend:**

- [x] **Límite de intentos en `POST /auth/login`.** Hoy acepta intentos ilimitados. Usar el rate limiter que trae ASP.NET Core (`AddRateLimiter`, sin paquetes nuevos): ventana fija por IP, 10 intentos por minuto, respuesta `429`. `UseRateLimiter` va después de `UseForwardedHeaders`, para que la IP sea la del cliente y no la del proxy del hosting. El resto de los endpoints no se limita.
- [x] **Cambio de contraseña con sesión iniciada**: `POST /auth/cambiar-password` (`RequireAuthorization`), recibe la contraseña actual y la nueva (8 caracteres o más). Contraseña actual incorrecta → `400` con mensaje claro, no `401` (el frontend trata el `401` como sesión vencida y manda al login).
- [x] **Recuperación cuando se olvidó la contraseña**: comando `dotnet SistemaTaller.Api.dll cambiar-password <email> "<nueva>"`, hermano de `crear-admin`. Lo corre el dueño del proyecto desde la consola del hosting; no hay recuperación por email en el piloto.
- [x] **Email único global.** El índice hoy es `(TallerId, Email)`, pero el login busca por email con `IgnoreQueryFilters()` y toma el primero: con dos talleres y un mismo email entra al taller equivocado. Migración que reemplaza el índice por uno único sobre `Email`; `crear-admin` valida contra todos los talleres. Actualizar [`backend.md`](../tecnica/backend.md) y [`base_de_datos.md`](../tecnica/base_de_datos.md), que hoy dicen "único dentro de un taller".
- [x] **Sesión de 30 días**: `expires` en `AuthEndpoints` y la mención de las 8 horas en [`backend.md`](../tecnica/backend.md).
- [x] **Endpoint de salud** `GET /salud`, sin autenticación: `200` si la API responde y la base acepta conexiones (`Database.CanConnectAsync`), `503` si no. No devuelve datos ni versión. Tiene que quedar fuera del fallback que sirve `index.html`.
- [x] **Logs útiles para diagnosticar**: registrar cada login fallido (email e IP, nunca la contraseña) y cada `429`. Los errores no manejados ya se registran en `GlobalExceptionHandler`. Todo a la salida estándar, que es lo que guarda el hosting.
- [x] **Corregir un servicio del catálogo.** `PATCH /api/servicios/{id}` hoy solo cambia activo y precio; nombre, categoría y si pide posición/lado no se pueden corregir. Ampliar el `PATCH` y la pantalla de Ajustes para editarlos. Los 7 servicios sembrados salen del prototipo y hay que poder ajustarlos al taller real sin tocar la base.

**Frontend:**

- [x] **Ajustes → "Cambiar contraseña"**: contraseña actual, nueva y repetir nueva; confirmación de que se guardó. Mismas reglas de accesibilidad que el resto (pantalla completa en mobile, modal en escritorio).
- [x] **Mensaje para el `429` en el login**: "Demasiados intentos. Esperá un minuto y probá de nuevo." Hoy mostraría un error genérico.
- [x] **Ajustes → editar servicio** (nombre, Auto/Moto/Ambos, pide posición, pide lado), junto al "Cambiar precio" que ya existe.
- [ ] Confirmar a mano que con la sesión de 30 días el celular no vuelve a pedir login al cerrar y reabrir el navegador.

**Testing:**

- [x] Login: el intento número 11 en un minuto da `429`, y un login correcto después de la ventana vuelve a funcionar.
- [x] Cambio de contraseña: con la actual correcta, la vieja deja de servir y la nueva entra; con la actual incorrecta, `400` y nada cambia; sin token, `401`; nueva de menos de 8 caracteres, `400`. Un usuario no puede cambiar la contraseña de otro taller (sale del token, no del body).
- [x] Email único: `crear-admin` con un email que ya existe en otro taller falla; la migración aplica sobre una base desde cero.
- [x] `/salud`: `200` sin token.
- [x] Edición de servicio: aislamiento por `taller_id` (404 sobre un servicio de otro taller) y nombre duplicado → `400`.
- [x] Test de frontend del formulario de cambio de contraseña y del mensaje de `429`.
- [x] Sumar al E2E: cambiar la contraseña, cerrar sesión y volver a entrar con la nueva.
- [x] Repetir el ensayo de producción de [`despliegue.md`](despliegue.md) con todo lo anterior y los E2E pasando contra `http://localhost:8080`.

**Documentación:**

- [x] [`despliegue.md`](despliegue.md): el comando `cambiar-password`, el endpoint `/salud` y cómo apuntarle un monitor.
- [x] [`requerimientos_funcionales.md`](../producto/requerimientos_funcionales.md): sumar como RF el cambio de contraseña y la edición de servicios.
- [x] [`pantallas.md`](../diseno/pantallas.md): los dos formularios nuevos de Ajustes.

**Checkpoint:** en el ensayo de producción local, cambiar la contraseña desde Ajustes, cerrar sesión y entrar con la nueva; fallar el login 11 veces seguidas y ver el aviso de "demasiados intentos"; `GET /salud` responde `200`.

---

## Fase 7 — Endurecer y mostrarlo en serio

Ya no es "una feature" — es la puesta a punto antes de que el dueño real lo use con datos reales.

- [x] Toda la suite de tests (`dotnet test`, `npm test`, el/los E2E de Playwright) en verde en `main`, no solo en la rama de la última fase.
- [x] **Preparado sin elegir plataforma** (2026-11-14): imagen Docker única que sirve API + frontend, migraciones al iniciar, HSTS/redirección detrás del proxy, y un ensayo de producción local con los E2E pasando — ver [`despliegue.md`](despliegue.md).
- [ ] Elegir la plataforma de hosting (candidatas en [`arquitectura.md`](../tecnica/arquitectura.md): Render, Railway, Fly.io, Azure App Service) y desplegar backend + base de datos.
- [ ] Variables de entorno de producción (cadena de conexión, clave JWT) — nunca las de desarrollo reusadas.
- [ ] HTTPS confirmado de punta a punta (frontend y backend).
- [ ] Backup automático de la base de datos configurado ([`arquitectura.md`](../tecnica/arquitectura.md)).
- [ ] Probar el sistema completo en el celular, en el taller si es posible, con el dueño mirando — no solo en el escritorio de desarrollo.
- [ ] Revisar junto con el dueño si algo de lo que quedó "fuera del MVP" ([`mvp.md`](../producto/mvp.md)) pasó a ser urgente después de ver el sistema funcionando de verdad.

**Agregado el 2026-11-15 — condiciones para arrancar el piloto con el primer taller.** Todas dependen del dueño del proyecto; ninguna es código.

- [ ] Fase 6.5 desplegada (el código está hecho y probado en el ensayo local; falta el despliegue real). No cargar datos reales antes.
- [ ] **Restauración probada**: bajar un backup del hosting y restaurarlo en una base nueva. Un backup que nunca se restauró no cuenta.
- [ ] **Monitor externo** gratuito (UptimeRobot o similar) apuntando a `/salud`, con aviso al celular del dueño del proyecto.
- [ ] **Saber dónde se leen los logs** en la plataforma elegida y cuántos días los guarda.
- [ ] **Base de producción limpia**: creada desde cero, sin los "Servicio E2E …" ni productos de prueba de la base de desarrollo. Los E2E no se corren contra producción.
- [ ] **Datos del taller real** cargados con el dueño del taller: nombre del taller (hoy sembrado como "San Martín"; si es otro, cambiarlo), servicios con sus precios de referencia y posición/lado desde Ajustes, y stock inicial si va a usar Ventas.
- [ ] **Usuario real** creado con `crear-admin`, con el email del taller y una contraseña que el dueño del taller cambie él mismo desde Ajustes.
- [ ] **Acuerdo de piloto por escrito** (un mensaje alcanza): que es una prueba, cuánto dura (4 a 6 semanas), que es gratis o con descuento, que sus datos son suyos y se los entregás si deja de usarlo, y que guarda nombres y teléfonos de sus clientes (Ley 25.326 de datos personales: usarlos solo para el taller).
- [ ] **Plan si algo falla**: el taller sigue con su cuaderno o método actual en paralelo la primera semana, y sabe a quién escribirle.
- [ ] **Canal de feedback y seguimiento**: un chat con el dueño del taller y una charla semanal fija. Anotar qué usa todos los días, dónde se traba y qué pide.
- [ ] **Criterio de éxito definido antes de empezar**: por ejemplo, que al final del piloto siga cargando órdenes todos los días sin que se lo recuerden, y que diga cuánto pagaría.

**Checkpoint:** el dueño del taller carga un vehículo real, con datos reales, desde su propio celular.
---

## Después del piloto — antes de ofrecerlo a más talleres

No es una fase todavía: es la lista de lo que se sabe que falta, para ordenarla con lo que salga del piloto. No construir nada de esto antes de terminarlo.

- **Alta de talleres**: hoy no hay forma de crear un segundo taller (`crear-admin` asigna siempre al primero). Falta el alta con su catálogo inicial de servicios.
- **Usuarios y roles**: alta de empleados desde la aplicación y, si hace falta, un rol distinto de `Admin`.
- **Recuperación de contraseña por email**, y cierre de sesiones al cambiar la contraseña.
- **Lo comercial**: planes, cobro, términos de uso y política de datos personales.
- **Límites conocidos**: los historiales devuelven como máximo 200 registros (sin paginación); no hay exportación de datos ni reportes (caja del día, totales por período).
- **Lo que está fuera del MVP** ([`mvp.md`](../producto/mvp.md)): facturación electrónica, avisos al cliente, presupuestos, detalle de Stock. El piloto dice cuáles importan.
- **CI**: construir la imagen Docker y correr los E2E en la CI antes de tener más de un cliente dependiendo de `main`.
