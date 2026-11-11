import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { cleanup, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import LoginPage from './LoginPage'

function montar() {
  render(
    <MemoryRouter initialEntries={['/login']}>
      <LoginPage />
    </MemoryRouter>,
  )
}

async function completarYEnviar() {
  const usuario = userEvent.setup()
  await usuario.type(screen.getByLabelText('Email'), 'admin@ejemplo.com')
  await usuario.type(screen.getByLabelText('Contraseña'), 'clave-incorrecta')
  await usuario.click(screen.getByRole('button', { name: 'Ingresar' }))
}

describe('LoginPage', () => {
  beforeEach(() => localStorage.clear())
  afterEach(() => {
    cleanup()
    vi.unstubAllGlobals()
  })

  it('muestra un mensaje simple si las credenciales son incorrectas', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response('{}', { status: 401 })))
    montar()
    await completarYEnviar()
    expect((await screen.findByRole('alert')).textContent).toBe('Usuario o contraseña incorrectos')
    expect(localStorage.getItem('sistema-taller.token')).toBeNull()
  })

  it('guarda el token y manda Authorization en pedidos posteriores', async () => {
    const fetchSimulado = vi
      .fn()
      .mockImplementation(async () => new Response(JSON.stringify({ token: 'abc.def.ghi' }), { status: 200 }))
    vi.stubGlobal('fetch', fetchSimulado)
    montar()
    await completarYEnviar()
    await vi.waitFor(() => expect(localStorage.getItem('sistema-taller.token')).toBe('abc.def.ghi'))

    const { pedir } = await import('@/api/cliente')
    await pedir('/api/algo')
    const headers = fetchSimulado.mock.calls.at(-1)![1].headers as Headers
    expect(headers.get('Authorization')).toBe('Bearer abc.def.ghi')
  })
})
