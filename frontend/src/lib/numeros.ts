/** Lee un monto escrito a mano ("1500", "1500,50"). Devuelve null si no es un número válido ≥ 0. */
export function leerMonto(texto: string): number | null {
  const limpio = texto.trim().replace(',', '.')
  if (!/^\d+(\.\d{1,2})?$/.test(limpio)) return null
  return Number(limpio)
}

/** Lee una cantidad entera (acepta negativos solo si se pide, para corregir stock). */
export function leerEntero(texto: string, permitirNegativo = false): number | null {
  const limpio = texto.trim()
  if (!(permitirNegativo ? /^-?\d+$/ : /^\d+$/).test(limpio)) return null
  return Number(limpio)
}
