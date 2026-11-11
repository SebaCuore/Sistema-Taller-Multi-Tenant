import { useState, type FormEvent } from 'react'
import { Link, Navigate } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { apiHistorial } from '@/api/historial'
import type { HistorialServicio, ItemOrden } from '@/api/tipos'
import Layout from '@/components/Layout'
import { Button } from '@/components/ui/button'
import { CLASE_CAMPO } from '@/lib/estilos'
import { obtenerToken } from '@/lib/sesion'
import { fechaArgentina, pesos } from '@/lib/tiempo'

type Columna = 'fecha' | 'patente' | 'total'

const descripcion = (i: ItemOrden) => [i.servicio, i.posicion, i.lado].filter(Boolean).join(' · ')
const resumen = (h: HistorialServicio) => h.servicios.map(descripcion).join(', ')

function ordenar(lista: HistorialServicio[], col: Columna, asc: boolean) {
  const valor = (h: HistorialServicio) =>
    col === 'fecha' ? new Date(h.fechaEntrega).getTime() : col === 'total' ? h.total : h.patente
  return [...lista].sort((a, b) => {
    const x = valor(a)
    const y = valor(b)
    return (x < y ? -1 : x > y ? 1 : 0) * (asc ? 1 : -1)
  })
}

export default function HistorialPage() {
  const [texto, setTexto] = useState('')
  const [patente, setPatente] = useState('')
  const [col, setCol] = useState<Columna>('fecha')
  const [asc, setAsc] = useState(false)
  const historial = useQuery({ queryKey: ['historial-servicios', patente], queryFn: () => apiHistorial.servicios(patente) })

  if (!obtenerToken()) return <Navigate to="/login" replace />

  const filas = ordenar(historial.data ?? [], col, asc)

  function buscar(e: FormEvent) {
    e.preventDefault()
    setPatente(texto.trim())
  }
  function ordenarPor(c: Columna) {
    if (c === col) setAsc(!asc)
    else {
      setCol(c)
      setAsc(c === 'patente')
    }
  }
  const flecha = (c: Columna) => (c === col ? (asc ? ' ↑' : ' ↓') : '')
  const ariaOrden = (c: Columna) => (c === col ? (asc ? 'ascending' : 'descending') : 'none')

  return (
    <Layout>
      <main className="mx-auto flex w-full max-w-5xl flex-col gap-5 px-4 pt-6 pb-28 md:pb-10">
        <h1 className="font-heading text-3xl font-bold">Historial de servicios</h1>

        <form onSubmit={buscar} role="search" className="flex flex-col gap-3 sm:flex-row">
          <label className="flex flex-1 flex-col gap-2 text-lg font-medium">
            Buscar por patente
            <input
              value={texto}
              onChange={(e) => setTexto(e.target.value)}
              autoCapitalize="characters"
              autoComplete="off"
              placeholder="AB 123 CD"
              className={`${CLASE_CAMPO} font-heading uppercase tracking-wider`}
            />
          </label>
          <Button type="submit" className="h-14 px-8 text-lg sm:self-end">
            Buscar
          </Button>
        </form>

        {historial.isPending && <p className="text-lg">Cargando…</p>}
        {historial.isError && (
          <p role="alert" className="text-lg font-medium text-destructive">
            No se pudo cargar el historial. Probá de nuevo.
          </p>
        )}
        {historial.isSuccess && filas.length === 0 && (
          <p className="text-lg">{patente ? `No hay servicios registrados para la patente ${patente}.` : 'Todavía no hay órdenes finalizadas.'}</p>
        )}

        {/* Mobile: tarjetas */}
        <ul className="flex flex-col gap-4 md:hidden">
          {filas.map((h) => (
            <li key={h.ordenId}>
              <Link to={`/ordenes/${h.ordenId}`} className="flex flex-col gap-2 rounded-lg border-2 border-border p-4">
                <span className="font-heading text-2xl font-bold tracking-wider">{h.patente}</span>
                <span className="text-lg">{fechaArgentina(h.fechaEntrega)}</span>
                <span className="text-lg">{resumen(h) || 'Sin servicios'}</span>
                <span className="text-xl font-bold">{pesos(h.total)}</span>
              </Link>
            </li>
          ))}
        </ul>

        {/* Escritorio: tabla ordenable */}
        {filas.length > 0 && (
          <table className="hidden w-full text-left text-lg md:table">
            <thead>
              <tr className="border-b-2 border-border">
                <th scope="col" aria-sort={ariaOrden('fecha')} className="py-2 pr-4">
                  <button type="button" className="h-14 font-bold" onClick={() => ordenarPor('fecha')}>
                    Fecha{flecha('fecha')}
                  </button>
                </th>
                <th scope="col" aria-sort={ariaOrden('patente')} className="py-2 pr-4">
                  <button type="button" className="h-14 font-bold" onClick={() => ordenarPor('patente')}>
                    Patente{flecha('patente')}
                  </button>
                </th>
                <th scope="col" className="py-2 pr-4 font-bold">Servicios realizados</th>
                <th scope="col" aria-sort={ariaOrden('total')} className="py-2 text-right">
                  <button type="button" className="h-14 font-bold" onClick={() => ordenarPor('total')}>
                    Total{flecha('total')}
                  </button>
                </th>
                <th scope="col" className="py-2 pl-4"><span className="sr-only">Detalle</span></th>
              </tr>
            </thead>
            <tbody>
              {filas.map((h) => (
                <tr key={h.ordenId} className="border-b border-border align-top">
                  <td className="py-3 pr-4">{fechaArgentina(h.fechaEntrega)}</td>
                  <td className="py-3 pr-4 font-heading font-bold tracking-wider">{h.patente}</td>
                  <td className="py-3 pr-4">{resumen(h) || 'Sin servicios'}</td>
                  <td className="py-3 text-right font-bold">{pesos(h.total)}</td>
                  <td className="py-3 pl-4">
                    <Link to={`/ordenes/${h.ordenId}`} className="flex h-14 items-center rounded-lg border border-border px-4 font-medium">
                      Ver detalle
                    </Link>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </main>
    </Layout>
  )
}
