# Flujo de trabajo con Git y GitHub

Explica cómo usar Git y GitHub para que el desarrollo quede ordenado y sea fácil de retomar — pensado para un desarrollador solo, sin equipo, sin ceremonia de más. "Crear un repo y subir todo" funciona para guardar una copia, pero no da ninguna de estas tres cosas que sí importan acá: un historial que se pueda leer y volver atrás con sentido, una red de seguridad que avise si algo se rompió antes de que lo notes vos, y una forma de nunca subir una contraseña o clave por accidente. Con un rato de setup al principio, las tres vienen solas.

## 1. Inicializar el repositorio (hoy no existe — ver `CLAUDE.md`)

Antes de tocar Git hay dos archivos que todavía no existen en la carpeta del proyecto y conviene crear primero: el `.gitignore` y un `README.md` mínimo. Si no, terminás versionando `bin/`, `obj/` o `node_modules/` por error (cuesta sacarlos del historial después), y el repo en GitHub queda sin ninguna explicación de qué es.

**Paso 1 — crear el `.gitignore`.** En la raíz de "Sistema Taller", creá un archivo llamado `.gitignore` con este contenido (es el mismo que se explica en detalle en la sección 3, abajo):

```gitignore
# .NET
bin/
obj/
*.user
.vs/

# Node / frontend
node_modules/
dist/
.vite/

# Secretos y configuración local — nunca se versionan
*.env
.env.*
appsettings.Development.json
appsettings.Local.json

# Editor
.idea/
.vscode/*
!.vscode/extensions.json
```

**Paso 2 — crear un `README.md` mínimo.** No hace falta que sea largo todavía (la sección 7 explica qué agregarle más adelante); alcanza con el nombre del proyecto, una línea de qué es, y un link a `docs/description.md`:

```markdown
# Sistema Taller

Sistema de gestión web para talleres mecánicos (autos y motos). Ver `docs/description.md` para el detalle completo.
```

**Paso 3 — recién ahora, `git init` y el primer commit** (con `.gitignore` y `README.md` ya en la carpeta, quedan incluidos en este mismo commit):

```bash
cd "Sistema Taller"
git init
git add .
git commit -m "docs: initial project state (specs, prototype and plan)"
```

**Paso 4 — GitHub.** Creá un repositorio vacío: al crearlo, GitHub ofrece tres casilleros ("Add a README file", "Add .gitignore", "Choose a license") — dejalos **todos sin marcar**. No es que ya "tengas" esos archivos en un sentido abstracto: es que si los marcás, GitHub genera un primer commit propio en el repo remoto, distinto al que ya hiciste vos en el paso 3, y el primer `git push` te va a pedir un merge innecesario para dos historiales que deberían haber sido uno solo. Con los tres sin marcar, el repo remoto queda realmente vacío y el push conecta limpio:

```bash
git branch -M main
git remote add origin https://github.com/TU_USUARIO/sistema-taller.git
git push -u origin main
```

## 2. Ramas: simple, no hace falta más

Nada de Git Flow (ramas `develop`/`release`/`hotfix`) — es ceremonia pensada para equipos grandes con múltiples versiones en producción a la vez, y este proyecto no tiene ninguna de las dos cosas. La regla que sí vale la pena:

- **`main` siempre queda en un estado que funciona.** Nunca se programa directo sobre `main`.
- **Una rama por feature**, idealmente calzando con las fases de `plan_desarrollo.md` — así el nombre de la rama ya te dice en qué fase estás:

```bash
git checkout -b feature/fase-1-base-backend-login
# ... trabajo de esa fase ...
git checkout -b feature/fase-2-vehiculos
```

- Cuando la fase está lista (pasó su checkpoint en `plan_desarrollo.md`), esa rama se integra a `main` mediante un **Pull Request** (sección 5) — no un merge directo local.

## 3. `.gitignore`

Ya se creó en el paso 1 de la sección anterior; esta sección es la referencia de por qué tiene ese contenido. Este proyecto mezcla un proyecto .NET y uno de Node — el `.gitignore` necesita cubrir los dos:

```gitignore
# .NET
bin/
obj/
*.user
.vs/

# Node / frontend
node_modules/
dist/
.vite/

# Secretos y configuración local — nunca se versionan
*.env
.env.*
appsettings.Development.json
appsettings.Local.json

# Editor
.idea/
.vscode/*
!.vscode/extensions.json
```

Nota sobre secretos: como `backend.md` ya establece usar `dotnet user-secrets` para desarrollo, esas claves ni siquiera viven dentro de la carpeta del proyecto (quedan en el perfil del usuario, fuera del repo) — así que en la práctica no deberían aparecer nunca en un `git status`. El `.gitignore` de `appsettings.Development.json` es una segunda red de seguridad, no la única.

## 4. Commits

Cada commit, un cambio lógico completo — no "subí todo lo que tenía" al final del día, tampoco un commit por cada archivo tocado. Mensaje en modo imperativo, describiendo el qué y no el cómo.

**Los mensajes de commit van en inglés**, a diferencia del resto del proyecto (código, documentación, nombres de entidades y campos van en español — ver `CLAUDE.md`). Es una excepción deliberada, no un descuido: el prefijo + mensaje corto en inglés es la convención de facto en prácticamente cualquier repositorio, propio o ajeno, así que el historial de Git queda legible con el mismo estándar que el resto del ecosistema — útil si en algún momento este repo lo mira alguien más, o si lo mostrás como parte de un portfolio.

### Prefijos: qué significa cada uno y cuándo usarlo

