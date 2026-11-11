import type { ItemEntrada, Lado, Posicion, Servicio } from '@/api/tipos'

/** Un servicio tildado en el selector, con lo que el empleado va completando. */
export interface Seleccion {
  servicioId: string
  posicion: Posicion | ''
  lado: Lado | ''
  precio: string
}

export function nuevaSeleccion(s: Servicio): Seleccion {
  // El precio de referencia solo precarga; el empleado lo puede cambiar.
  return { servicioId: s.id, posicion: '', lado: '', precio: s.precioReferencia != null ? String(s.precioReferencia) : '' }
}

export function aItemEntrada(sel: Seleccion): ItemEntrada {
  const precio = sel.precio.trim() === '' ? undefined : Number(sel.precio)
  return {
    servicioId: sel.servicioId,
    posicion: sel.posicion || undefined,
    lado: sel.lado || undefined,
    precioFinal: precio,
  }
}

/** El selector solo ofrece servicios activos que aplican al tipo de vehículo (Auto/Moto/Ambos). */
export function serviciosParaVehiculo(servicios: Servicio[], tipo: string): Servicio[] {
  return servicios.filter((s) => s.activo && (s.categoriaVehiculo === 'Ambos' || s.categoriaVehiculo === tipo))
}

export function precioValido(precio: string): boolean {
  return precio.trim() === '' || (/^\d+([.,]\d{1,2})?$/.test(precio.trim()) && Number(precio.replace(',', '.')) >= 0)
}
