const CLAVE = 'sistema-taller.token'

// localStorage puede no estar disponible (modo privado, datos bloqueados): se tolera.
export function obtenerToken(): string | null {
  try {
    return localStorage.getItem(CLAVE)
  } catch {
    return null
  }
}

export function guardarToken(token: string): void {
  try {
    localStorage.setItem(CLAVE, token)
  } catch {
    /* sin persistencia: la sesión vale hasta recargar */
  }
}

export function borrarToken(): void {
  try {
    localStorage.removeItem(CLAVE)
  } catch {
    /* nada que borrar */
  }
}
