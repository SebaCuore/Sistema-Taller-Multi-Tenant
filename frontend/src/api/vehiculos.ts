import { vehiculosMock } from './vehiculos.mock'
import { vehiculosReal } from './vehiculos.real'

// Único punto donde se elige mock o API real; las pantallas importan solo `api` de acá.
export const api = import.meta.env.VITE_USAR_MOCKS === 'true' ? vehiculosMock : vehiculosReal
