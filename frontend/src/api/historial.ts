import { historialMock } from './historial.mock'
import { historialReal } from './historial.real'

export const apiHistorial = import.meta.env.VITE_USAR_MOCKS === 'true' ? historialMock : historialReal
