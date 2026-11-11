# Testing

Define qué rol tiene el testing en este proyecto y cómo se hace, en proporción al tamaño real del proyecto — nada de perseguir un porcentaje de cobertura, pero el aislamiento por `taller_id` (ver [`backend.md`](../tecnica/backend.md)) no es negociable: es la única parte del sistema donde un bug no se nota en el momento, se nota cuando un taller ve datos de otro.

## Rol del testing acá

Este proyecto tiene poca lógica de negocio compleja — la mayoría del backend es CRUD sobre Entity Framework Core. Eso cambia dónde conviene invertir el esfuerzo de testing respecto de un proyecto típico:

- **Poco valor** en unit tests aislados de cada entidad o cada mapeo — es código mecánico, de bajo riesgo, y ya lo cubre el compilador de C# más las migraciones de EF Core.
- **Mucho valor** en tests de integración que ejercitan el `DbContext` real contra una base Postgres real — porque ahí es donde vive el filtro por `taller_id`, las conversiones de enum a texto, y el manejo de fechas en UTC (todo lo documentado en [`backend.md`](../tecnica/backend.md) como "detalles que rompen en tiempo de ejecución, no en compilación").
- **Máximo valor, y no negociable**: la batería de tests que prueba específicamente que un taller nunca puede ver ni tocar datos de otro. Se escribe primero, antes que cualquier feature — es el gate de salida de la Fase 1 de [`plan_desarrollo.md`](plan_desarrollo.md), no un extra al final.

GitHub Actions corre `dotnet test` y `npm test` en cada push a `main` (ver [`flujo_de_trabajo.md`](flujo_de_trabajo.md)), pero eso no reemplaza correrlos antes de pushear: como se trabaja directo sobre `main`, una fase no se cierra sin la verificación completa de [`CLAUDE.md`](../../CLAUDE.md) → "Definición de terminado".

## La pirámide aplicada a este proyecto

```
        E2E (Playwright)        → 1, recién en la Fase 3, el flujo completo
     Integración (WebApplicationFactory)  → la mayoría del esfuerzo backend
  Unit tests (xUnit puro, sin base de datos) → pocos, solo donde hay lógica real
```

Al revés de un proyecto con mucha lógica de dominio, acá la base de la pirámide no es "muchos unit tests" — es "tests de integración razonablemente baratos", porque la parte riesgosa del sistema (el filtro global, las queries) solo se prueba de verdad contra una base real.

## Tests de aislamiento por `taller_id` (prioridad máxima — Fase 1)

Herramientas: **xUnit** + `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory<Program>`) + **`Testcontainers.PostgreSql`** (levanta un Postgres real y descartable por corrida de tests — no usar SQLite en memoria acá: no fuerza `timestamptz` en UTC ni maneja igual los enums-como-texto, y esas son justo las dos cosas que [`backend.md`](../tecnica/backend.md) marca como más propensas a romper en tiempo de ejecución).

Estructura típica del fixture — dos talleres, cada uno con sus propios datos:

```csharp
public class AislamientoTallerTests : IClassFixture<SistemaTallerFactory>
{
    private readonly SistemaTallerFactory _factory;

    public AislamientoTallerTests(SistemaTallerFactory factory) => _factory = factory;

    [Fact]
    public async Task Listar_vehiculos_no_devuelve_datos_de_otro_taller()
    {
        var (tallerA, tallerB) = await _factory.SembrarDosTalleresConVehiculos();
        var cliente = _factory.CrearClienteAutenticadoComo(tallerA);

        var respuesta = await cliente.GetFromJsonAsync<List<VehiculoRespuesta>>("/api/vehiculos");

        Assert.All(respuesta!, v => Assert.DoesNotContain(v.Id, tallerB.VehiculoIds));
    }

    [Fact]
    public async Task Pedir_por_id_un_vehiculo_de_otro_taller_devuelve_404()
    {
        var (tallerA, tallerB) = await _factory.SembrarDosTalleresConVehiculos();
        var cliente = _factory.CrearClienteAutenticadoComo(tallerA);

        var respuesta = await cliente.GetAsync($"/api/vehiculos/{tallerB.VehiculoIds[0]}");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode); // no 403: no confirmar que el recurso existe
    }

    [Fact]
    public async Task Alta_de_vehiculo_ignora_un_taller_id_mandado_a_mano_en_el_body()
    {
        var (tallerA, tallerB) = await _factory.SembrarDosTalleresConVehiculos();
        var cliente = _factory.CrearClienteAutenticadoComo(tallerA);

        // el DTO de entrada no tiene campo TallerId — si alguien lo agrega a mano en el JSON,
        // System.Text.Json lo ignora porque el record no lo declara; este test documenta esa garantía.
        await cliente.PostAsJsonAsync("/api/vehiculos", new { patente = "ZZ999ZZ", tallerId = tallerB.Id });

        var guardado = await _factory.BuscarPorPatenteSinFiltro("ZZ999ZZ");
        Assert.Equal(tallerA.Id, guardado!.TallerId);
    }
}
```

