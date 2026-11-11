import { useState, type FormEvent } from 'react'
import { Navigate, useNavigate } from 'react-router-dom'
import { iniciarSesion } from '@/api/auth'
import { ErrorApi } from '@/api/cliente'
import { Button } from '@/components/ui/button'
import { guardarToken, obtenerToken } from '@/lib/sesion'

const CLASE_CAMPO =
  'h-14 w-full rounded-lg border border-input bg-background px-4 text-lg outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50'

export default function LoginPage() {
  const navegar = useNavigate()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [enviando, setEnviando] = useState(false)

  if (obtenerToken()) return <Navigate to="/" replace />

  async function enviar(e: FormEvent) {
    e.preventDefault()
    setError(null)
    setEnviando(true)
    try {
      const { token } = await iniciarSesion(email, password)
      guardarToken(token)
      navegar('/', { replace: true })
    } catch (err) {
      // Sin detalle técnico (pantallas.md): un 401 y un corte de red muestran el mensaje que corresponde.
      setError(
        err instanceof ErrorApi && err.status === 401
          ? 'Usuario o contraseña incorrectos'
          : 'No se pudo conectar. Probá de nuevo en un momento.',
      )
    } finally {
      setEnviando(false)
    }
  }

  return (
    <main className="mx-auto flex min-h-svh w-full max-w-md flex-col justify-center gap-6 px-4 py-8">
      <h1 className="font-heading text-3xl font-bold">Ingresar al taller</h1>
      <form onSubmit={enviar} className="flex flex-col gap-5">
        <label className="flex flex-col gap-2 text-lg font-medium">
          Email
          <input
            type="email"
            autoComplete="username"
            required
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            className={CLASE_CAMPO}
          />
        </label>
        <label className="flex flex-col gap-2 text-lg font-medium">
          Contraseña
          <input
            type="password"
            autoComplete="current-password"
            required
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            className={CLASE_CAMPO}
          />
        </label>
        {error && (
          <p role="alert" className="text-lg font-medium text-destructive">
            {error}
          </p>
        )}
        <Button type="submit" disabled={enviando} className="h-14 w-full text-lg">
          {enviando ? 'Ingresando…' : 'Ingresar'}
        </Button>
      </form>
    </main>
  )
}
