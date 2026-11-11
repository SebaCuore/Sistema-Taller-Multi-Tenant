import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { cleanup, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { MemoryRouter } from 'react-router-dom'
import type { Vehiculo } from '@/api/tipos'

const conocido: Vehiculo = {
  id: 'v1', patente: 'ABC123', tipoVehiculoId: 'auto', tipo: 'Auto', marca: null, modelo: null, anio: null,
  contactoNombre: 'Juan Pérez', contactoTelefono: '388 415 2233',
}

const api = vi.hoisted(() => ({
  tiposVehiculo: vi.fn(),
  buscarPorPatente: vi.fn(),
  crear: vi.fn(),
  editar: vi.fn(),
}))
vi.mock('@/api/vehiculos', () => ({ api }))

import NuevoVehiculoPage from './NuevoVehiculoPage'

function montar() {
  render(
    <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
      <MemoryRouter>
        <NuevoVehiculoPage />
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

describe('NuevoVehiculoPage — patente conocida (RF-07)', () => {
  beforeEach(() => {
    localStorage.setItem('sistema-taller.token', 'x')
    api.tiposVehiculo.mockResolvedValue([{ id: 'auto', nombre: 'Auto' }, { id: 'moto', nombre: 'Moto' }])
    api.crear.mockImplementation(async (e) => ({ ...conocido, ...e, id: 'nuevo', tipo: 'Auto' }))
  })
  afterEach(() => {
    cleanup()
    vi.clearAllMocks()
    localStorage.clear()
  })

  async function escribirPatente(texto: string) {
    const u = userEvent.setup()
    await u.type(screen.getByLabelText('Patente'), texto)
    await u.tab() // al salir del campo se consulta
    return u
  }

  it('muestra el aviso con el contacto anterior pero NO autocompleta en silencio', async () => {
    api.buscarPorPatente.mockResolvedValue(conocido)
    montar()
    await escribirPatente('abc 123')

    expect(await screen.findByText('Esta patente ya estuvo acá')).toBeTruthy()
    expect(api.buscarPorPatente).toHaveBeenCalledWith('ABC123')
    expect((screen.getByLabelText('Nombre del dueño') as HTMLInputElement).value).toBe('')
    expect((screen.getByLabelText('Teléfono') as HTMLInputElement).value).toBe('')
  })

  it('no deja guardar hasta resolver el aviso', async () => {
    api.buscarPorPatente.mockResolvedValue(conocido)
    montar()
    const u = await escribirPatente('ABC123')
    await screen.findByText('Esta patente ya estuvo acá')

    await u.click(screen.getByRole('button', { name: 'Guardar vehículo' }))

    expect((await screen.findByRole('alert')).textContent).toContain('decidí si sigue siendo el mismo dueño')
    expect(api.crear).not.toHaveBeenCalled()
  })

  it('"Sí, es el mismo" completa el contacto y permite guardar', async () => {
    api.buscarPorPatente.mockResolvedValue(conocido)
    montar()
    const u = await escribirPatente('ABC123')
    await u.click(await screen.findByRole('button', { name: 'Sí, es el mismo' }))

    expect((screen.getByLabelText('Nombre del dueño') as HTMLInputElement).value).toBe('Juan Pérez')
    await u.click(screen.getByRole('button', { name: 'Guardar vehículo' }))

    expect(await screen.findByText('Vehículo guardado')).toBeTruthy()
    expect(api.crear.mock.calls[0][0]).toMatchObject({ patente: 'ABC123', contactoNombre: 'Juan Pérez' })
  })

  it('"No, cargar otro contacto" deja el contacto vacío y permite guardar', async () => {
    api.buscarPorPatente.mockResolvedValue(conocido)
    montar()
    const u = await escribirPatente('ABC123')
    await u.click(await screen.findByRole('button', { name: 'No, cargar otro contacto' }))

    expect((screen.getByLabelText('Nombre del dueño') as HTMLInputElement).value).toBe('')
    await u.click(screen.getByRole('button', { name: 'Guardar vehículo' }))

    expect(await screen.findByText('Vehículo guardado')).toBeTruthy()
    expect(api.crear.mock.calls[0][0].contactoNombre).toBeUndefined()
  })

  it('una patente nueva no muestra ningún aviso', async () => {
    api.buscarPorPatente.mockResolvedValue(null)
    montar()
    await escribirPatente('ZZZ999')

    await vi.waitFor(() => expect(api.buscarPorPatente).toHaveBeenCalled())
    expect(screen.queryByText('Esta patente ya estuvo acá')).toBeNull()
  })
})
