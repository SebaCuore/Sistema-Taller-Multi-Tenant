# Arquitectura

## Visión general

Aplicación web cliente-servidor clásica:

```
[ Navegador (React + TypeScript) ]
              |
        HTTPS / API
              |
[ Backend: ASP.NET Core (.NET 10) ]
              |
[ Base de datos: PostgreSQL ]
```

## Frontend

- **React + TypeScript + Vite + Tailwind CSS + shadcn/ui.** Detalle completo del stack y de las guías de accesibilidad/usabilidad en [`frontend.md`](./frontend.md).
- Consume una API HTTP del backend (REST, ver [`backend.md`](./backend.md)).
- No debe asumir que existe un solo taller: toda vista que liste datos (vehículos, órdenes, servicios, stock) opera siempre dentro del contexto de un `taller_id`, aunque hoy solo exista uno.
- Totalmente online, sin soporte offline en el MVP (decisión explícita — ver `mvp.md`).
- Se construye con datos simulados (mocks vía TanStack Query) antes de que exista el backend, para validar pantallas y detectar requerimientos faltantes.

## Backend y base de datos

**Decidido: ASP.NET Core sobre .NET 10, con Entity Framework Core 10 y PostgreSQL.** El detalle de estructura de proyecto, convenciones de EF Core y cómo se implementa en código el aislamiento por `taller_id` está en [`backend.md`](./backend.md) — este documento se queda con el porqué de la elección.

Motivo de cada pieza, en base a los criterios que ya se habían dejado anotados acá:

- **El dominio es fuertemente relacional** (vehículos, servicios, órdenes, productos, con integridad entre ellos: el total de una orden depende de sus líneas de servicio, una venta depende de que el producto exista). Un motor relacional resuelve esto con foreign keys y transacciones; un motor de documentos obligaría a reimplementar esas garantías a mano.
- **El filtro por `taller_id` tiene que ser difícil de olvidar** (ver "Aislamiento multi-tenancy" abajo) — Entity Framework Core permite imponerlo una sola vez, a nivel de `DbContext`, con *global query filters*, en vez de confiar en que cada endpoint lo recuerde.
- **PostgreSQL** es gratuito, de código abierto y soportado de fábrica por prácticamente cualquier plataforma de hosting tipo PaaS — no ata el proyecto a un proveedor específico ni suma costo de licencia.
- **Costo de hosting**: se confirmó que el costo de alojar el backend no depende del lenguaje/runtime elegido (Render y Railway, por ejemplo, cobran por RAM/CPU asignada, no por si el proceso es .NET, Node o cualquier otro) — así que la elección de stack se pudo hacer en base a otros criterios (el dominio relacional, y aprovechar la base ya construida en C#) sin resignar nada en costo.

## Aislamiento multi-taller (multi-tenancy)

**Estrategia elegida: esquema compartido con columna `taller_id`.**

- Todas las tablas cuyo contenido pertenece a un taller (`Vehiculo`, `Servicio`, `OrdenTrabajo`, `Producto`, `Venta`, `Usuario`, etc.) tienen una columna `taller_id` que referencia a `Taller`.
- Hoy existe un único registro en `Taller`, pero el modelo ya soporta N talleres sin cambios de esquema.
- **Regla de seguridad no negociable:** ninguna consulta a estas tablas puede ejecutarse sin filtrar por el `taller_id` del usuario autenticado. Esto se resuelve a nivel de capa de acceso a datos (no confiar en que cada endpoint lo recuerde por separado), para evitar que un bug filtre datos de un taller a otro. La implementación concreta (global query filters de EF Core aplicados automáticamente a toda entidad marcada) está en [`backend.md`](./backend.md).
- Alternativas descartadas por ahora: esquema o base de datos separada por taller (más aislamiento, pero mucho más costo operativo para el volumen actual — se puede reconsiderar si el producto crece fuerte como SaaS).

## Seguridad

(Se documenta acá, como sección, en vez de un archivo aparte — el proyecto arranca chico; si crece, se puede separar a `security.md`.)

- **Autenticación**: usuario + contraseña, con JWT emitido por el backend tras validar credenciales (detalle en `backend.md`). Contraseñas siempre hasheadas (BCrypt), nunca en texto plano ni en logs.
- **HTTPS obligatorio** en el entorno de producción — sin excepciones, dado que es un requisito explícito del proyecto ("seguro para lanzar a la web").
- **Secretos y credenciales** (contraseña de base de datos, clave de firma del JWT, etc.) siempre por variables de entorno (o `dotnet user-secrets` en desarrollo), nunca hardcodeados ni versionados en el repositorio.
- **Aislamiento entre talleres**: ver sección anterior — es, en la práctica, el requisito de seguridad más importante del sistema una vez que haya más de un taller.
- **Roles y permisos**: para el MVP no hay un sistema de roles granular. Cada `Usuario` tiene un campo `rol` simple (hoy solo se usa `admin`), pensado para poder agregar valores nuevos (`empleado`, etc.) más adelante sin rediseñar la tabla ni el token JWT. No se modela una tabla de permisos separada todavía — se agrega si/cuando haga falta diferenciar accesos de verdad.
- **Backups**: se recomienda backup automático y periódico de la base de datos desde que haya datos reales de un taller en producción (a definir la herramienta concreta junto con el hosting).

## Despliegue

- **Plataforma concreta todavía pendiente**, a elegir entre opciones tipo PaaS con TLS/HTTPS de fábrica y bajo esfuerzo de mantenimiento — candidatas ya relevadas: Render, Railway, Fly.io o Azure App Service (esta última con integración algo más directa si en algún momento se quisiera usar SQL Server en vez de PostgreSQL). La elección de ASP.NET Core + PostgreSQL no ata a ninguna de estas en particular — las cuatro las soportan igual.
- Se prioriza no exponer el proyecto a errores de configuración manual de servidor en esta primera etapa (nada de administrar un VPS a mano todavía).
