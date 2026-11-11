/** "hace 5 min", "hace 2 h", "hace 3 d": tiempo transcurrido desde el ingreso. */
export function tiempoTranscurrido(desdeIso: string, ahora: Date = new Date()): string {
  const minutos = Math.max(0, Math.floor((ahora.getTime() - new Date(desdeIso).getTime()) / 60000))
  if (minutos < 1) return 'recién ingresado'
  if (minutos < 60) return `hace ${minutos} min`
  const horas = Math.floor(minutos / 60)
  if (horas < 24) return `hace ${horas} h`
  return `hace ${Math.floor(horas / 24)} d`
}

/** Fecha y hora en horario de Argentina (el backend guarda todo en UTC). */
export function fechaArgentina(iso: string): string {
  return new Date(iso).toLocaleString('es-AR', {
    timeZone: 'America/Argentina/Buenos_Aires',
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

export function pesos(monto: number): string {
  return `$ ${monto.toLocaleString('es-AR')}`
}
