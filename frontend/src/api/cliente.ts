import { obtenerToken } from '@/lib/sesion'

export const URL_API = import.meta.env.VITE_API_URL ?? 'http://localhost:5108'

export class ErrorApi extends Error {
  status: number
  constructor(status: number, mensaje: string) {
    super(mensaje)
    this.status = status
  }
}

/** fetch contra la API; manda el token en Authorization cuando hay sesión. */
export async function pedir<T>(ruta: string, opciones: RequestInit = {}): Promise<T> {
  const token = obtenerToken()
  const headers = new Headers(opciones.headers)
  if (opciones.body) headers.set('Content-Type', 'application/json')
  if (token) headers.set('Authorization', `Bearer ${token}`)

  const respuesta = await fetch(`${URL_API}${ruta}`, { ...opciones, headers })
  if (!respuesta.ok) throw new ErrorApi(respuesta.status, `Error ${respuesta.status}`)
  return (await respuesta.json()) as T
}
