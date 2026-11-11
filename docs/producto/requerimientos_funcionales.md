# Requerimientos funcionales

Alcance: MVP (primer lanzamiento). Ver [`mvp.md`](mvp.md) para el corte explícito entre lo que entra ahora y lo que queda para después.

## 1. Autenticación y usuarios

- **RF-01**: El sistema debe permitir iniciar sesión con usuario/email y contraseña.
- **RF-02**: Las contraseñas deben almacenarse hasheadas, nunca en texto plano.
- **RF-03**: Sin sistema de roles granular en el MVP — todo usuario autenticado tiene el mismo nivel de acceso (rol `admin` por defecto).
- **RF-04**: Toda acción dentro del sistema debe operar únicamente sobre los datos del `taller_id` del usuario autenticado.

## 2. Vehículos

- **RF-05**: Debe poder darse de alta un vehículo con: patente, tipo (auto/moto), marca (opcional), modelo (opcional), año (opcional), nombre y teléfono de contacto del dueño.
- **RF-06**: La patente es el campo principal de búsqueda de un vehículo.
- **RF-07**: Si se ingresa una patente ya registrada, el sistema debe sugerir autocompletar los datos de contacto del último registro conocido para esa patente, pidiendo confirmación explícita antes de guardar (el vehículo pudo haber cambiado de dueño).
- **RF-08**: Debe poder editarse la información de un vehículo ya cargado.

## 3. Catálogo de servicios

- **RF-09**: Cada taller debe contar con un catálogo propio de servicios, sembrado inicialmente con una lista por defecto (ver [`base_de_datos.md`](../tecnica/base_de_datos.md)).
- **RF-10**: Debe poder activarse/desactivarse un servicio del catálogo sin eliminarlo.
- **RF-11**: Debe poder agregarse un servicio propio, no incluido en la lista por defecto.
- **RF-12**: Cada servicio puede tener un precio de referencia, opcional.
- **RF-13**: Cada servicio indica si admite posición (delantero/trasero) y/o lado (izquierda/derecha) — el lado solo aplica en la práctica a servicios de auto.

## 4. Órdenes de trabajo (sección "Taller")

- **RF-14**: Debe existir una vista ("Taller") que liste los vehículos actualmente en curso, agrupados o filtrables por estado (ingresado, en proceso, listo para retirar, entregado).
- **RF-15**: Debe poder crearse una orden de trabajo asociada a un vehículo.
- **RF-16**: Una orden de trabajo puede incluir uno o varios servicios del catálogo.
- **RF-17**: Al agregar un servicio a una orden, si el servicio admite posición y/o lado, deben poder especificarse (por ejemplo, "Balanceo — delantero izquierdo").
- **RF-18**: El precio de cada servicio dentro de una orden se precarga con el precio de referencia (si existe) pero es editable manualmente por el empleado, tanto al agregarlo como en cualquier momento posterior mientras la orden esté en curso (decisión del dueño, 2026-11-11).
- **RF-19**: Debe poder agregarse o quitarse un servicio de una orden mientras está en curso.
- **RF-20**: Registrar los estados intermedios (en proceso, listo para retirar) es opcional — el empleado puede cargar la orden completa recién al finalizar el trabajo.
- **RF-21**: Al finalizar una orden de trabajo (estado `entregado`), debe quedar registrada en el historial. Antes de finalizar, el sistema pide confirmación explícita y avisa que la orden pasa al historial y ya no se va a poder modificar (decisión del dueño, 2026-11-11).

## 5. Historial de trabajos

- **RF-22**: Debe poder buscarse el historial completo de órdenes de trabajo de un vehículo por patente.
- **RF-23**: El historial debe mostrar, por cada orden pasada: fecha, servicios realizados (con posición/lado si aplica) y precio de cada uno.

## 6. Stock de productos

> Alcance funcional de alto nivel — el detalle de campos de `Producto` queda pendiente (ver [`mvp.md`](mvp.md)).

- **RF-24**: Debe poder agregarse, editarse, eliminarse y consultarse un producto en el stock.
- **RF-25**: Cada producto debe tener al menos nombre, precio y cantidad en stock.

## 7. Ventas de productos

> Alcance funcional de alto nivel — el detalle fino queda pendiente (ver [`mvp.md`](mvp.md)).

- **RF-26**: Debe poder registrarse la venta de un producto seleccionándolo desde el stock, sin necesidad de asociarla a un cliente ni a un vehículo.
- **RF-27**: Al registrar una venta, debe descontarse automáticamente la cantidad vendida del stock del producto.
- **RF-28**: Debe permitirse registrar una venta aunque el stock del producto esté en 0 (el empleado puede no haber tenido tiempo de reponerlo).
- **RF-29**: Cada venta debe registrar producto, precio y fecha.

## Fuera de alcance para el MVP

Ver el detalle completo en [`mvp.md`](mvp.md). En resumen: facturación electrónica (AFIP), notificaciones automáticas al cliente, portal de cliente, sistema de roles granular, onboarding de nuevos talleres como producto SaaS, detalle avanzado de Stock/Producto.
