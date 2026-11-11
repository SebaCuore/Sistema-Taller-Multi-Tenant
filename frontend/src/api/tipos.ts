export interface Vehiculo {
  id: string
  patente: string
  tipoVehiculoId: string
  tipo: string
  marca: string | null
  modelo: string | null
  anio: number | null
  contactoNombre: string | null
  contactoTelefono: string | null
}

/** Lo que se manda al crear o editar. No lleva tallerId: lo decide el servidor por el token. */
export interface VehiculoEntrada {
  patente: string
  tipoVehiculoId: string
  marca?: string
  modelo?: string
  anio?: number
  contactoNombre?: string
  contactoTelefono?: string
}

export interface TipoVehiculo {
  id: string
  nombre: string
}

/** Contrato de la API de vehículos: lo cumplen tanto la implementación real como el mock. */
export interface ApiVehiculos {
  tiposVehiculo(): Promise<TipoVehiculo[]>
  /** El registro más reciente de esa patente, o null si nunca estuvo (RF-07). */
  buscarPorPatente(patente: string): Promise<Vehiculo | null>
  crear(entrada: VehiculoEntrada): Promise<Vehiculo>
  editar(id: string, entrada: VehiculoEntrada): Promise<Vehiculo>
}

// ---- Órdenes de trabajo (Fase 3) ----

export type EstadoOrden = 'Ingresado' | 'EnProceso' | 'ListoParaRetirar' | 'Entregado'
export type Posicion = 'Delantero' | 'Trasero'
export type Lado = 'Izquierda' | 'Derecha'

export interface Servicio {
  id: string
  nombre: string
  categoriaVehiculo: 'Auto' | 'Moto' | 'Ambos'
  aplicaPosicion: boolean
  aplicaLado: boolean
  precioReferencia: number | null
  activo: boolean
}

export interface ItemEntrada {
  servicioId: string
  posicion?: Posicion
  lado?: Lado
  precioFinal?: number
}

export interface OrdenAlta {
  vehiculoId: string
  notas?: string
  servicios: ItemEntrada[]
}

export interface OrdenResumen {
  id: string
  vehiculoId: string
  patente: string
  tipo: string
  marca: string | null
  modelo: string | null
  estado: EstadoOrden
  fechaIngreso: string
  cantidadServicios: number
  total: number
}

export interface ItemOrden {
  id: string
  servicioId: string
  servicio: string
  posicion: Posicion | null
  lado: Lado | null
  precioFinal: number
  notas: string | null
}

export interface OrdenDetalle {
  id: string
  vehiculoId: string
  patente: string
  tipo: string
  marca: string | null
  modelo: string | null
  contactoNombre: string | null
  contactoTelefono: string | null
  estado: EstadoOrden
  fechaIngreso: string
  fechaEntrega: string | null
  notas: string | null
  total: number
  servicios: ItemOrden[]
}

/** Contrato de la API de órdenes y catálogo: lo cumplen la implementación real y el mock. */
export interface ApiOrdenes {
  servicios(): Promise<Servicio[]>
  /** Sin estado: las órdenes en curso (todas menos Entregado). */
  listar(estado?: EstadoOrden): Promise<OrdenResumen[]>
  obtener(id: string): Promise<OrdenDetalle>
  crear(alta: OrdenAlta): Promise<OrdenDetalle>
  agregarServicio(id: string, item: ItemEntrada): Promise<OrdenDetalle>
  quitarServicio(id: string, itemId: string): Promise<OrdenDetalle>
  /** Mientras la orden esté en curso (RF-18). */
  cambiarPrecio(id: string, itemId: string, precioFinal: number): Promise<OrdenDetalle>
  /** Solo estados en curso; para entregar se usa finalizar(). */
  cambiarEstado(id: string, estado: Exclude<EstadoOrden, 'Entregado'>): Promise<OrdenDetalle>
  finalizar(id: string): Promise<OrdenDetalle>
}
