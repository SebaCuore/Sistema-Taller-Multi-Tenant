import { useState } from 'react'
import { Link, Navigate, useNavigate } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { apiOrdenes } from '@/api/ordenes'
import type { EstadoOrden } from '@/api/tipos'
import { Button } from '@/components/ui/button'
import { CLASE_ESTADO, ETIQUETA_ESTADO } from '@/lib/estados'
import { borrarToken, obtenerToken } from '@/lib/sesion'
import { pesos, tiempoTranscurrido } from '@/lib/tiempo'

type Filtro = 'Todos' | Exclude<EstadoOrden, 'Entregado'>
const FILTROS: Filtro[] = ['Todos', 'Ingresado', 'EnProceso', 'ListoParaRetirar']

export default function TallerPage() {
  const navegar = useNavigate()
  const [filtro, setFiltro] = useState<Filtro>('Todos')
  const ordenes = useQuery({ queryKey: ['ordenes'], queryFn: () => apiOrdenes.listar() })

  if (!obtenerToken()) return <Navigate to="/login" replace />

  const visibles = (ordenes.data ?? []).filter((o) => filtro === 'Todos' || o.estado === filtro)

  return (
    <main className="mx-auto flex min-h-svh w-full max-w-5xl flex-col gap-5 px-4 pt-6 pb-28">
      <header className="flex items-center justify-between gap-3">
        <h1 className="font-heading text-3xl font-bold">Taller</h1>
        <Button
          variant="outline"
          className="h-14 px-5 text-lg"
          onClick={() => {
            borrarToken()
            navegar('/login', { replace: true })
          }}
        >
          Cerrar sesión
        </Button>
      </header>

      <div role="group" aria-label="Filtrar por estado" className="flex flex-wrap gap-3">
        {FILTROS.map((f) => (
          <button
            key={f}
            type="button"
            aria-pressed={filtro === f}
            onClick={() => setFiltro(f)}
            className="h-14 rounded-lg border border-input px-5 text-lg font-medium aria-pressed:border-primary aria-pressed:bg-primary aria-pressed:text-primary-foreground"
          >
            {f === 'Todos' ? 'Todos' : ETIQUETA_ESTADO[f]}
          </button>
        ))}
      </div>

      {ordenes.isPending && <p className="text-lg">Cargando…</p>}
      {ordenes.isError && <p role="alert" className="text-lg font-medium text-destructive">No se pudo cargar el taller. Probá de nuevo.</p>}
      {ordenes.isSuccess && visibles.length === 0 && (
        <p className="text-lg">{filtro === 'Todos' ? 'No hay vehículos en el taller ahora.' : 'No hay vehículos en este estado.'}</p>
      )}

      <ul className="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-3">
        {visibles.map((o) => (
          <li key={o.id}>
            <Link
              to={`/ordenes/${o.id}`}
              className="flex min-h-14 flex-col gap-2 rounded-lg border-2 border-border p-4 hover:bg-muted"
            >
              <span className="font-heading text-2xl font-bold tracking-wider">{o.patente}</span>
              <span className="text-lg">
                {o.tipo}
                {[o.marca, o.modelo].filter(Boolean).length > 0 ? ` · ${[o.marca, o.modelo].filter(Boolean).join(' ')}` : ''}
              </span>
              <span className="flex flex-wrap items-center justify-between gap-2">
                <span className={`rounded-md border-2 px-3 py-1 text-lg font-bold ${CLASE_ESTADO[o.estado]}`}>
                  {ETIQUETA_ESTADO[o.estado]}
                </span>
                <span className="text-lg">{tiempoTranscurrido(o.fechaIngreso)}</span>
              </span>
              <span className="text-lg">
                {o.cantidadServicios} {o.cantidadServicios === 1 ? 'servicio' : 'servicios'} · {pesos(o.total)}
              </span>
            </Link>
          </li>
        ))}
      </ul>

      <div className="fixed inset-x-0 bottom-0 border-t border-border bg-background p-4">
        <Link
          to="/vehiculos/nuevo"
          className="mx-auto flex h-14 w-full max-w-md items-center justify-center rounded-lg bg-primary text-lg font-bold text-primary-foreground"
        >
          + Nuevo vehículo
        </Link>
      </div>
    </main>
  )
}
