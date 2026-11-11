import { pedir } from './cliente'
import type { ApiHistorial, HistorialServicio } from './tipos'

export const historialReal: ApiHistorial = {
  servicios: (patente) =>
    pedir<HistorialServicio[]>(
      patente ? `/api/historial/servicios?patente=${encodeURIComponent(patente)}` : '/api/historial/servicios',
    ),
}
