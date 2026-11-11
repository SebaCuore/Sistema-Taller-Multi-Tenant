import type { ReactNode } from 'react'
import { NavLink, useNavigate } from 'react-router-dom'
import { Button } from '@/components/ui/button'
import { borrarToken } from '@/lib/sesion'

// Por ahora solo Taller e Historial; Ventas y Stock se suman en la Fase 6.
const ACCESOS = [
  { a: '/', texto: 'Taller', fin: true },
  { a: '/historial', texto: 'Historial', fin: false },
]

const CLASE_ACCESO =
  'flex h-14 items-center justify-center rounded-lg px-4 text-lg font-medium aria-[current=page]:bg-primary aria-[current=page]:text-primary-foreground md:justify-start'

/**
 * Contenedor de navegación (pantallas.md): barra inferior fija en mobile, barra lateral fija en escritorio.
 * Los mismos accesos con texto en ambos; solo cambia el contenedor.
 */
export default function Layout({ children }: { children: ReactNode }) {
  const navegar = useNavigate()
  const salir = () => {
    borrarToken()
    navegar('/login', { replace: true })
  }

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
        <Button variant="outline" className="h-14 w-full text-lg" onClick={salir}>
          Cerrar sesión
        </Button>
      </aside>

      <div className="min-w-0 flex-1">
        <div className="flex justify-end px-4 pt-4 md:hidden">
          <Button variant="outline" className="h-14 px-5 text-lg" onClick={salir}>
            Cerrar sesión
          </Button>
        </div>
        {children}
      </div>

      <nav
        aria-label="Principal"
        className="fixed inset-x-0 bottom-0 z-10 grid h-[4.5rem] grid-cols-2 gap-2 border-t border-border bg-background p-2 md:hidden"
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
