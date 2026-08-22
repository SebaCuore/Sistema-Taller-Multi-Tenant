# Pantallas

Define la estructura de pantallas de la primera versión de frontend, mobile-first pero funcional en escritorio, siguiendo las guías de `frontend.md`.

## Navegación

**El mapa de pantallas es el mismo en mobile y en escritorio — lo único que cambia es el contenedor de navegación**, nunca qué información existe ni cómo se organiza.

### Mobile

Barra inferior fija, 4 accesos grandes (ícono + texto, nunca ícono solo):

```
[ Taller ]   [ Ventas ]   [ Stock ]   [ Historial ]
```

- Dentro de **Historial**: dos botones grandes arriba de la pantalla — **Servicios** / **Ventas**.
- Ícono de ajustes (⚙) en la esquina superior de cualquier pantalla → **Catálogo de Servicios** y **Cerrar sesión**. Se saca de la barra principal porque es de uso poco frecuente.

### Escritorio

Barra lateral izquierda fija, siempre visible (no hace falta ocultarla, sobra espacio horizontal), con los mismos accesos: Taller, Ventas, Stock, Historial (con Servicios/Ventas como pestañas dentro de la pantalla en vez de dos botones), y Ajustes al pie de la barra.

## Patrón general responsive

| Elemento | Mobile | Escritorio |
|---|---|---|
| Formularios cortos (nuevo vehículo, nueva venta, nuevo producto, nuevo servicio) | Pantalla completa — mejor para uso con una mano y botones grandes | Modal/diálogo centrado — hay espacio de sobra, no hace falta ocupar toda la pantalla |
| Listados (Historial, Stock) | Tarjetas apiladas, una columna | Tabla clásica — el mouse da más precisión para columnas angostas |
| Tarjetas de "Taller" | 1 columna | 2 a 4 columnas según ancho de pantalla |
| Tamaño de botones y texto | Grande | **Igual de grande** — el público (mecánicos, muchos de edad avanzada) necesita la misma claridad en la PC del mostrador que en el celular. Más espacio en pantalla no significa una interfaz más densa. |

## 0. Login

- **Propósito**: autenticar al usuario (RF-01).
- **Campos**: usuario/email, contraseña.
- **Casos especiales**: mensaje de error simple y claro si falla ("Usuario o contraseña incorrectos" — sin detalles técnicos).

## 1. Taller (pantalla de inicio)

- **Propósito**: ver de un vistazo los vehículos actualmente en curso (RF-14).
- **Elementos**: tarjetas, una por orden de trabajo activa, mostrando patente, tipo (auto/moto), estado actual y tiempo transcurrido desde el ingreso. Filtro/agrupación por estado (ingresado / en proceso / listo para retirar).
- **Acciones**: botón grande y fijo "+ Nuevo vehículo" (siempre visible, no hay que scrollear para encontrarlo). Tocar una tarjeta existente → pantalla 3 (Detalle de orden en curso).
- **Mobile**: tarjetas apiladas.
- **Escritorio**: grilla de tarjetas en varias columnas.
- **Casos especiales**: si no hay vehículos activos, mensaje simple ("No hay vehículos en el taller ahora") en vez de una pantalla vacía sin explicación.

## 2. Nuevo vehículo / Nueva orden

- **Propósito**: dar de alta un vehículo y su orden de trabajo (RF-05 a RF-07, RF-15).
- **Campos**: patente (obligatoria), tipo de vehículo (auto/moto), marca/modelo/año (opcionales), contacto (nombre/teléfono).
- **Caso especial clave — autocompletado (RF-07)**: al ingresar una patente ya registrada antes, se sugiere el contacto del último registro conocido, pero **pidiendo confirmación explícita** ("¿Sigue siendo Juan Pérez el dueño?") antes de guardar — nunca se autocompleta en silencio.
- Después de los datos del vehículo, selección de uno o varios servicios del catálogo activo del taller (con posición/lado si el servicio lo requiere, y precio editable — ver pantalla 4).
- **Mobile**: pantalla completa, un paso a la vez (datos del vehículo → luego servicios), para no saturar con muchos campos juntos.
- **Escritorio**: modal, puede mostrar ambos pasos en una sola vista más ancha si entra cómodo.

