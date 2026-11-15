import { pedir } from './cliente'
import type { Servicio } from './tipos'
import type { ApiCatalogo } from './tipos-comercio'

const real: ApiCatalogo = {
  listar: () => pedir<Servicio[]>('/api/servicios'),
  agregar: (alta) => pedir<Servicio>('/api/servicios', { method: 'POST', body: JSON.stringify(alta) }),
  cambiar: (id, cambio) => pedir<Servicio>(`/api/servicios/${id}`, { method: 'PATCH', body: JSON.stringify(cambio) }),
}

const enMemoria: Servicio[] = [
  { id: 's-alin', nombre: 'Alineación', categoriaVehiculo: 'Auto', aplicaPosicion: true, aplicaLado: false, precioReferencia: 32000, activo: true },
  { id: 's-bal', nombre: 'Balanceo', categoriaVehiculo: 'Ambos', aplicaPosicion: true, aplicaLado: true, precioReferencia: 18000, activo: true },
]

/** Datos en memoria (VITE_USAR_MOCKS=true). */
const mock: ApiCatalogo = {
  async listar() {
    return structuredClone(enMemoria)
  },
  async agregar(alta) {
    const nuevo: Servicio = { id: crypto.randomUUID(), ...alta, precioReferencia: alta.precioReferencia ?? null, activo: true }
    enMemoria.push(nuevo)
    return { ...nuevo }
  },
  async cambiar(id, cambio) {
    const s = enMemoria.find((x) => x.id === id)
    if (!s) throw new Error('No existe')
    if (cambio.activo !== undefined) s.activo = cambio.activo
    if (cambio.nombre !== undefined) s.nombre = cambio.nombre
    if (cambio.categoriaVehiculo !== undefined) s.categoriaVehiculo = cambio.categoriaVehiculo
    if (cambio.aplicaPosicion !== undefined) s.aplicaPosicion = cambio.aplicaPosicion
    if (cambio.aplicaLado !== undefined) s.aplicaLado = cambio.aplicaLado
    if (cambio.sinPrecio) s.precioReferencia = null
    else if (cambio.precioReferencia !== undefined) s.precioReferencia = cambio.precioReferencia
    return { ...s }
  },
}

// Único punto donde se elige mock o API real.
export const apiCatalogo = import.meta.env.VITE_USAR_MOCKS === 'true' ? mock : real
