import type { ApiOrdenes, ItemEntrada, ItemOrden, OrdenDetalle, OrdenResumen, Servicio } from './tipos'

const catalogo: Servicio[] = [
  { id: 's-alin', nombre: 'Alineación', categoriaVehiculo: 'Auto', aplicaPosicion: true, aplicaLado: false, precioReferencia: 32000, activo: true },
  { id: 's-bal', nombre: 'Balanceo', categoriaVehiculo: 'Ambos', aplicaPosicion: true, aplicaLado: true, precioReferencia: 18000, activo: true },
  { id: 's-rot', nombre: 'Rotación', categoriaVehiculo: 'Auto', aplicaPosicion: false, aplicaLado: false, precioReferencia: null, activo: true },
  { id: 's-moto', nombre: 'Service de moto', categoriaVehiculo: 'Moto', aplicaPosicion: false, aplicaLado: false, precioReferencia: null, activo: true },
]

const ordenes: OrdenDetalle[] = []

function item(e: ItemEntrada): ItemOrden {
  const s = catalogo.find((x) => x.id === e.servicioId)!
  return {
    id: crypto.randomUUID(), servicioId: s.id, servicio: s.nombre, posicion: e.posicion ?? null,
    lado: e.lado ?? null, precioFinal: e.precioFinal ?? s.precioReferencia ?? 0, notas: null,
  }
}

function recalcular(o: OrdenDetalle): OrdenDetalle {
  o.total = o.servicios.reduce((suma, i) => suma + i.precioFinal, 0)
  return structuredClone(o)
}

function buscar(id: string): OrdenDetalle {
  const o = ordenes.find((x) => x.id === id)
  if (!o) throw new Error('No existe')
  return o
}

/** Datos en memoria para construir pantallas sin backend (VITE_USAR_MOCKS=true). */
export const ordenesMock: ApiOrdenes = {
  async servicios() {
    return structuredClone(catalogo)
  },
  async listar(estado) {
    const lista = ordenes.filter((o) => (estado ? o.estado === estado : o.estado !== 'Entregado'))
    return lista.map<OrdenResumen>((o) => ({
      id: o.id, vehiculoId: o.vehiculoId, patente: o.patente, tipo: o.tipo, marca: o.marca, modelo: o.modelo,
      estado: o.estado, fechaIngreso: o.fechaIngreso, cantidadServicios: o.servicios.length, total: o.total,
    }))
  },
  async obtener(id) {
    return structuredClone(buscar(id))
  },
  async crear(alta) {
    const nueva: OrdenDetalle = {
      id: crypto.randomUUID(), vehiculoId: alta.vehiculoId, patente: 'MOCK123', tipo: 'Auto', marca: null,
      modelo: null, contactoNombre: null, contactoTelefono: null, estado: 'Ingresado',
      fechaIngreso: new Date().toISOString(), fechaEntrega: null, notas: alta.notas ?? null, total: 0,
      servicios: alta.servicios.map(item),
    }
    ordenes.push(nueva)
    return recalcular(nueva)
  },
  async agregarServicio(id, e) {
    const o = buscar(id)
    o.servicios.push(item(e))
    return recalcular(o)
  },
  async quitarServicio(id, itemId) {
    const o = buscar(id)
    o.servicios = o.servicios.filter((i) => i.id !== itemId)
    return recalcular(o)
  },
  async cambiarEstado(id, estado) {
    const o = buscar(id)
    o.estado = estado
    return recalcular(o)
  },
  async finalizar(id) {
    const o = buscar(id)
    o.estado = 'Entregado'
    o.fechaEntrega = new Date().toISOString()
    return recalcular(o)
  },
}
