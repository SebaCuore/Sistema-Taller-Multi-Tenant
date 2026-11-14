import { pedir } from './cliente'
import type { ApiStock, Producto, Venta, VentaRegistrada } from './tipos-comercio'

const real: ApiStock = {
  productos: () => pedir<Producto[]>('/api/productos'),
  agregarProducto: (entrada) => pedir<Producto>('/api/productos', { method: 'POST', body: JSON.stringify(entrada) }),
  editarProducto: (id, entrada) => pedir<Producto>(`/api/productos/${id}`, { method: 'PUT', body: JSON.stringify(entrada) }),
  eliminarProducto: (id) => pedir<void>(`/api/productos/${id}`, { method: 'DELETE' }),
  vender: (alta) => pedir<VentaRegistrada>('/api/ventas', { method: 'POST', body: JSON.stringify(alta) }),
  historialVentas: () => pedir<Venta[]>('/api/historial/ventas'),
}

let productos: Producto[] = [{ id: 'p-aceite', nombre: 'Aceite 15W40 · 4 L', precio: 28500, stockActual: 6 }]
const ventas: Venta[] = []

/** Datos en memoria (VITE_USAR_MOCKS=true). */
const mock: ApiStock = {
  async productos() {
    return structuredClone(productos)
  },
  async agregarProducto(entrada) {
    const nuevo = { id: crypto.randomUUID(), ...entrada }
    productos.push(nuevo)
    return { ...nuevo }
  },
  async editarProducto(id, entrada) {
    const p = productos.find((x) => x.id === id)
    if (!p) throw new Error('No existe')
    Object.assign(p, entrada)
    return { ...p }
  },
  async eliminarProducto(id) {
    productos = productos.filter((x) => x.id !== id)
  },
  async vender(alta) {
    const p = productos.find((x) => x.id === alta.productoId)
    if (!p) throw new Error('No existe')
    const sinStockSuficiente = p.stockActual < alta.cantidad
    p.stockActual -= alta.cantidad // puede quedar negativo (RF-28)
    const precioUnitario = alta.precioUnitario ?? p.precio
    const venta: Venta = {
      id: crypto.randomUUID(), productoId: p.id, producto: p.nombre, cantidad: alta.cantidad, precioUnitario,
      total: precioUnitario * alta.cantidad, fecha: new Date().toISOString(),
    }
    ventas.unshift(venta)
    return { ...venta, stockRestante: p.stockActual, sinStockSuficiente }
  },
  async historialVentas() {
    return structuredClone(ventas)
  },
}

// Único punto donde se elige mock o API real.
export const apiStock = import.meta.env.VITE_USAR_MOCKS === 'true' ? mock : real
