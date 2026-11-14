import { useState, type FormEvent } from 'react'
import { Link, Navigate } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiStock } from '@/api/stock'
import type { Producto, VentaRegistrada } from '@/api/tipos-comercio'
import Layout from '@/components/Layout'
import { Button } from '@/components/ui/button'
import { CLASE_CAMPO } from '@/lib/estilos'
import { leerEntero, leerMonto } from '@/lib/numeros'
import { obtenerToken } from '@/lib/sesion'
import { pesos } from '@/lib/tiempo'

const ETIQUETA = 'flex flex-col gap-2 text-lg font-medium'

/** Ventas de mostrador (RF-26 a RF-29): sin cliente ni vehículo; nunca se bloquea por falta de stock. */
export default function VentasPage() {
  const cliente = useQueryClient()
  const [vendiendo, setVendiendo] = useState(false)
  const [ultima, setUltima] = useState<VentaRegistrada | null>(null)

  if (!obtenerToken()) return <Navigate to="/login" replace />

  if (vendiendo)
    return (
      <Layout>
        <NuevaVenta
          onCancelar={() => setVendiendo(false)}
          onVendido={(v) => {
            setUltima(v)
            setVendiendo(false)
            void cliente.invalidateQueries({ queryKey: ['productos'] })
            void cliente.invalidateQueries({ queryKey: ['historial-ventas'] })
          }}
        />
      </Layout>
    )

  return (
    <Layout>
      <main className="mx-auto flex w-full max-w-xl flex-col gap-5 px-4 pt-6 pb-28 md:pb-10">
        <h1 className="font-heading text-3xl font-bold">Ventas</h1>

        {ultima && (
          <section role="status" className="flex flex-col gap-2 rounded-lg border-2 border-listo p-4">
            <h2 className="font-heading text-xl font-bold">Venta registrada</h2>
            <p className="text-lg">
              {ultima.cantidad} × {ultima.producto} · <b>{pesos(ultima.total)}</b>
            </p>
            {ultima.sinStockSuficiente && (
              <p className="text-lg font-medium text-proceso">
                Aviso: no había stock suficiente cargado. La venta quedó registrada igual y el stock quedó en {ultima.stockRestante}.
              </p>
            )}
          </section>
        )}

        <Button className="h-14 w-full text-lg font-bold" onClick={() => setVendiendo(true)}>
          + Nueva venta
        </Button>
        <Link to="/historial/ventas" className="flex h-14 items-center justify-center rounded-lg border border-border text-lg font-medium">
          Ver ventas anteriores
        </Link>
      </main>
    </Layout>
  )
}

