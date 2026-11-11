import { pedir } from './cliente'

export function iniciarSesion(email: string, password: string): Promise<{ token: string }> {
  return pedir('/auth/login', { method: 'POST', body: JSON.stringify({ email, password }) })
}
