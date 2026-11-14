import type { ReactNode } from 'react'
import { NavLink } from 'react-router-dom'

const ACCESOS = [
  { a: '/', texto: 'Taller', fin: true },
  { a: '/ventas', texto: 'Ventas', fin: false },
  { a: '/stock', texto: 'Stock', fin: false },
  { a: '/historial', texto: 'Historial', fin: false },
]

const CLASE_ACCESO =
  'flex h-14 items-center justify-center rounded-lg px-2 text-base font-medium aria-[current=page]:bg-primary aria-[current=page]:text-primary-foreground md:justify-start md:px-4 md:text-lg'

/**
 * Contenedor de navegación (pantallas.md): barra inferior fija en mobile, barra lateral fija en escritorio.
 * Los mismos accesos con texto en ambos; solo cambia el contenedor. Ajustes (catálogo y cerrar sesión) va aparte.
 */
export default function Layout({ children }: { children: ReactNode }) {
  return (
    <div className="md:flex">
      <aside className="hidden md:sticky md:top-0 md:flex md:h-svh md:w-60 md:shrink-0 md:flex-col md:justify-between md:border-r md:border-border md:p-4">
        <nav aria-label="Principal" className="flex flex-col gap-3">
          {ACCESOS.map((x) => (
            <NavLink key={x.a} to={x.a} end={x.fin} className={CLASE_ACCESO}>
              {x.texto}
            </NavLink>
          ))}
        </nav>
        <NavLink to="/ajustes" className={`${CLASE_ACCESO} border border-border`}>
          ⚙ Ajustes
        </NavLink>
      </aside>

      <div className="min-w-0 flex-1">
        <div className="flex justify-end px-4 pt-4 md:hidden">
          <NavLink to="/ajustes" className={`${CLASE_ACCESO} border border-border px-5 text-lg`}>
            ⚙ Ajustes
          </NavLink>
        </div>
        {children}
      </div>

      <nav
        aria-label="Principal"
        className="fixed inset-x-0 bottom-0 z-10 grid h-[4.5rem] grid-cols-4 gap-1 border-t border-border bg-background p-2 md:hidden"
      >
        {ACCESOS.map((x) => (
          <NavLink key={x.a} to={x.a} end={x.fin} className={CLASE_ACCESO}>
            {x.texto}
          </NavLink>
        ))}
      </nav>
    </div>
  )
}
