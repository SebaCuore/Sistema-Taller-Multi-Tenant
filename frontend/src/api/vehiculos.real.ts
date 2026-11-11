import { ErrorApi, pedir } from './cliente'
import type { ApiVehiculos, TipoVehiculo, Vehiculo, VehiculoEntrada } from './tipos'

export const vehiculosReal: ApiVehiculos = {
  tiposVehiculo: () => pedir<TipoVehiculo[]>('/api/tipos-vehiculo'),

  async buscarPorPatente(patente) {
    try {
      return await pedir<Vehiculo>(`/api/vehiculos/buscar-por-patente/${encodeURIComponent(patente)}`)
    } catch (err) {
      if (err instanceof ErrorApi && err.status === 404) return null
      throw err
    }
  },

  obtener: (id: string) => pedir<Vehiculo>(`/api/vehiculos/${id}`),

  crear: (entrada: VehiculoEntrada) =>
    pedir<Vehiculo>('/api/vehiculos', { method: 'POST', body: JSON.stringify(entrada) }),

  editar: (id: string, entrada: VehiculoEntrada) =>
    pedir<Vehiculo>(`/api/vehiculos/${id}`, { method: 'PUT', body: JSON.stringify(entrada) }),
}
