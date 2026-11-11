import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { cleanup, render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { MemoryRouter } from 'react-router-dom'
import type { HistorialServicio } from '@/api/tipos'

const base = {
  vehiculoId: 'v', tipo: 'Auto', marca: null, modelo: null, fechaIngreso: '2026-08-12T13:00:00Z',
}
const viejo: HistorialServicio = {
  ...base, ordenId: 'h1', patente: 'AAA111', fechaEntrega: '2026-08-12T16:00:00Z', total: 50000,
  servicios: [{ id: 'i1', servicioId: 's', servicio: 'Balanceo', posicion: 'Delantero', lado: 'Izquierda', precioFinal: 50000, notas: null }],
}
const nuevo: HistorialServicio = {
  ...base, ordenId: 'h2', patente: 'ZZZ999', fechaEntrega: '2026-10-01T16:00:00Z', total: 10000,
  servicios: [{ id: 'i2', servicioId: 's', servicio: 'Rotación', posicion: null, lado: null, precioFinal: 10000, notas: null }],
}

const api = vi.hoisted(() => ({ servicios: vi.fn() }))
vi.mock('@/api/historial', () => ({ apiHistorial: api }))

import HistorialPage from './HistorialPage'

function montar() {
  render(
    <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
      <MemoryRouter>
        <HistorialPage />
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

describe('HistorialPage (RF-22, RF-23)', () => {
  beforeEach(() => {
    localStorage.setItem('sistema-taller.token', 'x')
    api.servicios.mockImplementation(async (patente?: string) => [viejo, nuevo].filter((h) => !patente || h.patente.includes(patente.toUpperCase())))
  })
  afterEach(() => {
    cleanup()
    vi.clearAllMocks()
    localStorage.clear()
  })

  it('muestra fecha, servicios con posición/lado y precio total de cada orden', async () => {
    montar()
    const tabla = await screen.findByRole('table')

    expect(within(tabla).getByText('Balanceo · Delantero · Izquierda')).toBeTruthy()
    expect(within(tabla).getByText('$ 50.000')).toBeTruthy()
    expect(within(tabla).getByText('Rotación')).toBeTruthy()
  })

  it('busca por patente llamando a la API con lo escrito', async () => {
    montar()
    const u = userEvent.setup()
    await screen.findByRole('table')
    await u.type(screen.getByLabelText('Buscar por patente'), 'aaa')
    await u.click(screen.getByRole('button', { name: 'Buscar' }))

    await vi.waitFor(() => expect(api.servicios).toHaveBeenLastCalledWith('aaa'))
    await vi.waitFor(() => expect(within(screen.getByRole('table')).queryByText('ZZZ999')).toBeNull())
  })

  it('avisa con un mensaje claro cuando la patente no tiene servicios', async () => {
    montar()
    const u = userEvent.setup()
    await screen.findByRole('table')
    await u.type(screen.getByLabelText('Buscar por patente'), 'QQQ000')
    await u.click(screen.getByRole('button', { name: 'Buscar' }))

    expect(await screen.findByText('No hay servicios registrados para la patente QQQ000.')).toBeTruthy()
  })

  it('ordena por columna desde un botón visible en el encabezado', async () => {
    montar()
    const u = userEvent.setup()
    const tabla = await screen.findByRole('table')
    const patentes = () => within(tabla).getAllByRole('row').slice(1).map((r) => within(r).getAllByRole('cell')[1].textContent)

    expect(patentes()).toEqual(['ZZZ999', 'AAA111']) // por fecha, la más reciente primero
    await u.click(within(tabla).getByRole('button', { name: /^Patente/ }))
    expect(patentes()).toEqual(['AAA111', 'ZZZ999'])
    await u.click(within(tabla).getByRole('button', { name: /^Patente/ }))
    expect(patentes()).toEqual(['ZZZ999', 'AAA111'])
  })
})