function NuevaVenta({ onCancelar, onVendido }: { onCancelar: () => void; onVendido: (v: VentaRegistrada) => void }) {
  const [busqueda, setBusqueda] = useState('')
  const [elegido, setElegido] = useState<Producto | null>(null)
  const [cantidad, setCantidad] = useState('1')
  const [precio, setPrecio] = useState('')
  const [error, setError] = useState<string | null>(null)
  const productos = useQuery({ queryKey: ['productos'], queryFn: apiStock.productos })
  const vender = useMutation({ mutationFn: apiStock.vender })

  const filtrados = (productos.data ?? []).filter((p) => p.nombre.toLowerCase().includes(busqueda.trim().toLowerCase()))
  const unidades = leerEntero(cantidad)
  // Advertencia suave: informa, pero el botón de confirmar sigue habilitado (RF-28).
  const aviso =
    elegido === null
      ? null
      : elegido.stockActual <= 0
        ? 'Este producto no tiene stock cargado. Podés venderlo igual.'
        : unidades !== null && unidades > elegido.stockActual
          ? `Solo hay ${elegido.stockActual} en stock. Podés venderlo igual.`
          : null

  function elegir(p: Producto) {
    setElegido(p)
    setPrecio(String(p.precio)) // viene del stock; se puede cambiar
    setError(null)
  }

  async function confirmar(e: FormEvent) {
    e.preventDefault()
    setError(null)
    if (!elegido) return setError('Elegí un producto.')
    if (unidades === null || unidades < 1) return setError('La cantidad tiene que ser 1 o más.')
    const monto = leerMonto(precio)
    if (monto === null) return setError('Escribí el precio: solo números.')
    try {
      onVendido(await vender.mutateAsync({ productoId: elegido.id, cantidad: unidades, precioUnitario: monto }))
    } catch {
      setError('No se pudo registrar la venta. Probá de nuevo.')
    }
  }

  return (
    <main className="mx-auto flex w-full max-w-xl flex-col gap-5 px-4 pt-6 pb-28 md:pb-10">
      <h1 className="font-heading text-3xl font-bold">Nueva venta</h1>
      <form onSubmit={confirmar} noValidate className="flex flex-col gap-5">
        {!elegido ? (
          <>
            <label className={ETIQUETA}>
              Buscar producto
              <input value={busqueda} onChange={(e) => setBusqueda(e.target.value)} autoComplete="off" className={CLASE_CAMPO} />
            </label>
            {productos.isPending && <p className="text-lg">Cargando…</p>}
            {productos.isSuccess && filtrados.length === 0 && (
              <p className="text-lg">{productos.data.length === 0 ? 'No hay productos cargados. Agregalos desde Stock.' : 'Ningún producto coincide.'}</p>
            )}
            <ul className="flex flex-col gap-3">
              {filtrados.map((p) => (
                <li key={p.id}>
                  <button
                    type="button"
                    onClick={() => elegir(p)}
                    className="flex min-h-14 w-full flex-col gap-1 rounded-lg border-2 border-border p-4 text-left text-lg hover:bg-muted"
                  >
                    <span className="font-bold">{p.nombre}</span>
                    <span>
                      {pesos(p.precio)} · {p.stockActual <= 0 ? 'sin stock' : `${p.stockActual} en stock`}
                    </span>
                  </button>
                </li>
              ))}
            </ul>
          </>
        ) : (
          <>
            <div className="flex flex-col gap-3 rounded-lg border-2 border-primary p-4">
              <p className="text-xl font-bold">{elegido.nombre}</p>
              <p className="text-lg">{elegido.stockActual <= 0 ? 'Sin stock cargado' : `${elegido.stockActual} en stock`}</p>
              <Button type="button" variant="outline" className="h-14 w-full text-lg" onClick={() => setElegido(null)}>
                Cambiar producto
              </Button>
            </div>
            <label className={ETIQUETA}>
              Cantidad
              <input value={cantidad} onChange={(e) => setCantidad(e.target.value)} inputMode="numeric" className={CLASE_CAMPO} />
            </label>
            <label className={ETIQUETA}>
              Precio por unidad ($)
              <input value={precio} onChange={(e) => setPrecio(e.target.value)} inputMode="decimal" className={CLASE_CAMPO} />
              <span className="text-base font-normal">Viene del stock, lo podés cambiar.</span>
            </label>
            {aviso && (
              <p role="status" className="rounded-lg border-2 border-proceso p-3 text-lg font-medium text-proceso">
                {aviso}
              </p>
            )}
            {unidades !== null && leerMonto(precio) !== null && (
              <p className="text-right text-xl font-bold">Total {pesos(unidades * (leerMonto(precio) ?? 0))}</p>
            )}
          </>
        )}
        {error && <p role="alert" className="text-lg font-medium text-destructive">{error}</p>}
        {elegido && (
          <Button type="submit" disabled={vender.isPending} className="h-14 w-full text-lg font-bold">
            {vender.isPending ? 'Registrando…' : 'Confirmar venta'}
          </Button>
        )}
        <Button type="button" variant="outline" className="h-14 w-full text-lg" onClick={onCancelar}>
          Cancelar
        </Button>
      </form>
    </main>
  )
}
