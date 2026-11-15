# 2026-11-15 — Fase 6.5: puesta a punto para el piloto

**Fase 6.5 hecha en código y probada en el ensayo de producción local. Falta solo comprobar a mano la sesión de 30 días en un celular real, y el despliegue (Fase 7).**

Decisiones confirmadas por el dueño al empezar: las tres por defecto del plan (sesión de 30 días, email único global, un solo usuario compartido) y, para la edición de servicios, que **el cambio vale también para las órdenes viejas** (no se congela el nombre en cada orden).

## Qué se hizo

- **Login** (`Features/Auth/AuthEndpoints.cs`): límite de 10 intentos por minuto por IP con el rate limiter de ASP.NET Core (`429`; configurable con `Login__IntentosPorVentana` / `Login__VentanaSegundos`), `UseRateLimiter` después de `UseForwardedHeaders`; sesión de 30 días; log de cada login fallido (email e IP, sin contraseña, con el email saneado) y de cada `429`.
- **`POST /auth/cambiar-password`**: el usuario sale del claim `sub`; actual incorrecta o nueva corta/larga → `400` con mensaje por campo; sin token `401`.
- **Comando `cambiar-password <email> "<nueva>"`** y **email único global** (migración `EmailUnicoEnTodoElSistema`; `crear-admin` valida contra todos los talleres). Los comandos viven ahora en `Features/Auth/ComandosUsuarios.cs` y recorren los talleres con un contexto por taller, **sin** `IgnoreQueryFilters` (el `grep` sigue mostrando solo el login).
- **`GET /salud`**: `200`/`503` según `CanConnectAsync`, sin token ni cuerpo.
- **`PATCH /api/servicios/{id}`** acepta nombre, tipo, posición y lado (nombre vacío o repetido → `400`).
- **Frontend**: Ajustes → "Cambiar contraseña" y "Editar servicio"; aviso del `429` en el login; `ErrorApi` ahora trae los errores por campo del `400`.
- **Documentación**: `despliegue.md` (sección "Operación del piloto"), RF-30/31/32, `pantallas.md`, `backend.md`, `base_de_datos.md`, `testing.md`, plan y `CLAUDE.md`.

## Verificado

- `dotnet build`: 0 advertencias, 0 errores. `dotnet test` (Testcontainers): **84/84** (12 nuevos: 30 días, 11.º intento `429` y recuperación tras la ventana, cambio de contraseña correcto/incorrecto/corta/sin token/otro taller, email repetido rechazado por la base, `/salud` sin token, edición de servicio, duplicado/vacío, 404 entre talleres).
- `npm run lint`, `npm run build`, `npm test` (**37**; nuevos: aviso `429`, edición de servicio, formulario de contraseña en sus tres casos).
- Base de desarrollo: `dotnet ef database update` aplicó la migración nueva. Comando `cambiar-password`: caso correcto, email inexistente y contraseña corta.
- **Ensayo de producción** (`docker-compose.produccion.yml`, base vacía, secretos aleatorios): migraciones al iniciar, `crear-admin` (y rechazo de un duplicado en otra capitalización), `cambiar-password`, `/salud` `200` sin token y sin cuerpo, `cambiar-password` sin token `401`, y **los 5 E2E pasando contra `http://localhost:8080`** (incluye cambiar la contraseña, salir y entrar con la nueva, y ver el aviso de demasiados intentos). Los logs muestran los logins fallidos y ninguna contraseña. Ensayo desmontado con `down -v`.
- Capturas a 390 y 1280 px de los dos formularios nuevos: [`capturas/fase-6-5/`](capturas/fase-6-5/). Controles de 56 px, texto de 18 px, errores en texto, sin scroll horizontal.
- Se usó el Chrome instalado (`PLAYWRIGHT_CHROMIUM_PATH`) porque el Chromium de Playwright no estaba descargado.

## No verificado / pendiente

- **Sesión de 30 días en el celular** (cerrar y reabrir el navegador sin que pida login): solo se comprobó que el token vence a los 30 días. Es la única casilla sin tildar de la fase.
- **El E2E `flujo-taller` falla contra la base de desarrollo** porque ahí el precio de referencia de Balanceo es 10.000 y no el 18.000 sembrado (alguien lo cambió desde Ajustes); en una base limpia pasa. No se tocó la base de desarrollo.
- El E2E del límite agota el cupo de la IP: correr con `--workers=1` y no repetir la corrida antes de un minuto (ver [`testing.md`](../proceso/testing.md)).
- El límite por IP necesita que el hosting mande `X-Forwarded-For`; confirmarlo al elegir plataforma.

## Esperando decisión

Nada nuevo. Sigue pendiente la Fase 7 (plataforma, cuentas, secretos).
