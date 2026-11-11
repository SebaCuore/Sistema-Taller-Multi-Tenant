import type { EstadoOrden } from '@/api/tipos'

export const ETIQUETA_ESTADO: Record<EstadoOrden, string> = {
  Ingresado: 'Ingresado',
  EnProceso: 'En proceso',
  ListoParaRetirar: 'Listo para retirar',
  Entregado: 'Entregado',
}

/** Color por estado; siempre se muestra junto al texto, nunca solo. */
export const CLASE_ESTADO: Record<EstadoOrden, string> = {
  Ingresado: 'border-primary text-primary',
  EnProceso: 'border-proceso text-proceso',
  ListoParaRetirar: 'border-listo text-listo',
  Entregado: 'border-border text-muted-foreground',
}
