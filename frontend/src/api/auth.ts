import { pedir } from './cliente'

export function cambiarPassword(passwordActual: string, passwordNueva: string): Promise<void> {
  return pedir('/auth/cambiar-password', { method: 'POST', body: JSON.stringify({ passwordActual, passwordNueva }) })
}

export function iniciarSesion(email: string, password: string): Promise<{ token: string }> {
  return pedir('/auth/login', { method: 'POST', body: JSON.stringify({ email, password }) })
}
