# 2026-11-11 — Retomar el proyecto y preparar el trabajo autónomo

**Fase:** 0 cerrada · 1 en curso (solo entidades).

**Hecho**
- Git: la rama local `feature/fase-1-base-backend-login` (entidades y enums, nunca subida a GitHub) se integró en `main` junto con el commit de docs que estaba solo en GitHub. La rama se borró: de ahora en más se trabaja sobre `main`.
- CI: fallaba en todos los pushes desde agosto porque `npm run build` rompía (TypeScript 6 convirtió `baseUrl` en error). Se sacó `baseUrl` de `tsconfig.json` y `tsconfig.app.json` (`paths` funciona igual sin él). Se agregó `npm run lint` a la CI y se desactivó `react-refresh/only-export-components` solo en `src/components/ui/` (shadcn exporta las variantes junto al componente). Los tests de backend y frontend se saltean mientras no existan sus proyectos.
- `.gitattributes` para normalizar fin de línea a LF en el repo.
- Documentación reorganizada en `docs/` por tema (`producto/`, `tecnica/`, `diseno/`, `proceso/`, `bitacora/`), cada carpeta con su índice `README.md`, nombres en español y referencias como links relativos. Los prototipos pasaron de `design/` a `docs/diseno/prototipos/`. Nuevo `scripts/verificar_docs.py` (links rotos e índices) y job `docs` en la CI.
- Docs: [`CLAUDE.md`](../../CLAUDE.md) con el protocolo de trabajo autónomo (push directo a `main`, seguimiento de la CI, definición de "terminado", cuándo parar); Fase 0 tildada en el plan y casillas nuevas en la Fase 1 (Vitest, `TESTS_CONNECTION_STRING`); [`flujo_de_trabajo.md`](../proceso/flujo_de_trabajo.md), [`testing.md`](../proceso/testing.md) y `README.md` al día; se crea la bitácora.

**Verificado**
- Frontend en limpio: `npm ci`, `npm run build`, `npm run lint` y `npm run test --if-present -- --run` → todo OK (Node 22; la CI usa Node 24).
- `ci.yml` parsea como YAML válido.
- `python3 scripts/verificar_docs.py` → sin problemas.
- Historial de la CI en GitHub: las 3 corridas anteriores fallaron en `npm run build` del frontend; el backend pasaba.

**No verificado**
- Backend: en esta sesión no había .NET SDK disponible, así que no se compiló localmente. Se revisaron las clases a mano (el `using` que faltaba ya estaba arreglado); lo confirma la CI en el próximo push.

**Esperando decisión**
- Charla con el dueño del taller sobre `Producto`/`Venta` (bloquea la Fase 6).
