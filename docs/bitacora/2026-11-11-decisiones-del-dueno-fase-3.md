# 2026-11-11 — Decisiones del dueño tras probar la Fase 3

**Fase:** 3 reabierta con dos ajustes, antes de la 4.

El dueño probó el sistema en su PC ("básico, pero funciona bien") y decidió:

- **Finalizar siempre pide confirmación**, avisando que la orden pasa al historial y ya no se va a poder modificar. Finalizar no borra datos, así que la interfaz no debe decir "se borra".
- **El precio de un servicio se puede cambiar en cualquier momento** mientras la orden esté en curso, no solo al agregarlo.
- **El E2E no se suma a la CI**: se prefiere mantener el proyecto lo más simple posible (pero escalable). Se sigue corriendo a mano al cerrar cada fase. Documentado en `testing.md` y en la definición de terminado de `CLAUDE.md`.

**Hecho:** documentado en `requerimientos_funcionales.md` (RF-18 y RF-21), `pantallas.md`, `CLAUDE.md` ("Reglas de dominio") y como casillas nuevas al final de la Fase 3 de `plan_desarrollo.md`, con el checkpoint actualizado.

**Verificado:** `python scripts/verificar_docs.py`. No hubo cambios de código en esta sesión.

**Pendiente:** implementar los dos ajustes (próxima sesión del agente).
