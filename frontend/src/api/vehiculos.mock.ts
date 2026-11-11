import { normalizarPatente } from '@/lib/patente'
import type { ApiVehiculos, TipoVehiculo, Vehiculo } from './tipos'

const tipos: TipoVehiculo[] = [
  { id: 'mock-auto', nombre: 'Auto' },
  { id: 'mock-moto', nombre: 'Moto' },
]

const guardados: Vehiculo[] = [
  {
    id: 'mock-1', patente: 'ABC123', tipoVehiculoId: 'mock-auto', tipo: 'Auto', marca: 'Renault',
    modelo: 'Kangoo', anio: 2016, contactoNombre: 'Juan Pérez', contactoTelefono: '388 415 2233',
  },
]

/** Datos en memoria, para construir pantallas sin backend. Se activa con VITE_USAR_MOCKS=true. */
export const vehiculosMock: ApiVehiculos = {
  async tiposVehiculo() {
    return tipos
  },
  async buscarPorPatente(patente) {
    const p = normalizarPatente(patente)
    return [...guardados].reverse().find((v) => v.patente === p) ?? null
  },
  async crear(entrada) {
    const nuevo: Vehiculo = {
      id: crypto.randomUUID(),
      patente: normalizarPatente(entrada.patente),
      tipoVehiculoId: entrada.tipoVehiculoId,
      tipo: tipos.find((t) => t.id === entrada.tipoVehiculoId)?.nombre ?? '',
      marca: entrada.marca ?? null,
      modelo: entrada.modelo ?? null,
      anio: entrada.anio ?? null,
      contactoNombre: entrada.contactoNombre ?? null,
      contactoTelefono: entrada.contactoTelefono ?? null,
    }
    guardados.push(nuevo)
    return nuevo
  },
  async editar(id, entrada) {
    const actual = guardados.find((v) => v.id === id)
    if (!actual) throw new Error('No existe')
    Object.assign(actual, entrada, { patente: normalizarPatente(entrada.patente) })
    return actual
  },
}
