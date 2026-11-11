import { useState, type FormEvent } from 'react'
import { Link, Navigate, useNavigate } from 'react-router-dom'
import { useMutation, useQuery } from '@tanstack/react-query'
import { apiOrdenes } from '@/api/ordenes'
import { api } from '@/api/vehiculos'
import type { Vehiculo } from '@/api/tipos'
import SelectorServicios from '@/components/SelectorServicios'
import { Button } from '@/components/ui/button'
import { CLASE_CAMPO } from '@/lib/estilos'
import { normalizarPatente } from '@/lib/patente'
import { aItemEntrada, precioValido, serviciosParaVehiculo, type Seleccion } from '@/lib/seleccion'
import { obtenerToken } from '@/lib/sesion'

// Decisión del usuario ante el aviso de patente conocida (RF-07).
type Decision = 'pendiente' | 'mismo' | 'otro'

const ETIQUETA = 'flex flex-col gap-2 text-lg font-medium'

export default function NuevoVehiculoPage() {
  const [patente, setPatente] = useState('')
  const [patenteConsultada, setPatenteConsultada] = useState('')
  const [tipoElegido, setTipoElegido] = useState('')
  const [marca, setMarca] = useState('')
  const [modelo, setModelo] = useState('')
  const [anio, setAnio] = useState('')
  const [contactoNombre, setContactoNombre] = useState('')
  const [contactoTelefono, setContactoTelefono] = useState('')
  const [decision, setDecision] = useState<Decision>('pendiente')
  const [error, setError] = useState<string | null>(null)
  const [guardado, setGuardado] = useState<Vehiculo | null>(null)

  const tipos = useQuery({ queryKey: ['tipos-vehiculo'], queryFn: api.tiposVehiculo })
  const conocido = useQuery({
    queryKey: ['vehiculo-por-patente', patenteConsultada],
    queryFn: () => api.buscarPorPatente(patenteConsultada),
    enabled: patenteConsultada.length > 0,
  })
  const crear = useMutation({ mutationFn: api.crear })

  if (!obtenerToken()) return <Navigate to="/login" replace />

  const tipoId = tipoElegido || tipos.data?.[0]?.id || ''
  const avisoVisible = !!conocido.data && decision === 'pendiente'

  function alSalirDePatente() {
    const p = normalizarPatente(patente)
    if (p !== patenteConsultada) {
      setPatenteConsultada(p)
      setDecision('pendiente') // otra patente: la decisión anterior ya no vale
    }
  }

  async function enviar(e: FormEvent) {
    e.preventDefault()
    setError(null)
    const p = normalizarPatente(patente)
    if (!p) return setError('Escribí la patente.')
    if (!tipoId) return setError('Elegí el tipo de vehículo.')
    // El aviso de patente conocida hay que resolverlo a mano antes de guardar.
    if (p !== patenteConsultada) return alSalirDePatente()
    if (conocido.data && decision === 'pendiente')
      return setError('Antes de guardar, decidí si sigue siendo el mismo dueño.')
    if (anio && !/^\d{4}$/.test(anio)) return setError('El año tiene que tener 4 números.')

    try {
      setGuardado(
        await crear.mutateAsync({
          patente: p,
          tipoVehiculoId: tipoId,
          marca: marca || undefined,
          modelo: modelo || undefined,
          anio: anio ? Number(anio) : undefined,
          contactoNombre: contactoNombre || undefined,
          contactoTelefono: contactoTelefono || undefined,
        }),
      )
    } catch {
      setError('No se pudo guardar. Revisá los datos y probá de nuevo.')
    }
  }

  if (guardado) return <PasoServicios vehiculo={guardado} />

  return (
    <main className="mx-auto flex min-h-svh w-full max-w-xl flex-col gap-6 px-4 py-8">
      <h1 className="font-heading text-3xl font-bold">Nuevo vehículo</h1>
      <form onSubmit={enviar} className="flex flex-col gap-5" noValidate>
        <label className={ETIQUETA}>
          Patente
          <input
            value={patente}
            onChange={(e) => setPatente(e.target.value)}
            onBlur={alSalirDePatente}
            autoCapitalize="characters"
            autoComplete="off"
            placeholder="AB 123 CD"
            className={`${CLASE_CAMPO} font-heading uppercase tracking-wider`}
          />
        </label>

        {avisoVisible && conocido.data && (
          <section aria-labelledby="aviso-titulo" className="flex flex-col gap-3 rounded-lg border-2 border-proceso p-4">
            <h2 id="aviso-titulo" className="font-heading text-xl font-bold">
              Esta patente ya estuvo acá
            </h2>
            <p className="text-lg">
              La última vez el contacto era{' '}
              <b>
                {[conocido.data.contactoNombre, conocido.data.contactoTelefono].filter(Boolean).join(' · ') ||
                  'sin datos de contacto'}
              </b>
              . ¿Sigue siendo el dueño?
            </p>
            <Button
              type="button"
              className="h-14 w-full text-lg"
              onClick={() => {
                setContactoNombre(conocido.data?.contactoNombre ?? '')
                setContactoTelefono(conocido.data?.contactoTelefono ?? '')
                setDecision('mismo')
              }}
            >
              Sí, es el mismo
            </Button>
            <Button type="button" variant="outline" className="h-14 w-full text-lg" onClick={() => setDecision('otro')}>
              No, cargar otro contacto
            </Button>
          </section>
        )}

        <fieldset className="flex flex-col gap-2">
          <legend className="mb-2 text-lg font-medium">Tipo de vehículo</legend>
          <div className="grid grid-cols-2 gap-3">
            {tipos.data?.map((t) => (
              <label
                key={t.id}
                className="flex h-14 items-center justify-center gap-2 rounded-lg border border-input text-lg has-[:checked]:border-primary has-[:checked]:bg-primary has-[:checked]:text-primary-foreground"
              >
                <input
                  type="radio"
                  name="tipo"
                  value={t.id}
                  checked={tipoId === t.id}
                  onChange={() => setTipoElegido(t.id)}
                  className="sr-only"
                />
                {t.nombre}
              </label>
            ))}
          </div>
        </fieldset>

        <div className="grid grid-cols-1 gap-5 sm:grid-cols-3">
          <label className={ETIQUETA}>
            Marca
            <input value={marca} onChange={(e) => setMarca(e.target.value)} className={CLASE_CAMPO} />
          </label>
          <label className={ETIQUETA}>
            Modelo
            <input value={modelo} onChange={(e) => setModelo(e.target.value)} className={CLASE_CAMPO} />
          </label>
          <label className={ETIQUETA}>
            Año
            <input value={anio} onChange={(e) => setAnio(e.target.value)} inputMode="numeric" className={CLASE_CAMPO} />
          </label>
        </div>

        <div className="grid grid-cols-1 gap-5 sm:grid-cols-2">
          <label className={ETIQUETA}>
            Nombre del dueño
            <input value={contactoNombre} onChange={(e) => setContactoNombre(e.target.value)} className={CLASE_CAMPO} />
          </label>
          <label className={ETIQUETA}>
            Teléfono
            <input
              value={contactoTelefono}
              onChange={(e) => setContactoTelefono(e.target.value)}
              inputMode="tel"
              className={CLASE_CAMPO}
            />
          </label>
        </div>

        {error && (
          <p role="alert" className="text-lg font-medium text-destructive">
            {error}
          </p>
        )}
        <Button type="submit" disabled={crear.isPending} className="h-14 w-full text-lg">
          {crear.isPending ? 'Guardando…' : 'Guardar vehículo'}
        </Button>
        <Link to="/" className="flex h-14 items-center justify-center rounded-lg border border-border text-lg font-medium">
          Cancelar
        </Link>
      </form>
    </main>
  )
}

