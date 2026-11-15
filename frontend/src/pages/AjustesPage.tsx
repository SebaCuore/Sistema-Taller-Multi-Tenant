import { useState, type FormEvent } from 'react'
import { Navigate, useNavigate } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { cambiarPassword } from '@/api/auth'
import { apiCatalogo } from '@/api/catalogo'
import { ErrorApi } from '@/api/cliente'
import type { Servicio } from '@/api/tipos'
import Layout from '@/components/Layout'
import { Button } from '@/components/ui/button'
import { CLASE_CAMPO } from '@/lib/estilos'
import { leerMonto } from '@/lib/numeros'
import { borrarToken, obtenerToken } from '@/lib/sesion'
import { pesos } from '@/lib/tiempo'

const CATEGORIAS: Servicio['categoriaVehiculo'][] = ['Auto', 'Moto', 'Ambos']
const ETIQUETA = 'flex flex-col gap-2 text-lg font-medium'
const CLASE_OPCION =
  'flex h-14 items-center justify-center rounded-lg border border-input px-4 text-lg has-[:checked]:border-primary has-[:checked]:bg-primary has-[:checked]:text-primary-foreground'
const CLASE_TILDE = 'flex min-h-14 cursor-pointer items-center gap-3 rounded-lg border border-input px-4 text-lg'

function detalle(s: Servicio) {
  const partes = [s.categoriaVehiculo === 'Ambos' ? 'Auto y moto' : s.categoriaVehiculo]
  if (s.aplicaPosicion) partes.push('pide posición')
  if (s.aplicaLado) partes.push('pide lado')
  return partes.join(' · ')
}

/** Ajustes: catálogo de servicios del taller (RF-09 a RF-13) y cerrar sesión. */
export default function AjustesPage() {
  const navegar = useNavigate()
  const cliente = useQueryClient()
  const [agregando, setAgregando] = useState(false)
  const [editando, setEditando] = useState<{ id: string; precio: string } | null>(null)
  const [corrigiendo, setCorrigiendo] = useState<string | null>(null)
  const [cambiandoPassword, setCambiandoPassword] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const catalogo = useQuery({ queryKey: ['catalogo'], queryFn: apiCatalogo.listar })
  const refrescar = () => {
    void cliente.invalidateQueries({ queryKey: ['catalogo'] })
    void cliente.invalidateQueries({ queryKey: ['servicios'] }) // el selector de las órdenes
  }
  const cambiar = useMutation({
    mutationFn: (x: { id: string; cambio: Parameters<typeof apiCatalogo.cambiar>[1] }) => apiCatalogo.cambiar(x.id, x.cambio),
    onSuccess: refrescar,
    onError: () => setError('No se pudo guardar el cambio. Probá de nuevo.'),
  })

  if (!obtenerToken()) return <Navigate to="/login" replace />

  async function guardarPrecio(s: Servicio) {
    if (!editando) return
    setError(null)
    const texto = editando.precio.trim()
    const monto = leerMonto(texto)
    if (texto !== '' && monto === null) return setError('Escribí un precio válido: solo números. Dejalo vacío para "A definir".')
    await cambiar.mutateAsync({ id: s.id, cambio: monto === null ? { sinPrecio: true } : { precioReferencia: monto } })
    setEditando(null)
  }

  return (
    <Layout>
      <main className="mx-auto flex w-full max-w-3xl flex-col gap-6 px-4 pt-6 pb-28 md:pb-10">
        <h1 className="font-heading text-3xl font-bold">Ajustes</h1>

        <section aria-labelledby="titulo-catalogo" className="flex flex-col gap-4">
          <h2 id="titulo-catalogo" className="font-heading text-2xl font-bold">Catálogo de servicios</h2>
          <p className="text-lg">
            Un servicio desactivado deja de ofrecerse en las órdenes nuevas, pero sigue apareciendo en las órdenes y el historial que ya lo usaban.
          </p>

          {!agregando ? (
            <Button className="h-14 w-full text-lg" onClick={() => setAgregando(true)}>
              + Agregar servicio
            </Button>
          ) : (
            <FormularioServicio
              onCancelar={() => setAgregando(false)}
              onGuardado={() => {
                setAgregando(false)
                refrescar()
              }}
            />
          )}

          {catalogo.isPending && <p className="text-lg">Cargando…</p>}
          {catalogo.isError && <p role="alert" className="text-lg font-medium text-destructive">No se pudo cargar el catálogo. Probá de nuevo.</p>}
          {error && <p role="alert" className="text-lg font-medium text-destructive">{error}</p>}

          <ul className="flex flex-col gap-3">
            {catalogo.data?.map((s) => (
              <li key={s.id} className="flex flex-col gap-3 rounded-lg border border-border p-4">
                <div className="flex flex-wrap items-baseline justify-between gap-2">
                  <span className="text-xl font-bold">{s.nombre}</span>
                  <span className={`rounded-md border-2 px-3 py-1 text-lg font-bold ${s.activo ? 'border-listo text-listo' : 'border-border text-muted-foreground'}`}>
                    {s.activo ? 'Activo' : 'Desactivado'}
                  </span>
                </div>
                <p className="text-lg">{detalle(s)}</p>
                <p className="text-lg">Precio de referencia: <b>{s.precioReferencia != null ? pesos(s.precioReferencia) : 'A definir'}</b></p>

                {corrigiendo === s.id ? (
                  <FormularioServicio
                    inicial={s}
                    onCancelar={() => setCorrigiendo(null)}
                    onGuardado={() => {
                      setCorrigiendo(null)
                      refrescar()
                    }}
                  />
                ) : editando?.id === s.id ? (
                  <div className="flex flex-col gap-3">
                    <label className={ETIQUETA}>
                      Precio de referencia de {s.nombre} ($)
                      <input
                        value={editando.precio}
                        onChange={(e) => setEditando({ id: s.id, precio: e.target.value })}
                        inputMode="decimal"
                        placeholder="A definir"
                        className={CLASE_CAMPO}
                      />
                    </label>
                    <Button className="h-14 w-full text-lg" disabled={cambiar.isPending} onClick={() => guardarPrecio(s)}>
                      Guardar precio
                    </Button>
                    <Button variant="outline" className="h-14 w-full text-lg" onClick={() => setEditando(null)}>
                      Cancelar
                    </Button>
                  </div>
                ) : (
                  <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
                    <Button
                      variant="outline"
                      className="h-14 w-full text-lg"
                      aria-label={`Editar ${s.nombre}`}
                      onClick={() => {
                        setError(null)
                        setCorrigiendo(s.id)
                      }}
                    >
                      Editar servicio
                    </Button>
                    <Button
                      variant="outline"
                      className="h-14 w-full text-lg"
                      aria-label={`Cambiar precio de ${s.nombre}`}
                      onClick={() => setEditando({ id: s.id, precio: s.precioReferencia != null ? String(s.precioReferencia) : '' })}
                    >
                      Cambiar precio
                    </Button>
                    <Button
                      variant="outline"
                      className="h-14 w-full text-lg"
                      aria-label={`${s.activo ? 'Desactivar' : 'Activar'} ${s.nombre}`}
                      disabled={cambiar.isPending}
                      onClick={() => {
                        setError(null)
                        cambiar.mutate({ id: s.id, cambio: { activo: !s.activo } })
                      }}
                    >
                      {s.activo ? 'Desactivar' : 'Activar'}
                    </Button>
                  </div>
                )}
              </li>
            ))}
          </ul>
        </section>

        <section aria-labelledby="titulo-sesion" className="flex flex-col gap-3">
          <h2 id="titulo-sesion" className="font-heading text-2xl font-bold">Sesión</h2>
          {cambiandoPassword ? (
            <FormularioPassword onCerrar={() => setCambiandoPassword(false)} />
          ) : (
            <Button variant="outline" className="h-14 w-full text-lg" onClick={() => setCambiandoPassword(true)}>
              Cambiar contraseña
            </Button>
          )}
          <Button
            variant="outline"
            className="h-14 w-full text-lg"
            onClick={() => {
              borrarToken()
              cliente.clear()
              navegar('/login', { replace: true })
            }}
          >
            Cerrar sesión
          </Button>
        </section>
      </main>
    </Layout>
  )
}