## 3. Detalle de orden en curso

- **Propósito**: gestionar una orden ya activa (RF-16 a RF-21) — es la pantalla que se abre al tocar una tarjeta existente en "Taller".
- **Elementos**: datos del vehículo, estado actual, lista de servicios agregados (cada uno con posición/lado si aplica, y precio — precargado con el precio de referencia pero editable a mano).
- **Acciones**: agregar servicio (mismo selector de catálogo que en el alta), quitar un servicio agregado, cambiar de estado (opcional — RF-20: se puede saltear directo a "Entregado"), botón grande "Finalizar" que cierra la orden y la manda al historial (RF-21).
- **Casos especiales**: confirmación antes de quitar un servicio ya agregado (acción destructiva). Si se finaliza sin haber pasado por estados intermedios, no debe dar error ni pedir explicación — es el caso normal para trabajos rápidos.

## 4. Historial de Servicios

- **Propósito**: consultar trabajos pasados (RF-22, RF-23).
- **Elementos**: listado cronológico de órdenes finalizadas, con buscador por patente arriba.
- **Mobile**: tarjetas.
- **Escritorio**: tabla (fecha, patente, servicios realizados, monto total), ordenable por columna.
- **Acción**: tocar una fila → **Detalle de servicio histórico** (solo lectura: fecha, servicios con posición/lado y precio de cada uno) con un botón para **editar los datos del vehículo** (marca/modelo/contacto) — resuelve la edición de vehículo sin necesitar una pantalla de "Vehículos" aparte.

## 5. Ventas

- **Propósito**: registrar una venta de mostrador (RF-26 a RF-29).
- **Elementos**: botón grande "+ Nueva venta" → selector de producto (buscador simple sobre el stock), cantidad, precio (precargado con el precio del producto, editable), confirmar.
- **Casos especiales**: se permite confirmar la venta aunque el stock del producto esté en 0 o quede negativo (RF-28) — sin bloquear ni mostrar error, como máximo una advertencia suave ("Este producto no tiene stock cargado").
- **Mobile**: pantalla completa para el alta.
- **Escritorio**: modal.

## 6. Historial de Ventas

- **Propósito**: consultar ventas pasadas.
- **Mobile**: tarjetas (producto, cantidad, precio, fecha).
- **Escritorio**: tabla, ordenable por fecha.

## 7. Stock / Productos

- **Propósito**: gestión del inventario (RF-24, RF-25).
- **Elementos**: listado de productos con nombre, precio y cantidad en stock. Acciones de agregar, editar y eliminar.
- **Mobile**: tarjetas con acciones grandes (editar/eliminar) visibles, no ocultas en un menú de tres puntos difícil de tocar.
- **Escritorio**: tabla editable.
- **Nota**: el detalle fino de campos de producto sigue pendiente de definición propia (ver `mvp.md`) — esta pantalla se ajusta cuando eso se resuelva.

## 8. Catálogo de Servicios (Ajustes)

- **Propósito**: configurar los servicios del taller (RF-09 a RF-13). Pantalla de uso poco frecuente, por eso vive en Ajustes y no en la navegación principal.
- **Elementos**: listado de servicios (por defecto + propios) con interruptor de activo/inactivo, y botón para agregar un servicio propio (nombre, categoría auto/moto, si aplica posición, si aplica lado, precio de referencia opcional).
- **Mobile y escritorio**: misma estructura, lista simple — no justifica un layout distinto por lo poco frecuente de su uso.

## Resumen de accesos

| Sección de navegación | Pantallas que incluye |
|---|---|
| Taller | 1 (inicio), 2 (nuevo vehículo/orden), 3 (detalle de orden en curso) |
| Ventas | 5 (nueva venta) |
| Stock | 7 |
| Historial → Servicios | 4 (listado + detalle histórico) |
| Historial → Ventas | 6 |
| Ajustes (⚙) | 8 (catálogo de servicios), cerrar sesión |
