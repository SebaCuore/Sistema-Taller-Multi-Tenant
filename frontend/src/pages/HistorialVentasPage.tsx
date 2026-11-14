import { useState } from 'react'
import { Navigate } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { apiStock } from '@/api/stock'
import Layout from '@/components/Layout'
import PestanasHistorial from '@/components/PestanasHistorial'
import { obtenerToken } from '@/lib/sesion'
import { fechaArgentina, pesos } from '@/lib/tiempo'

/** Historial de ventas (pantalla 6): tarjetas en mobile, tabla ordenable por fecha en escritorio. */
export default function HistorialVentasPage() {
  const [masNuevaPrimero, setMasNuevaPrimero] = useState(true)
  const ventas = useQuery({ queryKey: ['historial-ventas'], queryFn: apiStock.historialVentas })

  if (!obtenerToken()) return <Navigate to="/login" replace />

  const filas = [...(ventas.data ?? [])].sort(
    (a, b) => (new Date(a.fecha).getTime() - new Date(b.fecha).getTime()) * (masNuevaPrimero ? -1 : 1),
  )

  return (
    <Layout>
      <main className="mx-auto flex w-full max-w-5xl flex-col gap-5 px-4 pt-6 pb-28 md:pb-10">
        <h1 className="font-heading text-3xl font-bold">Historial de ventas</h1>
        <PestanasHistorial />

        {ventas.isPending && <p className="text-lg">Cargando…</p>}
        {ventas.isError && <p role="alert" className="text-lg font-medium text-destructive">No se pudo cargar el historial. Probá de nuevo.</p>}
        {ventas.isSuccess && filas.length === 0 && <p className="text-lg">Todavía no hay ventas registradas.</p>}

        <ul className="flex flex-col gap-4 md:hidden">
          {filas.map((v) => (
            <li key={v.id} className="flex flex-col gap-2 rounded-lg border-2 border-border p-4">
              <span className="text-xl font-bold">{v.producto}</span>
              <span className="text-lg">{fechaArgentina(v.fecha)}</span>
              <span className="text-lg">
                {v.cantidad} × {pesos(v.precioUnitario)}
              </span>
              <span className="text-xl font-bold">{pesos(v.total)}</span>
            </li>
          ))}
        </ul>

        {filas.length > 0 && (
          <table className="hidden w-full text-left text-lg md:table">
            <thead>
              <tr className="border-b-2 border-border">
                <th scope="col" aria-sort={masNuevaPrimero ? 'descending' : 'ascending'} className="py-2 pr-4">
                  <button type="button" className="h-14 font-bold" onClick={() => setMasNuevaPrimero(!masNuevaPrimero)}>
                    Fecha {masNuevaPrimero ? '↓' : '↑'}
                  </button>
                </th>
                <th scope="col" className="py-2 pr-4 font-bold">Producto</th>
                <th scope="col" className="py-2 pr-4 text-right font-bold">Cantidad</th>
                <th scope="col" className="py-2 pr-4 text-right font-bold">Precio</th>
                <th scope="col" className="py-2 text-right font-bold">Total</th>
              </tr>
            </thead>
            <tbody>
              {filas.map((v) => (
                <tr key={v.id} className="border-b border-border">
                  <td className="py-3 pr-4">{fechaArgentina(v.fecha)}</td>
                  <td className="py-3 pr-4 font-medium">{v.producto}</td>
                  <td className="py-3 pr-4 text-right">{v.cantidad}</td>
                  <td className="py-3 pr-4 text-right">{pesos(v.precioUnitario)}</td>
                  <td className="py-3 text-right font-bold">{pesos(v.total)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </main>
    </Layout>
  )
}
