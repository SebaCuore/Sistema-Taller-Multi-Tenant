/** Mayúsculas y sin espacios ni guiones: "af 402-qp" → "AF402QP". Misma regla que el backend. */
export function normalizarPatente(texto: string): string {
  return texto.replace(/[^\p{L}\p{N}]/gu, '').toUpperCase()
}
