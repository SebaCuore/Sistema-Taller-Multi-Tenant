import { expect, test } from '@playwright/test'

const EMAIL = process.env.E2E_EMAIL ?? 'admin@ejemplo.com'
const PASSWORD = process.env.E2E_PASSWORD ?? 'clave-de-prueba-1'

// Checkpoint de la Fase 3: login → alta de vehículo → orden con dos servicios →
// "listo para retirar" → finalizar → la orden desaparece del tablero.
test('flujo completo del taller', async ({ page }) => {
  const patente = `E${Math.floor(10 + Math.random() * 89)}${Math.floor(100 + Math.random() * 899)}`

  await page.goto('/')
  await expect(page).toHaveURL(/\/login$/)
  await page.getByLabel('Email').fill(EMAIL)
  await page.getByLabel('Contraseña').fill(PASSWORD)
  await page.getByRole('button', { name: 'Ingresar' }).click()

  await page.getByRole('link', { name: '+ Nuevo vehículo' }).click()
  await page.getByLabel('Patente').fill(patente)
  await page.getByLabel('Marca').fill('Ford')
  await page.getByLabel('Nombre del dueño').fill('Cliente de prueba')
  await page.getByRole('button', { name: 'Guardar vehículo' }).click()

  // Paso 2: dos servicios
  await expect(page.getByRole('heading', { name: 'Servicios' })).toBeVisible()
  await page.getByRole('checkbox', { name: 'Balanceo' }).check()
  await page.getByRole('radio', { name: 'Delantero' }).check({ force: true })
  await page.getByRole('radio', { name: 'Izquierda' }).check({ force: true })
  await page.getByRole('checkbox', { name: 'Rotación' }).check()
  await page.getByLabel('Precio de Rotación ($)').fill('9000')
  await page.getByRole('button', { name: 'Crear orden' }).click()

  // La orden aparece en el tablero
  const tarjeta = page.getByRole('link', { name: new RegExp(patente) })
  await expect(tarjeta).toBeVisible()
  await expect(tarjeta).toContainText('2 servicios')
  await tarjeta.click()

  // Detalle: total = 18.000 (referencia de Balanceo) + 9.000 (editado a mano)
  await expect(page.getByText('Total $ 27.000')).toBeVisible()
  await page.getByRole('button', { name: 'Pasar a Listo para retirar' }).click()
  await expect(page.getByText('Listo para retirar').first()).toBeVisible()

  // Finalizar: vuelve al tablero y la orden ya no está
  await page.getByRole('button', { name: 'Finalizar' }).click()
  await expect(page.getByRole('link', { name: '+ Nuevo vehículo' })).toBeVisible()
  await expect(page.getByRole('link', { name: new RegExp(patente) })).toHaveCount(0)
})
