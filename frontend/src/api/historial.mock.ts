import { normalizarPatente } from '@/lib/patente'
import type { ApiHistorial, HistorialServicio } from './tipos'

const datos: HistorialServicio[] = [
  {
    ordenId: 'h1', vehiculoId: 'mock-1', patente: 'ABC123', tipo: 'Auto', marca: 'Renault', modelo: 'Kangoo',
    fechaIngreso: '2026-08-12T13:00:00Z', fechaEntrega: '2026-08-12T16:00:00Z', total: 50000,
    servicios: [
      { id: 'i1', servicioId: 's-alin', servicio: 'Alineación', posicion: null, lado: null, precioFinal: 32000, notas: null },
      { id: 'i2', servicioId: 's-bal', servicio: 'Balanceo', posicion: 'Delantero', lado: 'Izquierda', precioFinal: 18000, notas: null },
    ],
  },
]

/** Datos en memoria (VITE_USAR_MOCKS=true). */
export const historialMock: ApiHistorial = {
  async servicios(patente) {
    const p = normalizarPatente(patente ?? '')
    return datos.filter((h) => h.patente.includes(p))
  },
}