El detalle de `SistemaTallerFactory` (un `WebApplicationFactory` que arranca el Testcontainer, corre las migraciones, y expone un método para emitir un JWT válido de un taller de prueba) es la primera pieza de infraestructura de test que conviene escribir — todo lo demás la reutiliza.

**Entornos sin Docker.** Si la variable de entorno `TESTS_CONNECTION_STRING` está definida, `SistemaTallerFactory` usa ese Postgres en vez de levantar un Testcontainer: crea una base con nombre único por corrida (por ejemplo `tests_<guid>`), aplica las migraciones y la borra al terminar. Así los tests corren igual en una máquina o en un agente sin Docker. En la CI la variable no se define y se usa Testcontainers, como en el resto de este documento.

**Estas tres pruebas (listar, pedir por id, alta) se repiten para cada entidad nueva que implemente `IPerteneceATaller`** a medida que se agregan features en [`plan_desarrollo.md`](plan_desarrollo.md) — no hace falta reinventar el fixture, solo instanciar el mismo patrón contra `Servicio`, `OrdenTrabajo`, `Producto`, `Venta`.

## Tests de integración del resto del CRUD

Un test de integración por endpoint, camino feliz + el error más obvio (ej. dar de alta una orden con un `servicio_id` inexistente) — no una combinatoria de todos los campos posibles. Mismo `WebApplicationFactory`, se escriben junto con cada fase de [`plan_desarrollo.md`](plan_desarrollo.md), no antes ni después.

## Unit tests puros (sin base de datos)

Solo donde hay lógica real más allá de "guardar lo que llega". En este proyecto son pocos lugares, y conviene nombrarlos explícitamente para no inventar tests donde no hace falta:

- La regla de stock negativo (RF-28): dado cantidad y stock actual, ¿arma bien el flag `sinStockSuficiente`?
- El matching de "patente conocida" (RF-07): normalización de la patente (mayúsculas, sin espacios) antes de comparar.
- `TallerActual`: ¿qué pasa si el JWT no trae el claim `taller_id`? (debe fallar explícito, no devolver un `Guid` vacío en silencio).

xUnit puro, sin `WebApplicationFactory` ni Testcontainers — corren en milisegundos, van primero en cualquier corrida local.

## Frontend: Vitest + React Testing Library

Pareja natural de Vite. Test de componente para lo que tiene lógica interactiva real, no snapshot de cada pantalla (se rompe con cualquier cambio de texto y no atrapa bugs de verdad):

- La confirmación antes de quitar un servicio de una orden (acción destructiva, RF-19) — que el servicio no se borre hasta confirmar.
- El aviso de patente conocida — que aparezca solo cuando corresponde y pida confirmación explícita, nunca autocomplete solo.
- El aviso suave de stock en 0 — que la venta se pueda confirmar igual, sin bloquear.

## End-to-end: Playwright (uno solo para empezar, recién en la Fase 3)

Playwright ya está listo para usarse sin instalación adicional. E2E rinde cuando hay una app real para clickear, así que no tiene sentido antes de tener algo andando — el primero se escribe apenas se cumple el checkpoint de la Fase 3 de [`plan_desarrollo.md`](plan_desarrollo.md) (login → alta de vehículo → orden con servicios → finalizar), porque ese es exactamente el flujo que más le costaría a un cambio futuro romper sin que nadie se dé cuenta. Un solo test E2E bien elegido vale más que diez sueltos — no hace falta cubrir cada pantalla con E2E, para eso están los tests de integración y de componente.

## Herramientas — resumen

| Capa | Herramienta | Cuándo se escribe |
|---|---|---|
| Aislamiento por `taller_id` | xUnit + `WebApplicationFactory` + `Testcontainers.PostgreSql` | Primero — gate de la Fase 1 |
| Integración backend (resto del CRUD) | xUnit + `WebApplicationFactory` | Junto con cada fase |
| Unit tests backend | xUnit puro | Donde hay lógica real (lista arriba) |
| Componentes frontend | Vitest + React Testing Library | Junto con cada pantalla que tenga lógica interactiva |
| End-to-end | Playwright | Uno, recién en la Fase 3, después se suma alguno más si un flujo crítico nuevo lo justifica |

## Qué no testear

Getters/setters, mapeos triviales de DTO, que ASP.NET Core enrute bien (es código de framework, ya está probado por Microsoft), o que EF Core genere el SQL correcto (se prueba indirectamente al probar el resultado de la query, no la query en sí). Perseguir un número de cobertura incentiva justamente escribir tests de esto — no es el objetivo acá.
