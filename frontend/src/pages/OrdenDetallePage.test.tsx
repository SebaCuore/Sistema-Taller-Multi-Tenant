import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { cleanup, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import type { OrdenDetalle } from '@/api/tipos'

const orden: OrdenDetalle = {
  id: 'o1', vehiculoId: 'v1', patente: 'ABC123', tipo: 'Auto', marca: 'Ford', modelo: 'Ranger',
  contactoNombre: 'Juan Pérez', contactoTelefono: null, estado: 'Ingresado',
  fechaIngreso: '2026-11-11T14:00:00Z', fechaEntrega: null, notas: null, total: 50000,
  servicios: [
    { id: 'i1', servicioId: 's1', servicio: 'Balanceo', posicion: 'Delantero', lado: 'Izquierda', precioFinal: 18000, notas: null },
    { id: 'i2', servicioId: 's2', servicio: 'Alineación', posicion: null, lado: null, precioFinal: 32000, notas: null },
  ],
}

const api = vi.hoisted(() => ({
  servicios: vi.fn(),
  obtener: vi.fn(),
  quitarServicio: vi.fn(),
  agregarServicio: vi.fn(),
  cambiarEstado: vi.fn(),
  finalizar: vi.fn(),
}))
vi.mock('@/api/ordenes', () => ({ apiOrdenes: api }))

import OrdenDetallePage from './OrdenDetallePage'

function montar() {
  render(
    <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
      <MemoryRouter initialEntries={['/ordenes/o1']}>
        <Routes>
          <Route path="/ordenes/:id" element={<OrdenDetallePage />} />
          <Route path="/" element={<p>Tablero</p>} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

describe('OrdenDetallePage', () => {
  beforeEach(() => {
    localStorage.setItem('sistema-taller.token', 'x')
    api.obtener.mockResolvedValue(orden)
    api.quitarServicio.mockResolvedValue({ ...orden, servicios: [orden.servicios[1]] })
    api.finalizar.mockResolvedValue({ ...orden, estado: 'Entregado', fechaEntrega: '2026-11-11T15:00:00Z' })
  })
  afterEach(() => {
    cleanup()
    vi.clearAllMocks()
    localStorage.clear()
  })

  it('pide confirmación antes de quitar un servicio y no lo quita hasta confirmar', async () => {
    montar()
    const u = userEvent.setup()
    await u.click(await screen.findByRole('button', { name: 'Quitar Balanceo · Delantero · Izquierda' }))

    expect(await screen.findByRole('alertdialog')).toBeTruthy()
    expect(api.quitarServicio).not.toHaveBeenCalled()

    await u.click(screen.getByRole('button', { name: 'Sí, quitar' }))
    await vi.waitFor(() => expect(api.quitarServicio).toHaveBeenCalledWith('o1', 'i1'))
  })

  it('"No, dejarlo" cierra la confirmación sin quitar nada', async () => {
    montar()
    const u = userEvent.setup()
    await u.click(await screen.findByRole('button', { name: /^Quitar Alineación/ }))
    await u.click(screen.getByRole('button', { name: 'No, dejarlo' }))

    expect(screen.queryByRole('alertdialog')).toBeNull()
    expect(api.quitarServicio).not.toHaveBeenCalled()
  })

  it('finaliza directo desde Ingresado, sin error ni pasos intermedios (RF-20)', async () => {
    montar()
    const u = userEvent.setup()
    await u.click(await screen.findByRole('button', { name: 'Finalizar' }))

    await vi.waitFor(() => expect(api.finalizar).toHaveBeenCalledWith('o1'))
    expect(api.cambiarEstado).not.toHaveBeenCalled()
    expect(await screen.findByText('Tablero')).toBeTruthy()
  })

  it('una orden entregada es de solo lectura: sin Quitar ni Finalizar', async () => {
    api.obtener.mockResolvedValue({ ...orden, estado: 'Entregado', fechaEntrega: '2026-11-11T15:00:00Z' })
    montar()

    await screen.findByText(/Ya no se puede modificar/)
    expect(screen.queryByRole('button', { name: 'Finalizar' })).toBeNull()
    expect(screen.queryByRole('button', { name: /^Quitar/ })).toBeNull()
  })
})
