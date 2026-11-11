import { describe, expect, it } from 'vitest'
import { tiempoTranscurrido } from './tiempo'

const ahora = new Date('2026-11-11T15:00:00Z')

describe('tiempoTranscurrido', () => {
  it.each([
    ['2026-11-11T15:00:00Z', 'recién ingresado'],
    ['2026-11-11T14:55:00Z', 'hace 5 min'],
    ['2026-11-11T12:30:00Z', 'hace 2 h'],
    ['2026-11-08T15:00:00Z', 'hace 3 d'],
    ['2026-11-11T15:30:00Z', 'recién ingresado'], // reloj adelantado: nunca negativo
  ])('%s → %s', (desde, esperado) => {
    expect(tiempoTranscurrido(desde, ahora)).toBe(esperado)
  })
})
