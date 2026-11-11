import { pedir } from './cliente'
import type { ApiOrdenes, OrdenDetalle, OrdenResumen, Servicio } from './tipos'

const enJson = (cuerpo: unknown) => JSON.stringify(cuerpo)

export const ordenesReal: ApiOrdenes = {
  servicios: () => pedir<Servicio[]>('/api/servicios?soloActivos=true'),
  listar: (estado) => pedir<OrdenResumen[]>(estado ? `/api/ordenes?estado=${estado}` : '/api/ordenes'),
  obtener: (id) => pedir<OrdenDetalle>(`/api/ordenes/${id}`),
  crear: (alta) => pedir<OrdenDetalle>('/api/ordenes', { method: 'POST', body: enJson(alta) }),
  agregarServicio: (id, item) =>
    pedir<OrdenDetalle>(`/api/ordenes/${id}/servicios`, { method: 'POST', body: enJson(item) }),
  quitarServicio: (id, itemId) =>
    pedir<OrdenDetalle>(`/api/ordenes/${id}/servicios/${itemId}`, { method: 'DELETE' }),
  cambiarEstado: (id, estado) =>
    pedir<OrdenDetalle>(`/api/ordenes/${id}/estado`, { method: 'PATCH', body: enJson({ estado }) }),
  finalizar: (id) => pedir<OrdenDetalle>(`/api/ordenes/${id}/finalizar`, { method: 'POST' }),
}
