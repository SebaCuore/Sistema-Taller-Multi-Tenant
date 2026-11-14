import { borrarToken, obtenerToken } from '@/lib/sesion'

// En desarrollo la API corre aparte (puerto 5108). En producción la misma API sirve el frontend: mismo origen.
export const URL_API = import.meta.env.VITE_API_URL ?? (import.meta.env.DEV ? 'http://localhost:5108' : '')

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
  if (respuesta.status === 401 && token && !ruta.startsWith('/auth/')) {
    // Sesión vencida o token inválido: se vuelve al login.
    borrarToken()
    window.location.assign('/login')
  }
  if (!respuesta.ok) throw new ErrorApi(respuesta.status, `Error ${respuesta.status}`)
  if (respuesta.status === 204) return undefined as T // sin contenido (por ejemplo, una baja)
  return (await respuesta.json()) as T
}
