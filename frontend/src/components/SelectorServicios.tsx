import type { Lado, Posicion, Servicio } from '@/api/tipos'
import { CLASE_CAMPO } from '@/lib/estilos'
import { nuevaSeleccion, type Seleccion } from '@/lib/seleccion'

interface Props {
  servicios: Servicio[]
  valor: Seleccion[]
  onChange: (valor: Seleccion[]) => void
}

const CLASE_OPCION =
  'flex h-14 items-center justify-center rounded-lg border border-input px-4 text-lg has-[:checked]:border-primary has-[:checked]:bg-primary has-[:checked]:text-primary-foreground'

function Opciones<T extends string>(props: {
  nombre: string
  leyenda: string
  opciones: T[]
  valor: T | ''
  onChange: (v: T) => void
}) {
  return (
    <fieldset className="flex flex-col gap-2">
      <legend className="mb-1 text-lg font-medium">{props.leyenda}</legend>
      <div className="grid grid-cols-2 gap-3">
        {props.opciones.map((o) => (
          <label key={o} className={CLASE_OPCION}>
            <input
              type="radio"
              name={props.nombre}
              checked={props.valor === o}
              onChange={() => props.onChange(o)}
              className="sr-only"
            />
            {o}
          </label>
        ))}
      </div>
    </fieldset>
  )
}

/** Selector de servicios del catálogo, compartido por el alta de la orden y el detalle (agregar servicio). */
export default function SelectorServicios({ servicios, valor, onChange }: Props) {
  function alternar(s: Servicio) {
    onChange(
      valor.some((v) => v.servicioId === s.id) ? valor.filter((v) => v.servicioId !== s.id) : [...valor, nuevaSeleccion(s)],
    )
  }
  function cambiar(id: string, parcial: Partial<Seleccion>) {
    onChange(valor.map((v) => (v.servicioId === id ? { ...v, ...parcial } : v)))
  }

  if (servicios.length === 0) return <p className="text-lg">No hay servicios disponibles para este vehículo.</p>

  return (
    <ul className="flex flex-col gap-3">
      {servicios.map((s) => {
        const sel = valor.find((v) => v.servicioId === s.id)
        return (
          <li key={s.id} className="rounded-lg border border-border">
            <label className="flex min-h-14 cursor-pointer items-center gap-3 px-4 text-lg font-medium">
              <input
                type="checkbox"
                checked={!!sel}
                onChange={() => alternar(s)}
                className="size-6 accent-primary"
              />
              {s.nombre}
            </label>
            {sel && (
              <div className="flex flex-col gap-4 border-t border-border p-4">
                {s.aplicaPosicion && (
                  <Opciones<Posicion>
                    nombre={`posicion-${s.id}`}
                    leyenda={`Posición de ${s.nombre}`}
                    opciones={['Delantero', 'Trasero']}
                    valor={sel.posicion}
                    onChange={(posicion) => cambiar(s.id, { posicion })}
                  />
                )}
                {s.aplicaLado && (
                  <Opciones<Lado>
                    nombre={`lado-${s.id}`}
                    leyenda={`Lado de ${s.nombre}`}
                    opciones={['Izquierda', 'Derecha']}
                    valor={sel.lado}
                    onChange={(lado) => cambiar(s.id, { lado })}
                  />
                )}
                <label className="flex flex-col gap-2 text-lg font-medium">
                  Precio de {s.nombre} ($)
                  <input
                    value={sel.precio}
                    onChange={(e) => cambiar(s.id, { precio: e.target.value })}
                    inputMode="decimal"
                    placeholder="A definir"
                    className={CLASE_CAMPO}
                  />
                </label>
              </div>
            )}
          </li>
        )
      })}
    </ul>
  )
}
