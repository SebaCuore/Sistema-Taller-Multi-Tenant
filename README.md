# Sistema Taller

Sistema de gestión web para talleres mecánicos (autos y motos): vehículos, órdenes de trabajo, catálogo de servicios, stock y ventas.

- **Documentación**: [`docs/README.md`](docs/README.md) — índice general de todo lo que hay.
- **Estado y avance**: [`docs/bitacora/`](docs/bitacora/README.md) — qué se hizo y verificó en cada sesión, y qué está esperando decisión.
- **Plan por fases**: [`docs/proceso/plan_desarrollo.md`](docs/proceso/plan_desarrollo.md).
- **Guía para agentes y convenciones del código**: [`CLAUDE.md`](CLAUDE.md).

## Arranque rápido (desarrollo)

```bash
docker compose up -d                                   # Postgres
cd backend/SistemaTaller.Api
dotnet ef database update                              # tablas y datos iniciales
dotnet run -- crear-admin admin@ejemplo.com "una-contraseña"   # solo la primera vez
dotnet run                                             # API en http://localhost:5108
cd ../../frontend && npm install && npm run dev        # web en http://localhost:5173
```

Para ensayar la versión de producción (una sola imagen Docker) o desplegar: [`docs/proceso/despliegue.md`](docs/proceso/despliegue.md).