function FormularioServicio({ inicial, onCancelar, onGuardado }: { inicial?: Servicio; onCancelar: () => void; onGuardado: () => void }) {
  const [nombre, setNombre] = useState(inicial?.nombre ?? '')
  const [categoria, setCategoria] = useState<Servicio['categoriaVehiculo']>(inicial?.categoriaVehiculo ?? 'Ambos')
  const [aplicaPosicion, setAplicaPosicion] = useState(inicial?.aplicaPosicion ?? false)
  const [aplicaLado, setAplicaLado] = useState(inicial?.aplicaLado ?? false)
  const [precio, setPrecio] = useState('')
  const [error, setError] = useState<string | null>(null)
  const agregar = useMutation({
    mutationFn: (alta: Parameters<typeof apiCatalogo.agregar>[0]) =>
      inicial
        ? apiCatalogo.cambiar(inicial.id, { nombre: alta.nombre, categoriaVehiculo: alta.categoriaVehiculo, aplicaPosicion: alta.aplicaPosicion, aplicaLado: alta.aplicaLado })
        : apiCatalogo.agregar(alta),
  })

  async function enviar(e: FormEvent) {
    e.preventDefault()
    setError(null)
    if (!nombre.trim()) return setError('Escribí el nombre del servicio.')
    const monto = inicial ? null : leerMonto(precio)
    if (!inicial && precio.trim() !== '' && monto === null) return setError('Escribí un precio válido: solo números.')
    try {
      await agregar.mutateAsync({
        nombre: nombre.trim(),
        categoriaVehiculo: categoria,
        aplicaPosicion,
        aplicaLado,
        precioReferencia: monto ?? undefined,
      })
      onGuardado()
    } catch {
      setError(`No se pudo ${inicial ? 'guardar' : 'agregar'}. Fijate que no exista ya un servicio con ese nombre.`)
    }
  }

  return (
    <form onSubmit={enviar} noValidate className="flex flex-col gap-4 rounded-lg border-2 border-primary p-4">
      <h3 className="font-heading text-xl font-bold">{inicial ? `Editar ${inicial.nombre}` : 'Nuevo servicio'}</h3>
      <label className={ETIQUETA}>
        Nombre
        <input value={nombre} onChange={(e) => setNombre(e.target.value)} className={CLASE_CAMPO} />
      </label>
      <fieldset className="flex flex-col gap-2">
        <legend className="mb-2 text-lg font-medium">Se hace en</legend>
        <div className="grid grid-cols-3 gap-3">
          {CATEGORIAS.map((c) => (
            <label key={c} className={CLASE_OPCION}>
              <input type="radio" name="categoria" checked={categoria === c} onChange={() => setCategoria(c)} className="sr-only" />
              {c}
            </label>
          ))}
        </div>
      </fieldset>
      <label className={CLASE_TILDE}>
        <input type="checkbox" checked={aplicaPosicion} onChange={(e) => setAplicaPosicion(e.target.checked)} className="size-6 accent-primary" />
        Pide posición (delantero / trasero)
      </label>
      <label className={CLASE_TILDE}>
        <input type="checkbox" checked={aplicaLado} onChange={(e) => setAplicaLado(e.target.checked)} className="size-6 accent-primary" />
        Pide lado (izquierda / derecha)
      </label>
      {!inicial && (
        <label className={ETIQUETA}>
          Precio de referencia ($), opcional
          <input value={precio} onChange={(e) => setPrecio(e.target.value)} inputMode="decimal" placeholder="A definir" className={CLASE_CAMPO} />
        </label>
      )}
      {error && <p role="alert" className="text-lg font-medium text-destructive">{error}</p>}
      <Button type="submit" disabled={agregar.isPending} className="h-14 w-full text-lg">
        {agregar.isPending ? 'Guardando…' : inicial ? 'Guardar cambios' : 'Guardar servicio'}
      </Button>
      <Button type="button" variant="outline" className="h-14 w-full text-lg" onClick={onCancelar}>
        Cancelar
      </Button>
    </form>
  )
}

