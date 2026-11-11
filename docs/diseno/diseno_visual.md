# Dirección visual

Define la identidad visual del sistema: paleta, tipografía y los dos elementos propios que lo hacen reconocible. Complementa a [`frontend.md`](../tecnica/frontend.md) (stack y guías de accesibilidad) y a [`pantallas.md`](pantallas.md) (qué pantallas existen).

> Prototipo navegable: [`prototipo.html`](prototipos/prototipo.html) — un archivo, sin dependencias. Abrilo en el celular para validar tamaños táctiles reales. La versión anterior queda en [`prototipo-v1.html`](prototipos/prototipo-v1.html) solo como referencia de comparación, no como fuente de verdad.

## De dónde sale

Del mundo del taller, no de una paleta genérica: chapa galvanizada, negro de goma, y **la patente** — el objeto que todo el mundo en el rubro lee de lejos y, además, la clave de búsqueda del sistema (RF-06). El público (dueños/empleados, muchos de edad avanzada, celular en mano sucia, parado al lado del elevador) obliga a que todo sea grande y de alto contraste: eso no es una restricción del diseño, es el diseño.

## Tokens de color

Van a un único archivo de configuración de Tailwind (ver [`frontend.md`](../tecnica/frontend.md)), de manera que cambiar el look completo sea editar un solo lugar.

| Token | Claro | Oscuro | Uso |
|---|---|---|---|
| `acero` | `#E3E7EA` | `#0E1114` | Fondo de página |
| `acero-2` | `#BDC6CF` | `#2C333A` | Bordes y reglas |
| `tiza` | `#FBFCFD` | `#191E23` | Superficie de tarjetas |
| `caucho` | `#14171A` | `#EEF2F5` | Tinta principal |
| `grasa` | `#59636D` | `#9BA7B1` | Tinta secundaria |
| `banda` | `#0A3FA8` | `#7BA8FF` | Acción principal |
| `listo` | `#146B36` | `#54C982` | Listo para retirar / éxito |
| `proceso` | `#8A4B00` | `#E0A34A` | En proceso |
| `senal` | `#B3300F` | `#FF8365` | Alerta y acciones destructivas |
| `chip-ink` | `#FFFFFF` | `#14171A` | Texto sobre chips de color sólido (estado) |

El azul de acción sale de la banda de la patente Mercosur, no de un azul de sistema. **La patente se dibuja siempre blanca con banda azul, en los dos temas** — una patente es una patente; el efecto lateral es que en modo oscuro es el objeto más brillante de la pantalla, que es exactamente lo que se quiere que el ojo encuentre primero.

`acero` es una superficie neutra, no un color de marca — se eligió deliberadamente poco saturada para que las tarjetas blancas y los acentos (patente, chips de estado) sean lo único que salte a la vista. Todas las combinaciones de texto sobre estos tokens están validadas contra WCAG AA (mínimo 4.5:1 en texto normal); el texto secundario (`grasa`) sobre `acero` da 4.9:1, y el resto de las combinaciones de texto del sistema dan entre 6:1 y 17:1.

## Tipografía

| Rol | Familia | Por qué |
|---|---|---|
| Display y datos | **Archivo** (variable, eje de ancho 62–125) | Grotesca industrial de Omnibus-Type (Buenos Aires). Una sola familia hace dos trabajos: expandida al 118% para títulos, condensada al 88% para patentes y números. |
| Cuerpo | **Alegreya Sans** | Humanística de Huerta Tipográfica (La Plata). Altura de x generosa; aguanta bien los 19px que necesita este público. |

No hay una tercera familia: el eje de ancho de Archivo cubre el rol utilitario. Escala base 19px en mobile, **igual de grande en escritorio** — más espacio no significa interfaz más densa.

## Jerarquía sin gritar

Alto contraste no significa que todo tenga que pesar igual. La negrita (700/800) queda reservada para lo que hay que encontrar primero: la patente, el precio, el título de pantalla, el botón principal. Todo lo demás — nombre del vehículo, etiquetas de campo, nombre de un servicio dentro de una orden, encabezados de tabla — usa un peso intermedio (600). Con un público que necesita contraste, la jerarquía se construye con peso y color, no bajando el contraste de lo secundario.

Misma lógica en chips y controles:

- **Estado de una orden** (`estado--proceso`, `estado--listo`, etc.): relleno sólido del color de estado con texto claro (token `chip-ink`), sin borde ni punto adicional. El código de color es el mismo de siempre; se sacaron capas redundantes.
- **Filtros y pestañas** (Todos/Ingresados/…, Servicios/Ventas): sin caja cuando están inactivos — solo texto. El filtro activo es el único que se dibuja como una pastilla rellena. Mismo tamaño de toque (56px), menos rectángulos con borde compitiendo en pantalla.
- **Elegir un servicio para una orden** y **activar/desactivar un servicio del catálogo** son acciones distintas y ahora se ven distintas: la primera es un checkbox (selección puntual), la segunda es el interruptor tipo switch (estado binario permanente). Comparten el mismo control visual solo cuando significan lo mismo.

## Los dos elementos propios

1. **La patente como identidad.** Todo vehículo aparece compuesto como una patente real (campo blanco, banda azul, Archivo condensada). El tratamiento de identidad es, literalmente, el modelo de datos hecho visible.
2. **El croquis de esquinas** — *el elemento firma*. Un esquema de cuatro ruedas (dos para motos) que enciende la esquina afectada cuando el servicio tiene posición y/o lado (RF-13, RF-17). Codifica `aplica_posicion` / `aplica_lado` de verdad en vez de decorar: un mecánico lee "delantero izquierdo" sin leer. **Siempre acompañado del texto**, nunca solo.

Todo lo demás se mantiene callado a propósito: tarjetas planas, sin gradientes, sin pilas de sombras, una regla de 1px. La audacia se gasta en un solo lugar.

## Decisiones descartadas

- **Stepper numerado de estados (01 → 02 → 03).** Mentiría: RF-20 dice que saltar directo a `entregado` es el caso normal de un trabajo corto. Reemplazado por un chip de estado más el tiempo transcurrido desde el ingreso, que es lo que importa en el piso del taller.
- **Íconos sin texto en la navegación.** Prohibido por [`frontend.md`](../tecnica/frontend.md); además el público no comparte el vocabulario de íconos de una app.
- **Tablas densas en mobile.** Tarjetas en mobile, tabla recién en escritorio (ver [`pantallas.md`](pantallas.md)).

## Piso de calidad

Responsive hasta mobile, foco de teclado visible, `prefers-reduced-motion` respetado, ambos temas definidos con tokens (nunca un color definido únicamente dentro de un `@media`). Altura táctil mínima 56px; ningún texto por debajo de 15px. Combinaciones de color revisadas contra WCAG AA (≥4.5:1 en texto, ≥3:1 en elementos gráficos no textuales).
