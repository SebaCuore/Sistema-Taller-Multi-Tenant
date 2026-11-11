import { ordenesMock } from './ordenes.mock'
import { ordenesReal } from './ordenes.real'

// Igual que vehiculos.ts: el único lugar donde se elige mock o API real.
export const apiOrdenes = import.meta.env.VITE_USAR_MOCKS === 'true' ? ordenesMock : ordenesReal
