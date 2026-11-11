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