function FormularioPassword({ onCerrar }: { onCerrar: () => void }) {
  const [actual, setActual] = useState('')
  const [nueva, setNueva] = useState('')
  const [repetida, setRepetida] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [guardada, setGuardada] = useState(false)
  const guardar = useMutation({ mutationFn: () => cambiarPassword(actual, nueva) })

  async function enviar(e: FormEvent) {
    e.preventDefault()
    setError(null)
    if (!actual) return setError('Escribí tu contraseña actual.')
    if (nueva.length < 8) return setError('La contraseña nueva tiene que tener al menos 8 caracteres.')
    if (nueva !== repetida) return setError('Las contraseñas nuevas no coinciden.')
    try {
      await guardar.mutateAsync()
      setGuardada(true)
    } catch (err) {
      const mensajes = err instanceof ErrorApi && err.status === 400 ? Object.values(err.errores).flat() : []
      setError(mensajes[0] ?? 'No se pudo cambiar la contraseña. Probá de nuevo.')
    }
  }

  if (guardada)
    return (
      <div className="flex flex-col gap-3 rounded-lg border-2 border-listo p-4">
        <p role="status" className="text-lg font-bold">Contraseña cambiada. Desde ahora entrás con la nueva.</p>
        <Button variant="outline" className="h-14 w-full text-lg" onClick={onCerrar}>Listo</Button>
      </div>
    )

  return (
    <form onSubmit={enviar} noValidate className="flex flex-col gap-4 rounded-lg border-2 border-primary p-4">
      <h3 className="font-heading text-xl font-bold">Cambiar contraseña</h3>
      <label className={ETIQUETA}>
        Contraseña actual
        <input type="password" autoComplete="current-password" value={actual} onChange={(e) => setActual(e.target.value)} className={CLASE_CAMPO} />
      </label>
      <label className={ETIQUETA}>
        Contraseña nueva (mínimo 8 caracteres)
        <input type="password" autoComplete="new-password" value={nueva} onChange={(e) => setNueva(e.target.value)} className={CLASE_CAMPO} />
      </label>
      <label className={ETIQUETA}>
        Repetir contraseña nueva
        <input type="password" autoComplete="new-password" value={repetida} onChange={(e) => setRepetida(e.target.value)} className={CLASE_CAMPO} />
      </label>
      {error && <p role="alert" className="text-lg font-medium text-destructive">{error}</p>}
      <Button type="submit" disabled={guardar.isPending} className="h-14 w-full text-lg">
        {guardar.isPending ? 'Guardando…' : 'Guardar contraseña'}
      </Button>
      <Button type="button" variant="outline" className="h-14 w-full text-lg" onClick={onCerrar}>
        Cancelar
      </Button>
    </form>
  )
}
