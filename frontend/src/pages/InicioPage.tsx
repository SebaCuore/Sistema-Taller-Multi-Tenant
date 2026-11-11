import { Link, Navigate, useNavigate } from 'react-router-dom'
import { Button } from '@/components/ui/button'
import { borrarToken, obtenerToken } from '@/lib/sesion'

// Provisoria: el tablero "Taller" real llega en la Fase 3.
export default function InicioPage() {
  const navegar = useNavigate()
  if (!obtenerToken()) return <Navigate to="/login" replace />

  return (
    <main className="mx-auto flex min-h-svh w-full max-w-md flex-col justify-center gap-6 px-4 py-8">
      <h1 className="font-heading text-3xl font-bold">Taller</h1>
      <p className="text-lg">Sesión iniciada.</p>
      <Link
        to="/vehiculos/nuevo"
        className="flex h-14 w-full items-center justify-center rounded-lg bg-primary text-lg font-medium text-primary-foreground"
      >
        + Nuevo vehículo
      </Link>
      <Button
        variant="outline"
        className="h-14 w-full text-lg"
        onClick={() => {
          borrarToken()
          navegar('/login', { replace: true })
        }}
      >
        Cerrar sesión
      </Button>
    </main>
  )
}
