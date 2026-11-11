import { useState, type FormEvent } from 'react'
import { Link, Navigate, useNavigate, useParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '@/api/vehiculos'
import type { Vehiculo } from '@/api/tipos'
import { Button } from '@/components/ui/button'
import { CLASE_CAMPO } from '@/lib/estilos'
import { obtenerToken } from '@/lib/sesion'

const ETIQUETA = 'flex flex-col gap-2 text-lg font-medium'

export default function EditarVehiculoPage() {
  const { id = '' } = useParams()
  const vehiculo = useQuery({ queryKey: ['vehiculo', id], queryFn: () => api.obtener(id) })

  if (!obtenerToken()) return <Navigate to="/login" replace />
  if (vehiculo.isPending) return <main className="p-4 text-lg">Cargando…</main>
  if (vehiculo.isError)
    return (
      <main className="mx-auto flex max-w-xl flex-col gap-4 p-4">
        <p role="alert" className="text-lg font-medium text-destructive">No se encontró el vehículo.</p>
        <Link to="/historial" className="flex h-14 items-center justify-center rounded-lg border border-border text-lg font-medium">Volver al historial</Link>
      </main>
    )
  // Se monta el formulario recién con los datos cargados, para que arranque con los valores guardados.
  return <Formulario vehiculo={vehiculo.data} />
}

function Formulario({ vehiculo }: { vehiculo: Vehiculo }) {
  const navegar = useNavigate()
  const cliente = useQueryClient()
  const [marca, setMarca] = useState(vehiculo.marca ?? '')
  const [modelo, setModelo] = useState(vehiculo.modelo ?? '')
  const [anio, setAnio] = useState(vehiculo.anio ? String(vehiculo.anio) : '')
  const [contactoNombre, setContactoNombre] = useState(vehiculo.contactoNombre ?? '')
  const [contactoTelefono, setContactoTelefono] = useState(vehiculo.contactoTelefono ?? '')
  const [error, setError] = useState<string | null>(null)
  const editar = useMutation({ mutationFn: (entrada: Parameters<typeof api.editar>[1]) => api.editar(vehiculo.id, entrada) })

  async function enviar(e: FormEvent) {
    e.preventDefault()
    setError(null)
    if (anio && !/^\d{4}$/.test(anio)) return setError('El año tiene que tener 4 números.')
    try {
      // Patente y tipo no se editan acá: se reenvían tal cual.
      await editar.mutateAsync({
        patente: vehiculo.patente,
        tipoVehiculoId: vehiculo.tipoVehiculoId,
        marca: marca || undefined,
        modelo: modelo || undefined,
        anio: anio ? Number(anio) : undefined,
        contactoNombre: contactoNombre || undefined,
        contactoTelefono: contactoTelefono || undefined,
      })
      await cliente.invalidateQueries()
      navegar(-1)
    } catch {
      setError('No se pudo guardar. Revisá los datos y probá de nuevo.')
    }
  }

  return (
    <main className="mx-auto flex min-h-svh w-full max-w-xl flex-col gap-6 px-4 py-8">
      <h1 className="font-heading text-3xl font-bold">Editar vehículo</h1>
      <p className="text-lg">
        <span className="font-heading text-2xl font-bold tracking-wider">{vehiculo.patente}</span> · {vehiculo.tipo}
      </p>
      <form onSubmit={enviar} className="flex flex-col gap-5" noValidate>
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
        <label className={ETIQUETA}>
          Nombre del dueño
          <input value={contactoNombre} onChange={(e) => setContactoNombre(e.target.value)} className={CLASE_CAMPO} />
        </label>
        <label className={ETIQUETA}>
          Teléfono
          <input value={contactoTelefono} onChange={(e) => setContactoTelefono(e.target.value)} inputMode="tel" className={CLASE_CAMPO} />
        </label>
        {error && <p role="alert" className="text-lg font-medium text-destructive">{error}</p>}
        <Button type="submit" disabled={editar.isPending} className="h-14 w-full text-lg">
          {editar.isPending ? 'Guardando…' : 'Guardar cambios'}
        </Button>
        <Button type="button" variant="outline" className="h-14 w-full text-lg" onClick={() => navegar(-1)}>
          Cancelar
        </Button>
      </form>
    </main>
  )
}
