import { useState, type FormEvent } from 'react'
import { Navigate } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiStock } from '@/api/stock'
import type { Producto } from '@/api/tipos-comercio'
import Layout from '@/components/Layout'
import { Button } from '@/components/ui/button'
import { CLASE_CAMPO } from '@/lib/estilos'
import { leerEntero, leerMonto } from '@/lib/numeros'
import { obtenerToken } from '@/lib/sesion'
import { pesos } from '@/lib/tiempo'

const ETIQUETA = 'flex flex-col gap-2 text-lg font-medium'

/** Stock / Productos (RF-24, RF-25): nombre, precio y cantidad. */
export default function StockPage() {
  const cliente = useQueryClient()
  // null = lista; 'nuevo' = alta; Producto = edición
  const [formulario, setFormulario] = useState<null | 'nuevo' | Producto>(null)
  const [aEliminar, setAEliminar] = useState<Producto | null>(null)
  const [error, setError] = useState<string | null>(null)

  const productos = useQuery({ queryKey: ['productos'], queryFn: apiStock.productos })
  const eliminar = useMutation({
    mutationFn: apiStock.eliminarProducto,
    onSuccess: () => cliente.invalidateQueries({ queryKey: ['productos'] }),
    onError: () => setError('No se pudo eliminar. Probá de nuevo.'),
  })

  if (!obtenerToken()) return <Navigate to="/login" replace />

  if (formulario)
    return (
      <Layout>
        <FormularioProducto
          producto={formulario === 'nuevo' ? null : formulario}
          onCerrar={() => setFormulario(null)}
          onGuardado={() => {
            setFormulario(null)
            void cliente.invalidateQueries({ queryKey: ['productos'] })
          }}
        />
      </Layout>
    )

  const lista = productos.data ?? []
  const acciones = (p: Producto) => (
    <>
      <Button variant="outline" className="h-14 w-full text-lg" aria-label={`Editar ${p.nombre}`} onClick={() => setFormulario(p)}>
        Editar
      </Button>
      <Button variant="outline" className="h-14 w-full text-lg" aria-label={`Eliminar ${p.nombre}`} onClick={() => setAEliminar(p)}>
        Eliminar
      </Button>
    </>
  )
  const stock = (p: Producto) => (
    <span className={p.stockActual <= 0 ? 'font-bold text-proceso' : ''}>
      {p.stockActual} {p.stockActual <= 0 ? '(sin stock)' : ''}
    </span>
  )

  return (
    <Layout>
      <main className="mx-auto flex w-full max-w-5xl flex-col gap-5 px-4 pt-6 pb-28 md:pb-10">
        <h1 className="font-heading text-3xl font-bold">Stock</h1>
        <Button className="h-14 w-full text-lg font-bold md:w-fit md:px-8" onClick={() => setFormulario('nuevo')}>
          + Agregar producto
        </Button>

        {aEliminar && (
          <section role="alertdialog" aria-labelledby="titulo-eliminar" className="flex flex-col gap-3 rounded-lg border-2 border-destructive p-4">
            <h2 id="titulo-eliminar" className="font-heading text-xl font-bold">¿Eliminar {aEliminar.nombre} del stock?</h2>
            <p className="text-lg">Deja de aparecer en el stock y en las ventas nuevas. Las ventas ya registradas no se pierden.</p>
            <Button
              variant="destructive"
              className="h-14 w-full text-lg font-bold"
              onClick={() => {
                setError(null)
                eliminar.mutate(aEliminar.id)
                setAEliminar(null)
              }}
            >
              Sí, eliminar
            </Button>
            <Button variant="outline" className="h-14 w-full text-lg" onClick={() => setAEliminar(null)}>
              No, dejarlo
            </Button>
          </section>
        )}

        {productos.isPending && <p className="text-lg">Cargando…</p>}
        {productos.isError && <p role="alert" className="text-lg font-medium text-destructive">No se pudo cargar el stock. Probá de nuevo.</p>}
        {error && <p role="alert" className="text-lg font-medium text-destructive">{error}</p>}
        {productos.isSuccess && lista.length === 0 && <p className="text-lg">Todavía no hay productos cargados.</p>}

        {/* Mobile: tarjetas con las acciones a la vista */}
        <ul className="flex flex-col gap-4 md:hidden">
          {lista.map((p) => (
            <li key={p.id} className="flex flex-col gap-3 rounded-lg border-2 border-border p-4">
              <span className="text-xl font-bold">{p.nombre}</span>
              <span className="text-lg">Precio: <b>{pesos(p.precio)}</b></span>
              <span className="text-lg">En stock: {stock(p)}</span>
              {acciones(p)}
            </li>
          ))}
        </ul>

        {/* Escritorio: tabla */}
        {lista.length > 0 && (
          <table className="hidden w-full text-left text-lg md:table">
            <thead>
              <tr className="border-b-2 border-border">
                <th scope="col" className="py-3 pr-4 font-bold">Producto</th>
                <th scope="col" className="py-3 pr-4 text-right font-bold">Precio</th>
                <th scope="col" className="py-3 pr-4 text-right font-bold">En stock</th>
                <th scope="col" className="py-3"><span className="sr-only">Acciones</span></th>
              </tr>
            </thead>
            <tbody>
              {lista.map((p) => (
                <tr key={p.id} className="border-b border-border">
                  <td className="py-3 pr-4 font-medium">{p.nombre}</td>
                  <td className="py-3 pr-4 text-right font-bold">{pesos(p.precio)}</td>
                  <td className="py-3 pr-4 text-right">{stock(p)}</td>
                  <td className="py-3"><div className="grid grid-cols-2 gap-3">{acciones(p)}</div></td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </main>
    </Layout>
  )
}

function FormularioProducto(props: { producto: Producto | null; onCerrar: () => void; onGuardado: () => void }) {
  const { producto } = props
  const [nombre, setNombre] = useState(producto?.nombre ?? '')
  const [precio, setPrecio] = useState(producto ? String(producto.precio) : '')
  const [cantidad, setCantidad] = useState(producto ? String(producto.stockActual) : '')
  const [error, setError] = useState<string | null>(null)
  const guardar = useMutation({
    mutationFn: (e: Parameters<typeof apiStock.agregarProducto>[0]) =>
      producto ? apiStock.editarProducto(producto.id, e) : apiStock.agregarProducto(e),
  })

  async function enviar(e: FormEvent) {
    e.preventDefault()
    setError(null)
    if (!nombre.trim()) return setError('Escribí el nombre del producto.')
    const monto = leerMonto(precio)
    if (monto === null) return setError('Escribí el precio: solo números.')
    const stock = cantidad.trim() === '' ? 0 : leerEntero(cantidad, true)
    if (stock === null) return setError('La cantidad tiene que ser un número entero.')
    try {
      await guardar.mutateAsync({ nombre: nombre.trim(), precio: monto, stockActual: stock })
      props.onGuardado()
    } catch {
      setError('No se pudo guardar. Revisá los datos y probá de nuevo.')
    }
  }

  return (
    <main className="mx-auto flex w-full max-w-xl flex-col gap-6 px-4 pt-6 pb-28 md:pb-10">
      <h1 className="font-heading text-3xl font-bold">{producto ? 'Editar producto' : 'Nuevo producto'}</h1>
      <form onSubmit={enviar} noValidate className="flex flex-col gap-5">
        <label className={ETIQUETA}>
          Nombre
          <input value={nombre} onChange={(e) => setNombre(e.target.value)} className={CLASE_CAMPO} />
        </label>
        <label className={ETIQUETA}>
          Precio ($)
          <input value={precio} onChange={(e) => setPrecio(e.target.value)} inputMode="decimal" className={CLASE_CAMPO} />
        </label>
        <label className={ETIQUETA}>
          Cantidad en stock
          <input value={cantidad} onChange={(e) => setCantidad(e.target.value)} inputMode="numeric" placeholder="0" className={CLASE_CAMPO} />
        </label>
        {error && <p role="alert" className="text-lg font-medium text-destructive">{error}</p>}
        <Button type="submit" disabled={guardar.isPending} className="h-14 w-full text-lg">
          {guardar.isPending ? 'Guardando…' : 'Guardar producto'}
        </Button>
        <Button type="button" variant="outline" className="h-14 w-full text-lg" onClick={props.onCerrar}>
          Cancelar
        </Button>
      </form>
    </main>
  )
}
