import { NavLink } from 'react-router-dom'

const CLASE =
  'flex h-14 items-center justify-center rounded-lg border border-input px-5 text-lg font-medium aria-[current=page]:border-primary aria-[current=page]:bg-primary aria-[current=page]:text-primary-foreground'

/** Los dos botones grandes de Historial (pantallas.md): Servicios / Ventas. */
export default function PestanasHistorial() {
  return (
    <nav aria-label="Historial" className="grid grid-cols-2 gap-3 md:flex">
      <NavLink to="/historial" end className={CLASE}>
        Servicios
      </NavLink>
      <NavLink to="/historial/ventas" className={CLASE}>
        Ventas
      </NavLink>
    </nav>
  )
}
