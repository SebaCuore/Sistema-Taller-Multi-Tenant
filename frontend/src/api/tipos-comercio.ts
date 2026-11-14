import type { Servicio } from './tipos'

// ---- Catálogo de servicios (Fase 5) ----

export interface ServicioAlta {
  nombre: string
  categoriaVehiculo: Servicio['categoriaVehiculo']
  aplicaPosicion: boolean
  aplicaLado: boolean
  precioReferencia?: number
}

/** Cambios parciales: solo se aplica lo que viene. `sinPrecio` borra el precio de referencia. */
export interface ServicioCambio {
  activo?: boolean
  precioReferencia?: number
  sinPrecio?: boolean
}

export interface ApiCatalogo {
  /** Todo el catálogo del taller, activos e inactivos. */
  listar(): Promise<Servicio[]>
  agregar(alta: ServicioAlta): Promise<Servicio>
  /** Nunca se borra un servicio: se desactiva. */
  cambiar(id: string, cambio: ServicioCambio): Promise<Servicio>
}

// ---- Stock y ventas (Fase 6) ----

export interface Producto {
  id: string
  nombre: string
  precio: number
  stockActual: number
}

export interface ProductoEntrada {
  nombre: string
  precio: number
  stockActual: number
}

export interface VentaAlta {
  productoId: string
  cantidad: number
  precioUnitario?: number
}

export interface Venta {
  id: string
  productoId: string
  producto: string
  cantidad: number
  precioUnitario: number
  total: number
  fecha: string
}

export interface VentaRegistrada extends Venta {
  stockRestante: number
  /** RF-28: la venta se registró igual; solo sirve para mostrar un aviso suave. */
  sinStockSuficiente: boolean
}

export interface ApiStock {
  productos(): Promise<Producto[]>
  agregarProducto(entrada: ProductoEntrada): Promise<Producto>
  editarProducto(id: string, entrada: ProductoEntrada): Promise<Producto>
  eliminarProducto(id: string): Promise<void>
  vender(alta: VentaAlta): Promise<VentaRegistrada>
  historialVentas(): Promise<Venta[]>
}