/** Paso 2 de la Pantalla 2: elegir los servicios y crear la orden del vehículo recién guardado. */
function PasoServicios({ vehiculo }: { vehiculo: Vehiculo }) {
  const navegar = useNavigate()
  const [seleccion, setSeleccion] = useState<Seleccion[]>([])
  const [error, setError] = useState<string | null>(null)
  const catalogo = useQuery({ queryKey: ['servicios'], queryFn: apiOrdenes.servicios })
  const crear = useMutation({ mutationFn: apiOrdenes.crear })

  async function crearOrden() {
    setError(null)
    if (seleccion.length === 0) return setError('Elegí al menos un servicio.')
    if (seleccion.some((s) => !precioValido(s.precio))) return setError('Revisá los precios: solo números.')
    try {
      await crear.mutateAsync({
        vehiculoId: vehiculo.id,
        servicios: seleccion.map((s) => aItemEntrada({ ...s, precio: s.precio.replace(',', '.') })),
      })
      navegar('/')
    } catch {
      setError('No se pudo crear la orden. Probá de nuevo.')
    }
  }

  return (
    <main className="mx-auto flex min-h-svh w-full max-w-xl flex-col gap-6 px-4 py-8">
      <h1 className="font-heading text-3xl font-bold">Servicios</h1>
      <p role="status" className="text-lg">
        {vehiculo.patente} · {vehiculo.tipo}. Elegí uno o varios servicios. El precio viene sugerido y lo podés cambiar.
      </p>
      {catalogo.isPending ? (
        <p className="text-lg">Cargando servicios…</p>
      ) : (
        <SelectorServicios
          servicios={serviciosParaVehiculo(catalogo.data ?? [], vehiculo.tipo)}
          valor={seleccion}
          onChange={setSeleccion}
        />
      )}
      {error && (
        <p role="alert" className="text-lg font-medium text-destructive">
          {error}
        </p>
      )}
      <Button className="h-14 w-full text-lg" disabled={crear.isPending} onClick={crearOrden}>
        {crear.isPending ? 'Creando…' : 'Crear orden'}
      </Button>
      <Link to="/" className="flex h-14 items-center justify-center rounded-lg border border-border text-lg font-medium">
        Dejar para después
      </Link>
    </main>
  )
}
