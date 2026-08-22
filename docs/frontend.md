# Frontend

## Stack

| Pieza | Elección | Por qué |
|---|---|---|
| Librería de UI | React + TypeScript | Definido desde el inicio del proyecto. |
| Build tool | Vite | Estándar actual para React+TS, arranque y recarga en desarrollo muy rápidos. Se descartó Next.js: no hace falta renderizado en servidor ni SEO para una app interna detrás de login. |
| Estilos | Tailwind CSS | Permite centralizar la paleta de colores y el tamaño de fuentes en un solo archivo de configuración — clave para el pedido de "paleta sencilla y fácil de cambiar". |
| Componentes | shadcn/ui (sobre Radix + Tailwind) | Componentes accesibles de fábrica (foco de teclado, contraste, tamaños de click correctos). El código se copia al proyecto en vez de instalarse como dependencia cerrada, así que se puede simplificar libremente. |
| Ruteo | React Router | Navegación entre pantallas de una SPA. |
| Datos de API | TanStack Query | Cachea, reintenta y maneja estados de carga/error. Permite empezar con datos simulados (mocks) y reemplazarlos por la API real sin tocar la lógica de las pantallas. |
| Íconos | lucide-react | Íconos simples y claros. Se usan siempre acompañados de texto, nunca solos. |
| Testing de componentes | Vitest + React Testing Library | Pareja natural de Vite. Detalle de qué se testea y por qué en [`testing.md`](./testing.md). |
| Testing end-to-end | Playwright | Un test de flujo completo por hito importante, no por pantalla — ver `testing.md`. |

## Enfoque general

- **Mobile-first.** Se diseña primero la versión de celular y se adapta hacia arriba para pantallas de escritorio — coherente con que la mayoría del uso va a ser desde el celular.
- **Totalmente online.** Sin soporte offline en el MVP (decisión explícita — ver `mvp.md`). Toda pantalla asume que hay conexión disponible.
- **El patrón `api/` alterna entre mocks y la API real.** Servido a través de TanStack Query, permite construir y probar una pantalla incluso si el endpoint real de esa función todavía no existe. En la práctica, backend y frontend se construyen función por función, no todo un lado primero — ver `plan_desarrollo.md`.

## Guías de accesibilidad y usabilidad

Público objetivo: dueños/empleados de taller, muchos de edad avanzada, con visión reducida, usando mayormente el celular con las manos no siempre limpias/libres. Estas guías son no negociables para cualquier pantalla nueva:

- **Botones grandes**: altura mínima táctil de 48–56px. Nada de botones o links de tamaño "por defecto" del navegador.
- **Tipografía grande**: tamaño base mayor al estándar web (18px en mobile en vez de 16px), nunca por debajo de 14px en ningún texto.
- **Alto contraste siempre**: evitar grises claros sobre blanco u otras combinaciones de bajo contraste.
- **Paleta reducida y centralizada**: un color de acción principal, uno de éxito, uno de error/alerta, y neutros para fondo/texto — todo como tokens de Tailwind en un único archivo, para poder cambiar el look completo del sistema editando un solo lugar.
- **Listas/tarjetas antes que tablas densas** en mobile — una tabla con muchas columnas es difícil de leer para alguien con poca vista en una pantalla chica.
- **Nunca un ícono solo**: siempre acompañado de una palabra que aclare qué hace.
- **Sin gestos como única vía de interacción** (swipe, long-press): siempre debe existir un botón visible equivalente.
- **Formularios simples**: un paso claro a la vez, evitar pantallas con demasiados campos juntos.
- **Confirmación antes de acciones destructivas** (borrar un vehículo, un producto, quitar un servicio de una orden).

## Estructura de carpetas propuesta (alto nivel)

```
src/
  components/   → componentes propios + los generados por shadcn/ui
  pages/        → una por pantalla
  api/          → llamadas a la API (o mocks, intercambiables sin tocar pages/)
  hooks/
  lib/          → utilidades, configuración de TanStack Query, etc.
  styles/       → configuración de Tailwind y tokens de color
```

## Pantallas

El detalle completo de pantallas, navegación (mobile y escritorio) y el patrón responsive a usar en cada tipo de pantalla está en [`screens.md`](./screens.md).

## Próximo paso

El plan de implementación fase por fase (qué pantalla se construye en qué momento, y contra qué parte del backend) está en [`plan_desarrollo.md`](./plan_desarrollo.md), empezando por el esqueleto del proyecto en su Fase 0.
