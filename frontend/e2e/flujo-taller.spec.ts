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
  // Cambiar el precio de un servicio ya cargado (RF-18): Rotación pasa de 9.000 a 10.000
  await page.getByRole('button', { name: /^Cambiar precio de Rotación/ }).click()
  await page.getByLabel('Nuevo precio de Rotación ($)').fill('10000')
  await page.getByRole('button', { name: 'Guardar precio' }).click()
  await expect(page.getByText('Total $ 28.000')).toBeVisible()
  await page.getByRole('button', { name: 'Pasar a Listo para retirar' }).click()
  await expect(page.getByText('Listo para retirar').first()).toBeVisible()

  // Finalizar siempre pide confirmación (RF-21): primero "No, volver", después "Sí, finalizar"
  await page.getByRole('button', { name: 'Finalizar' }).click()
  await expect(page.getByText(`¿Finalizar la orden de ${patente}?`)).toBeVisible()
  await page.getByRole('button', { name: 'No, volver' }).click()
  await expect(page.getByRole('button', { name: 'Finalizar' })).toBeVisible()
  await page.getByRole('button', { name: 'Finalizar' }).click()
  await page.getByRole('button', { name: 'Sí, finalizar' }).click()
  await expect(page.getByRole('link', { name: '+ Nuevo vehículo' })).toBeVisible()
  await expect(page.getByRole('link', { name: new RegExp(patente) })).toHaveCount(0)
  // Fase 4: la orden finalizada aparece en el historial, buscando por patente, con servicios y precios
  await page.getByRole('link', { name: 'Historial' }).click()
  await page.getByLabel('Buscar por patente').fill(patente.toLowerCase())
  await page.getByRole('button', { name: 'Buscar' }).click()
  const entrada = page.getByRole('link', { name: new RegExp(patente) })
  await expect(entrada).toContainText('Balanceo · Delantero · Izquierda')
  await expect(entrada).toContainText('Rotación')
  await expect(entrada).toContainText('$ 28.000')

  // Detalle histórico (solo lectura) y edición de los datos del vehículo
  await entrada.click()
  await expect(page.getByText('Ya no se puede modificar')).toBeVisible()
  await expect(page.getByRole('button', { name: 'Finalizar' })).toHaveCount(0)
  await page.getByRole('link', { name: 'Editar datos del vehículo' }).click()
  await page.getByLabel('Marca').fill('Chevrolet')
  await page.getByRole('button', { name: 'Guardar cambios' }).click()
  await expect(page.getByText('Chevrolet')).toBeVisible()
})
