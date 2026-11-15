# Despliegue

Cómo se pone el sistema en producción (Fase 7 de [`plan_desarrollo.md`](plan_desarrollo.md)). Lo que está acá ya está hecho y probado en local; **lo que falta lo decide el dueño**: elegir la plataforma, crear las cuentas y cargar los secretos reales (ver "Qué falta decidir", al final).

## Cómo se empaqueta

Una sola imagen Docker ([`Dockerfile`](../../Dockerfile), en la raíz): compila el frontend (`npm run build`), publica la API y deja el frontend dentro de `wwwroot`. **La misma API sirve la web**: un solo servicio, un solo dominio, sin CORS ni una segunda aplicación que mantener. Las candidatas de [`arquitectura.md`](../tecnica/arquitectura.md) (Render, Railway, Fly.io, Azure App Service) aceptan un `Dockerfile` tal cual.

- Rutas `/api/...` y `/auth/...` → la API. Cualquier otra ruta (`/historial`, `/ordenes/…`) → `index.html`, para que recargar la página funcione.
- El contenedor escucha en el puerto **8080** y corre con un usuario sin privilegios.
- En desarrollo nada cambia: Vite en `5173`, la API en `5108`, CORS abierto solo para ese origen.

## Variables de entorno

| Variable | Obligatoria | Qué es |
|---|---|---|
| `ConnectionStrings__Default` | Sí | Cadena de conexión a PostgreSQL, por ejemplo `Host=…;Database=sistema_taller;Username=…;Password=…` |
| `Jwt__Key` | Sí | Clave de firma de los tokens, **32 caracteres o más**, aleatoria. La API no arranca si falta o es corta. Nunca reusar la de desarrollo. Cambiarla cierra todas las sesiones. |
| `MigrarAlIniciar` | Recomendada (`true`) | Aplica las migraciones pendientes (y los datos iniciales) cada vez que arranca. Alternativa manual: `dotnet SistemaTaller.Api.dll migrar`. |
| `FrontendOrigin` | No | Solo si el frontend se sirviera desde otro dominio. Con la imagen única se deja sin definir. |

Para generar una clave: `python -c "import secrets; print(secrets.token_urlsafe(48))"`.

## Pasos en cualquier plataforma

1. Crear una base PostgreSQL administrada (el sistema se desarrolla y se prueba con la versión 18) y copiar su cadena de conexión.
2. Crear el servicio web a partir del repositorio (detecta el `Dockerfile`), puerto 8080, con las variables de la tabla.
3. Primer arranque: con `MigrarAlIniciar=true` se crean las tablas y los datos iniciales (taller "San Martín", tipos Auto/Moto y los 7 servicios por defecto).
4. Crear el usuario administrador, una sola vez, desde la consola de la plataforma:
   `dotnet SistemaTaller.Api.dll crear-admin <email> "<contraseña de 8+ caracteres>"`
5. Entrar por el dominio `https://…` que da la plataforma e iniciar sesión.
6. Activar el **backup automático diario** de la base en el panel de la plataforma (ver abajo).

HTTPS lo termina la plataforma; la API ya lee `X-Forwarded-Proto`, envía HSTS y redirige a `https` fuera de desarrollo.

## Ensayo en una sola máquina

[`docker-compose.produccion.yml`](../../docker-compose.produccion.yml) levanta la imagen con su propio Postgres, sin tocar la base de desarrollo:

```bash
JWT_KEY=<32+ caracteres> POSTGRES_PASSWORD=<contraseña> docker compose -f docker-compose.produccion.yml up -d --build
docker compose -f docker-compose.produccion.yml exec app dotnet SistemaTaller.Api.dll crear-admin admin@ejemplo.com "una-contraseña"
# http://localhost:8080
docker compose -f docker-compose.produccion.yml down        # agregar -v borra también los datos del ensayo
```

Los tests E2E se pueden apuntar a ese ensayo: `E2E_BASE_URL=http://localhost:8080 E2E_EMAIL=… E2E_PASSWORD=… npm run test:e2e` (desde `frontend/`).

Sirve también como instalación real en una PC o servidor propio del taller, pero ahí el HTTPS y los backups quedan a cargo de quien lo instala.

## Operación del piloto (Fase 6.5)

- **Contraseña olvidada**: no hay recuperación por email. El responsable del sistema la restablece desde la consola de la plataforma:
  `dotnet SistemaTaller.Api.dll cambiar-password <email> "<nueva de 8+ caracteres>"`
  (en local: `dotnet run -- cambiar-password <email> "<nueva>"` desde `backend/SistemaTaller.Api/`). Con la sesión de 30 días, las sesiones ya abiertas no se cierran al cambiar la contraseña; para cerrarlas todas hay que cambiar `Jwt__Key`.
- **Salud**: `GET /salud` responde `200` si la API y la base están bien, `503` si la base no responde. No pide token y no devuelve datos. Apuntarle un monitor externo gratuito (UptimeRobot o similar, intervalo de 5 minutos, aviso al celular del responsable) con la URL `https://<dominio>/salud`.
- **Límite de intentos**: `POST /auth/login` acepta 10 intentos por minuto por IP y responde `429` después. Cambiables con `Login__IntentosPorVentana` y `Login__VentanaSegundos` (por defecto 10 y 60). Depende de que la plataforma mande `X-Forwarded-For`; si todos los pedidos llegaran con la IP del proxy, el límite sería compartido por todos los usuarios.
- **Logs**: todo sale por la salida estándar. Cada login fallido registra el email y la IP (nunca la contraseña) como `Login fallido para …`, y cada `429` como `Login limitado (429) …`. Los errores no manejados los registra `GlobalExceptionHandler`. Saber dónde se leen y cuántos días se guardan en la plataforma elegida.

## Backups

- **En una plataforma**: usar el backup automático diario de su Postgres administrado y probar una restauración antes de cargar datos reales.
- **A mano** (o en el ensayo de arriba), con las herramientas de Postgres:

```bash
pg_dump -U postgres -d sistema_taller --format=custom > respaldo.dump     # guardar
pg_restore -U postgres -d <base_vacia> < respaldo.dump                     # restaurar
```

## Qué falta decidir (dueño)

- **Plataforma de hosting** y su costo mensual; crear las cuentas.
- **Secretos de producción**: `Jwt__Key` y la contraseña de la base (no se versionan, no se reusan los de desarrollo).
- **Dominio** propio, si se quiere uno distinto al que da la plataforma.
- **Usuario administrador real** (email y contraseña del taller).
- Probarlo en el celular, en el taller, con el dueño del taller mirando, y revisar con él los precios de referencia y lo que quedó fuera del MVP ([`mvp.md`](../producto/mvp.md)).
