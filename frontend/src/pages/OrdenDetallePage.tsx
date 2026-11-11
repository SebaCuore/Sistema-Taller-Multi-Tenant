import { useState } from 'react'
import { Link, Navigate, useNavigate, useParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiOrdenes } from '@/api/ordenes'
import type { EstadoOrden, ItemOrden } from '@/api/tipos'
import SelectorServicios from '@/components/SelectorServicios'
import { Button } from '@/components/ui/button'
import { CLASE_CAMPO } from '@/lib/estilos'
import { CLASE_ESTADO, ETIQUETA_ESTADO } from '@/lib/estados'
import { aItemEntrada, precioValido, serviciosParaVehiculo, type Seleccion } from '@/lib/seleccion'
import { obtenerToken } from '@/lib/sesion'
import { fechaArgentina, pesos } from '@/lib/tiempo'

type EstadoEnCurso = Exclude<EstadoOrden, 'Entregado'>
const ESTADOS_EN_CURSO: EstadoEnCurso[] = ['Ingresado', 'EnProceso', 'ListoParaRetirar']

function descripcion(i: ItemOrden) {
  return [i.servicio, i.posicion, i.lado].filter(Boolean).join(' · ')
}

export default function OrdenDetallePage() {
  const { id = '' } = useParams()
  const navegar = useNavigate()
  const cliente = useQueryClient()
  const [aQuitar, setAQuitar] = useState<ItemOrden | null>(null)
  const [finalizando, setFinalizando] = useState(false)
  const [editando, setEditando] = useState<{ id: string; precio: string } | null>(null)
  const [agregando, setAgregando] = useState(false)
  const [seleccion, setSeleccion] = useState<Seleccion[]>([])
  const [error, setError] = useState<string | null>(null)

  const orden = useQuery({ queryKey: ['orden', id], queryFn: () => apiOrdenes.obtener(id) })
  const catalogo = useQuery({ queryKey: ['servicios'], queryFn: apiOrdenes.servicios, enabled: agregando })

  const refrescar = () => {
    void cliente.invalidateQueries({ queryKey: ['ordenes'] })
    void cliente.invalidateQueries({ queryKey: ['orden', id] })
  }
  const accion = useMutation({
    mutationFn: (fn: () => Promise<unknown>) => fn(),
    onSuccess: refrescar,
    onError: () => setError('No se pudo completar la acción. Probá de nuevo.'),
  })
  const ejecutar = (fn: () => Promise<unknown>) => {
    setError(null)
    return accion.mutateAsync(fn)
  }

  if (!obtenerToken()) return <Navigate to="/login" replace />
  if (orden.isPending) return <main className="p-4 text-lg">Cargando…</main>
  if (orden.isError)
    return (
      <main className="mx-auto flex max-w-xl flex-col gap-4 p-4">
        <p role="alert" className="text-lg font-medium text-destructive">No se encontró la orden.</p>
        <Link to="/" className="flex h-14 items-center justify-center rounded-lg border border-border text-lg font-medium">Volver al taller</Link>
      </main>
    )

  const o = orden.data
  const cerrada = o.estado === 'Entregado'
  const disponibles = serviciosParaVehiculo(catalogo.data ?? [], o.tipo)

  async function guardarPrecio(i: ItemOrden) {
    if (!editando || editando.precio.trim() === '' || !precioValido(editando.precio))
      return setError('Escribí un precio válido: solo números.')
    await ejecutar(() => apiOrdenes.cambiarPrecio(id, i.id, Number(editando.precio.replace(',', '.'))))
    setEditando(null)
  }

  async function agregar() {
    if (seleccion.some((s) => !precioValido(s.precio))) return setError('Revisá los precios: solo números.')
    for (const s of seleccion) await ejecutar(() => apiOrdenes.agregarServicio(id, aItemEntrada({ ...s, precio: s.precio.replace(',', '.') })))
    setSeleccion([])
    setAgregando(false)
  }

  return (
    <main className="mx-auto flex min-h-svh w-full max-w-2xl flex-col gap-6 px-4 py-6">
      <Link to={cerrada ? '/historial' : '/'} className="flex h-14 w-fit items-center rounded-lg border border-border px-5 text-lg font-medium">
        {cerrada ? '← Volver al historial' : '← Volver al taller'}
      </Link>

      <header className="flex flex-col gap-2">
        <h1 className="font-heading text-3xl font-bold tracking-wider">{o.patente}</h1>
        <p className="text-lg">
          {o.tipo}
          {[o.marca, o.modelo].filter(Boolean).length > 0 ? ` · ${[o.marca, o.modelo].filter(Boolean).join(' ')}` : ''}
        </p>
        {(o.contactoNombre || o.contactoTelefono) && (
          <p className="text-lg">Contacto: {[o.contactoNombre, o.contactoTelefono].filter(Boolean).join(' · ')}</p>
        )}
        <p className="text-lg">Ingresó el {fechaArgentina(o.fechaIngreso)}</p>
        <p>
          <span className={`inline-block rounded-md border-2 px-3 py-1 text-lg font-bold ${CLASE_ESTADO[o.estado]}`}>
            {ETIQUETA_ESTADO[o.estado]}
          </span>
        </p>
        {cerrada && (
          <Link
            to={`/vehiculos/${o.vehiculoId}/editar`}
            className="flex h-14 w-fit items-center rounded-lg border border-border px-5 text-lg font-medium"
          >
            Editar datos del vehículo
          </Link>
        )}
        {cerrada && o.fechaEntrega && <p className="text-lg">Entregada el {fechaArgentina(o.fechaEntrega)}. Ya no se puede modificar.</p>}
      </header>

      <section aria-labelledby="titulo-servicios" className="flex flex-col gap-3">
        <h2 id="titulo-servicios" className="font-heading text-2xl font-bold">Servicios</h2>
        {o.servicios.length === 0 && <p className="text-lg">Todavía no hay servicios cargados.</p>}
        <ul className="flex flex-col gap-3">
          {o.servicios.map((i) => (
            <li key={i.id} className="flex flex-col gap-3 rounded-lg border border-border p-4">
              <div className="flex items-baseline justify-between gap-3 text-lg">
                <span className="font-medium">{descripcion(i)}</span>
                <span className="font-bold">{pesos(i.precioFinal)}</span>
              </div>
              {!cerrada && editando?.id === i.id && (
                <div className="flex flex-col gap-3">
                  <label className="flex flex-col gap-2 text-lg font-medium">
                    Nuevo precio de {i.servicio} ($)
                    <input
                      value={editando.precio}
                      onChange={(e) => setEditando({ id: i.id, precio: e.target.value })}
                      inputMode="decimal"
                      className={CLASE_CAMPO}
                    />
                  </label>
                  <Button className="h-14 w-full text-lg" disabled={accion.isPending} onClick={() => guardarPrecio(i)}>
                    Guardar precio
                  </Button>
                  <Button variant="outline" className="h-14 w-full text-lg" onClick={() => setEditando(null)}>
                    Cancelar
                  </Button>
                </div>
              )}
              {!cerrada && editando?.id !== i.id && (
                <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
                  <Button
                    variant="outline"
                    className="h-14 w-full text-lg"
                    aria-label={`Cambiar precio de ${descripcion(i)}`}
                    onClick={() => setEditando({ id: i.id, precio: String(i.precioFinal) })}
                  >
                    Cambiar precio
                  </Button>
                  <Button
                    variant="outline"
                    className="h-14 w-full text-lg"
                    aria-label={`Quitar ${descripcion(i)}`}
                    onClick={() => setAQuitar(i)}
                  >
                    Quitar
                  </Button>
                </div>
              )}
            </li>
          ))}
        </ul>
        <p className="text-right text-xl font-bold">Total {pesos(o.total)}</p>
      </section>

      {aQuitar && (
        <section role="alertdialog" aria-labelledby="titulo-quitar" className="flex flex-col gap-3 rounded-lg border-2 border-destructive p-4">
          <h2 id="titulo-quitar" className="font-heading text-xl font-bold">¿Quitar {descripcion(aQuitar)} de la orden?</h2>
          <p className="text-lg">Se borra el servicio y su precio de esta orden.</p>
          <Button
            variant="destructive"
            className="h-14 w-full text-lg font-bold"
            onClick={async () => {
              const item = aQuitar
              setAQuitar(null)
              await ejecutar(() => apiOrdenes.quitarServicio(id, item.id))
            }}
          >
            Sí, quitar
          </Button>
          <Button variant="outline" className="h-14 w-full text-lg" onClick={() => setAQuitar(null)}>
            No, dejarlo
          </Button>
        </section>
      )}

      {!cerrada && (
        <section aria-labelledby="titulo-agregar" className="flex flex-col gap-3">
          <h2 id="titulo-agregar" className="font-heading text-2xl font-bold">Agregar servicio</h2>
          {!agregando ? (
            <Button variant="outline" className="h-14 w-full text-lg" onClick={() => setAgregando(true)}>
              + Agregar servicio
            </Button>
          ) : (
            <>
              {catalogo.isPending ? <p className="text-lg">Cargando servicios…</p> : <SelectorServicios servicios={disponibles} valor={seleccion} onChange={setSeleccion} />}
              <Button className="h-14 w-full text-lg" disabled={seleccion.length === 0 || accion.isPending} onClick={agregar}>
                Agregar a la orden
              </Button>
              <Button variant="outline" className="h-14 w-full text-lg" onClick={() => { setAgregando(false); setSeleccion([]) }}>
                Cancelar
              </Button>
            </>
          )}
        </section>
      )}

      {!cerrada && (
        <section aria-labelledby="titulo-estado" className="flex flex-col gap-3">
          <h2 id="titulo-estado" className="font-heading text-2xl font-bold">Estado</h2>
          <p className="text-lg">Marcar los pasos intermedios es opcional: se puede finalizar directamente.</p>
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
            {ESTADOS_EN_CURSO.filter((e) => e !== o.estado).map((e) => (
              <Button key={e} variant="outline" className="h-14 text-lg" disabled={accion.isPending} onClick={() => ejecutar(() => apiOrdenes.cambiarEstado(id, e))}>
                Pasar a {ETIQUETA_ESTADO[e]}
              </Button>
            ))}
          </div>
{!finalizando ? (
            <Button className="h-14 w-full text-lg font-bold" disabled={accion.isPending} onClick={() => setFinalizando(true)}>
              Finalizar
            </Button>
          ) : (
            <div role="alertdialog" aria-labelledby="titulo-finalizar" className="flex flex-col gap-3 rounded-lg border-2 border-primary p-4">
              <h3 id="titulo-finalizar" className="font-heading text-xl font-bold">¿Finalizar la orden de {o.patente}?</h3>
              <p className="text-lg">La orden pasa al historial y ya no se va a poder modificar.</p>
              <Button
                className="h-14 w-full text-lg font-bold"
                disabled={accion.isPending}
                onClick={async () => {
                  await ejecutar(() => apiOrdenes.finalizar(id))
                  navegar('/') // la orden sale del tablero y queda en el historial
                }}
              >
                Sí, finalizar
              </Button>
              <Button variant="outline" className="h-14 w-full text-lg" onClick={() => setFinalizando(false)}>
                No, volver
              </Button>
            </div>
          )}
        </section>
      )}

      {error && <p role="alert" className="text-lg font-medium text-destructive">{error}</p>}
    </main>
  )
}
