#!/usr/bin/env python3
"""Verifica que la documentación esté navegable (ver CLAUDE.md -> "Documentación").

Comprueba:
  1. Que ningún link relativo de los .md (docs/, README.md, CLAUDE.md) esté roto.
  2. Que cada carpeta de docs/ con documentos .md tenga su README.md.
  3. Que el README.md de cada carpeta enlace a cada archivo de esa carpeta y a cada subcarpeta.
     Las carpetas que solo tienen archivos que no son .md (p. ej. prototipos/) se indexan
     desde el README.md de la carpeta de arriba. docs/bitacora/capturas/ se ignora.
  4. Que docs/README.md enlace a cada documento fuera de docs/bitacora/.
  5. Que no haya dos documentos con el mismo nombre dentro de docs/.

Uso: python scripts/verificar_docs.py   (sale con código 1 si encuentra problemas)
"""
import re
import sys
from pathlib import Path

RAIZ = Path(__file__).resolve().parent.parent
DOCS = RAIZ / "docs"
IGNORAR = {DOCS / "bitacora" / "capturas"}
LINK = re.compile(r"\[[^\]]*\]\(([^)\s]+)\)")

problemas: list[str] = []


def rel(p: Path) -> str:
    return p.relative_to(RAIZ).as_posix()


def ignorado(p: Path) -> bool:
    return any(p == i or i in p.parents for i in IGNORAR)


def links_de(md: Path) -> set[Path]:
    """Destinos (resueltos) de los links relativos de un .md, sin contar bloques de código."""
    destinos, en_codigo = set(), False
    for n, linea in enumerate(md.read_text(encoding="utf-8").splitlines(), 1):
        if linea.lstrip().startswith("```"):
            en_codigo = not en_codigo
            continue
        if en_codigo:
            continue
        # tramos de `código en línea`: un link que empieza adentro de uno es un ejemplo, no un link
        codigo = [m.span() for m in re.finditer(r"`[^`]*`", linea)]
        for m in LINK.finditer(linea):
            if any(a <= m.start() < b for a, b in codigo):
                continue
            objetivo = m.group(1)
            if re.match(r"^[a-z]+:", objetivo) or objetivo.startswith("#"):
                continue  # http:, mailto:, anclas dentro del mismo archivo
            destino = (md.parent / objetivo.split("#")[0]).resolve()
            if not destino.exists():
                problemas.append(f"{rel(md)}:{n}: link roto -> {objetivo}")
            destinos.add(destino)
    return destinos


archivos_md = [RAIZ / "README.md", RAIZ / "CLAUDE.md"] + sorted(
    p for p in DOCS.rglob("*.md") if not ignorado(p)
)
links = {md: links_de(md) for md in archivos_md if md.exists()}

for carpeta in sorted([DOCS] + [d for d in DOCS.rglob("*") if d.is_dir()]):
    if ignorado(carpeta):
        continue
    archivos = [f for f in carpeta.iterdir() if f.is_file() and f.name != "README.md"]
    subcarpetas = [d for d in carpeta.iterdir() if d.is_dir() and not ignorado(d)]
    tiene_md = any(f.suffix == ".md" for f in archivos) or carpeta == DOCS
    indice = carpeta / "README.md"
    if not tiene_md:
        continue  # carpeta de recursos: la indexa la carpeta de arriba (se chequea ahí)
    if not indice.exists():
        problemas.append(f"{rel(carpeta)}/: falta README.md (índice de la carpeta)")
        continue
    enlazados = links.get(indice, set())
    for f in archivos:
        if f.resolve() not in enlazados:
            problemas.append(f"{rel(indice)}: no enlaza a {f.name}")
    for d in subcarpetas:
        hijos_md = any(x.suffix == ".md" for x in d.iterdir() if x.is_file())
        if hijos_md:
            if (d / "README.md").resolve() not in enlazados and d.resolve() not in enlazados:
                problemas.append(f"{rel(indice)}: no enlaza a la subcarpeta {d.name}/")
        else:
            for f in d.iterdir():
                if f.is_file() and f.resolve() not in enlazados:
                    problemas.append(f"{rel(indice)}: no enlaza a {d.name}/{f.name}")

general = links.get(DOCS / "README.md", set())
for p in sorted(DOCS.rglob("*")):
    if p.is_file() and p.name != "README.md" and not ignorado(p) \
            and DOCS / "bitacora" not in p.parents and p.resolve() not in general:
        problemas.append(f"docs/README.md: no enlaza a {rel(p)}")

nombres: dict[str, Path] = {}
for p in DOCS.rglob("*.md"):
    if p.name == "README.md" or ignorado(p):
        continue
    if p.name in nombres:
        problemas.append(f"nombre repetido: {rel(p)} y {rel(nombres[p.name])}")
    nombres[p.name] = p

if problemas:
    print("Problemas en la documentación:")
    for x in problemas:
        print("  -", x)
    sys.exit(1)
print(f"Documentación OK: {len(links)} archivos revisados, sin links rotos ni documentos fuera del índice.")
