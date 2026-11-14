import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { cleanup, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { MemoryRouter } from 'react-router-dom'
import type { ReactNode } from 'react'
import type { Servicio } from '@/api/tipos'
import type { Producto } from '@/api/tipos-comercio'

const stock = vi.hoisted(() => ({
  productos: vi.fn(),
  agregarProducto: vi.fn(),
  editarProducto: vi.fn(),
  eliminarProducto: vi.fn(),
  vender: vi.fn(),
  historialVentas: vi.fn(),
}))
const catalogo = vi.hoisted(() => ({ listar: vi.fn(), agregar: vi.fn(), cambiar: vi.fn() }))
vi.mock('@/api/stock', () => ({ apiStock: stock }))
vi.mock('@/api/catalogo', () => ({ apiCatalogo: catalogo }))

import AjustesPage from './AjustesPage'
import StockPage from './StockPage'
import VentasPage from './VentasPage'

const sinStock: Producto = { id: 'p1', nombre: 'Bujía', precio: 3000, stockActual: 0 }
const conStock: Producto = { id: 'p2', nombre: 'Aceite', precio: 28500, stockActual: 6 }
const balanceo: Servicio = {
  id: 's1', nombre: 'Balanceo', categoriaVehiculo: 'Ambos', aplicaPosicion: true, aplicaLado: true, precioReferencia: 18000, activo: true,
}

function montar(pagina: ReactNode) {
  render(
    <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
      <MemoryRouter>{pagina}</MemoryRouter>
    </QueryClientProvider>,
  )
  return userEvent.setup()
}

beforeEach(() => {
  localStorage.setItem('sistema-taller.token', 'x')
  stock.productos.mockResolvedValue([sinStock, conStock])
  catalogo.listar.mockResolvedValue([balanceo])
})
afterEach(() => {
  cleanup()
  vi.clearAllMocks()
  localStorage.clear()
})

describe('VentasPage — stock en 0 (RF-28)', () => {
  it('avisa suavemente que no hay stock pero deja confirmar la venta igual', async () => {
    stock.vender.mockResolvedValue({
      id: 'v1', productoId: 'p1', producto: 'Bujía', cantidad: 1, precioUnitario: 3000, total: 3000,
      fecha: '2026-11-14T12:00:00Z', stockRestante: -1, sinStockSuficiente: true,
    })
    const u = montar(<VentasPage />)
    await u.click(screen.getByRole('button', { name: '+ Nueva venta' }))
    await u.click(await screen.findByRole('button', { name: /Bujía/ }))

    expect(screen.getByRole('status').textContent).toContain('no tiene stock cargado')
    const confirmar = screen.getByRole('button', { name: 'Confirmar venta' }) as HTMLButtonElement
    expect(confirmar.disabled).toBe(false)

    await u.click(confirmar)
    await vi.waitFor(() => expect(stock.vender.mock.calls[0][0]).toEqual({ productoId: 'p1', cantidad: 1, precioUnitario: 3000 }))
    expect(await screen.findByText('Venta registrada')).toBeTruthy()
    expect(screen.getByText(/La venta quedó registrada igual/)).toBeTruthy()
    expect(screen.queryByRole('alert')).toBeNull() // es un aviso, no un error
  })

  it('con stock suficiente no muestra ningún aviso y precarga el precio del producto', async () => {
    const u = montar(<VentasPage />)
    await u.click(screen.getByRole('button', { name: '+ Nueva venta' }))
    await u.click(await screen.findByRole('button', { name: /Aceite/ }))

    expect(screen.queryByRole('status')).toBeNull()
    expect((screen.getByLabelText(/Precio por unidad/) as HTMLInputElement).value).toBe('28500')
  })

  it('pedir más de lo que hay también avisa sin bloquear', async () => {
    const u = montar(<VentasPage />)
    await u.click(screen.getByRole('button', { name: '+ Nueva venta' }))
    await u.click(await screen.findByRole('button', { name: /Aceite/ }))
    const cantidad = screen.getByLabelText('Cantidad')
    await u.clear(cantidad)
    await u.type(cantidad, '10')

    expect(screen.getByRole('status').textContent).toContain('Solo hay 6 en stock')
    expect((screen.getByRole('button', { name: 'Confirmar venta' }) as HTMLButtonElement).disabled).toBe(false)
  })
})

describe('StockPage', () => {
  it('pide confirmación antes de eliminar un producto', async () => {
    stock.eliminarProducto.mockResolvedValue(undefined)
    const u = montar(<StockPage />)
    await u.click((await screen.findAllByRole('button', { name: 'Eliminar Aceite' }))[0])

    expect(screen.getByRole('alertdialog')).toBeTruthy()
    expect(stock.eliminarProducto).not.toHaveBeenCalled()

    await u.click(screen.getByRole('button', { name: 'No, dejarlo' }))
    expect(screen.queryByRole('alertdialog')).toBeNull()
    expect(stock.eliminarProducto).not.toHaveBeenCalled()

    await u.click(screen.getAllByRole('button', { name: 'Eliminar Aceite' })[0])
    await u.click(screen.getByRole('button', { name: 'Sí, eliminar' }))
    await vi.waitFor(() => expect(stock.eliminarProducto.mock.calls[0][0]).toBe('p2'))
  })

  it('agrega un producto con nombre, precio y cantidad', async () => {
    stock.agregarProducto.mockResolvedValue({ id: 'p3', nombre: 'Filtro', precio: 9500.5, stockActual: 4 })
    const u = montar(<StockPage />)
    await u.click(await screen.findByRole('button', { name: '+ Agregar producto' }))
    await u.type(screen.getByLabelText('Nombre'), 'Filtro')
    await u.type(screen.getByLabelText('Precio ($)'), '9500,50')
    await u.type(screen.getByLabelText('Cantidad en stock'), '4')
    await u.click(screen.getByRole('button', { name: 'Guardar producto' }))

    await vi.waitFor(() => expect(stock.agregarProducto.mock.calls[0][0]).toEqual({ nombre: 'Filtro', precio: 9500.5, stockActual: 4 }))
  })
})

describe('AjustesPage — catálogo de servicios', () => {
  it('desactiva un servicio con un botón con texto (no lo borra)', async () => {
    catalogo.cambiar.mockResolvedValue({ ...balanceo, activo: false })
    const u = montar(<AjustesPage />)
    await u.click(await screen.findByRole('button', { name: 'Desactivar Balanceo' }))

    await vi.waitFor(() => expect(catalogo.cambiar).toHaveBeenCalledWith('s1', { activo: false }))
    expect(screen.queryByRole('button', { name: /Eliminar|Borrar/ })).toBeNull()
  })

  it('agrega un servicio propio', async () => {
    catalogo.agregar.mockResolvedValue({ ...balanceo, id: 's2', nombre: 'Cambio de aceite' })
    const u = montar(<AjustesPage />)
    await u.click(await screen.findByRole('button', { name: '+ Agregar servicio' }))
    await u.type(screen.getByLabelText('Nombre'), 'Cambio de aceite')
    await u.click(screen.getByRole('radio', { name: 'Auto' }))
    await u.click(screen.getByRole('checkbox', { name: /Pide posición/ }))
    await u.type(screen.getByLabelText(/Precio de referencia \(\$\), opcional/), '25000')
    await u.click(screen.getByRole('button', { name: 'Guardar servicio' }))

    await vi.waitFor(() =>
      expect(catalogo.agregar.mock.calls[0][0]).toEqual({
        nombre: 'Cambio de aceite', categoriaVehiculo: 'Auto', aplicaPosicion: true, aplicaLado: false, precioReferencia: 25000,
      }),
    )
  })

  it('cambia el precio de referencia, y vacío lo deja "A definir"', async () => {
    catalogo.cambiar.mockResolvedValue({ ...balanceo, precioReferencia: null })
    const u = montar(<AjustesPage />)
    await u.click(await screen.findByRole('button', { name: 'Cambiar precio de Balanceo' }))
    await u.clear(screen.getByLabelText('Precio de referencia de Balanceo ($)'))
    await u.click(screen.getByRole('button', { name: 'Guardar precio' }))

    await vi.waitFor(() => expect(catalogo.cambiar).toHaveBeenCalledWith('s1', { sinPrecio: true }))
  })
})