Un prefijo liviano al principio del mensaje ayuda a que el historial se lea de un vistazo, sin abrir cada commit para saber de qué se trata. Alcanza con estos cinco — no hace falta memorizar más categorías que estas:

| Prefijo | Cuándo usarlo | Ejemplo |
|---|---|---|
| `feat` | Una funcionalidad nueva, visible para quien usa el sistema — un requerimiento funcional nuevo, un endpoint, una pantalla. | `feat: add vehicle registration with known-plate confirmation` |
| `fix` | Corrige algo que ya existía y funcionaba mal — un bug real, no una mejora. | `fix: taller filter did not reach OrdenTrabajoServicio` |
| `docs` | Cambios solo en documentación (`docs/`, `CLAUDE.md`, `README.md`) — sin tocar ningún archivo de código. | `docs: add development plan by phases` |
| `test` | Agregar o modificar tests, sin cambiar el comportamiento del código de producción. | `test: add taller_id isolation battery for vehicles` |
| `chore` | Todo lo que hace falta para que el proyecto funcione y no entra en ninguna de las categorías anteriores: dependencias, configuración, infraestructura, CI. Si un cambio no encaja claramente en `feat`/`fix`/`docs`/`test`, casi siempre es esto. | `chore: add local Postgres via Docker Compose` |

Un caso ambiguo típico: al implementar una función nueva junto con sus propios tests en el mismo commit (lo normal, no hace falta separarlos), el prefijo es `feat` — los tests van como parte de entregar la funcionalidad completa, no como un commit aparte. `test` queda para cuando se agregan o ajustan tests por sí solos, sin que cambie el código de producción al mismo tiempo — por ejemplo, la batería de aislamiento por `taller_id` que `testing.md` pide escribir como paso propio al principio de la Fase 1.

## 5. Pull Requests, incluso trabajando solo

Suena a ceremonia de equipo, pero da dos cosas útiles estando solo: un punto de pausa para releerte antes de que algo entre a `main`, y el lugar exacto donde el chequeo automático (sección 6) tiene que ponerse verde antes de mergear. Flujo:

```bash
git push -u origin feature/fase-2-vehiculos
```

En GitHub, abrís el Pull Request de `feature/fase-2-vehiculos` hacia `main`. Ahí ves el diff completo de la fase, esperás a que el workflow de la sección 6 termine en verde, y recién ahí lo mergeás (botón "Squash and merge" deja un solo commit prolijo en `main` por feature, aunque hayas hecho diez commits chicos en el camino — cómodo para no ensuciar el historial de `main` con work-in-progress).

## 6. GitHub Actions — qué es, explicado desde cero

Es un robot que GitHub hace correr automáticamente cada vez que empujás código o abrís un Pull Request. Se le dice qué hacer con un archivo YAML dentro de `.github/workflows/`, ese archivo vive en el propio repositorio (o sea, es "código" como cualquier otro). Lo típico — y lo que sirve acá — es que compile el proyecto y corra los tests; si algo falla, te enterás en la pestaña "Actions" del repo y en el propio Pull Request, antes de que ese cambio llegue a `main`. Es el reemplazo automático de acordarte de correr `dotnet test` y `npm test` a mano cada vez.

Creá el archivo `.github/workflows/ci.yml`:

```yaml
name: CI

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

jobs:
  backend:
    runs-on: ubuntu-latest
    defaults:
      run:
        working-directory: backend/SistemaTaller.Api
    steps:
      - uses: actions/checkout@v7
      - uses: actions/setup-dotnet@v5
        with:
          dotnet-version: '10.0.x'
      - run: dotnet restore
      - run: dotnet build --no-restore
      - run: dotnet test --no-build

  frontend:
    runs-on: ubuntu-latest
    defaults:
      run:
        working-directory: frontend
    steps:
      - uses: actions/checkout@v7
      - uses: actions/setup-node@v6
        with:
          node-version: 24
          cache: 'npm'
          cache-dependency-path: frontend/package-lock.json
      - run: npm ci
      - run: npm run build
      - run: npm test -- --run
```

Los dos jobs (`backend` y `frontend`) corren en paralelo, cada uno en su propia máquina virtual descartable. `dotnet test` en este punto va a incluir los tests de integración de `testing.md` — como corren contra Testcontainers (que necesita Docker), el runner de GitHub Actions ya lo trae instalado de fábrica, no hace falta configurar nada extra para eso.

Con esto commiteado y pusheado una vez a `main`, todo Pull Request nuevo ya lo dispara solo — no hay que volver a tocarlo salvo que cambie algo de la estructura de carpetas.

## 7. README del proyecto

El `README.md` creado en el paso 2 de la sección 1 es intencionalmente mínimo, solo para que el primer commit no quede vacío de contexto. Vale la pena volver a él una vez que el esqueleto de Fase 0 (`plan_desarrollo.md`) esté funcionando y agregarle: con qué se levanta cada parte (dos o tres comandos, ver `backend.md` y `plan_desarrollo.md` → Fase 0) y un link a `docs/` para el resto. Es lo primero que se ve al entrar al repositorio en GitHub, y el lugar donde un futuro vos (o alguien más) entiende en 30 segundos por dónde arrancar.

## Resumen del hábito diario

1. `git checkout main && git pull` antes de arrancar cualquier cosa.
2. Rama nueva por feature/fase: `git checkout -b feature/...`.
3. Commits chicos y descriptivos a medida que avanza esa fase.
4. Al llegar al checkpoint de la fase (`plan_desarrollo.md`): push de la rama, abrir Pull Request.
5. Esperar el check verde de GitHub Actions, mergear a `main`.
6. Borrar la rama ya mergeada, volver al paso 1 para la fase siguiente.
