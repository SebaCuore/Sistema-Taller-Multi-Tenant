import { expect, test, type Page } from '@playwright/test'

const EMAIL = process.env.E2E_EMAIL ?? 'admin@ejemplo.com'
const PASSWORD = process.env.E2E_PASSWORD ?? 'clave-de-prueba-1'
const sufijo = () => `${Date.now()}`.slice(-6)

async function ingresar(page: Page) {
  await page.goto('/login')
  await page.getByLabel('Email').fill(EMAIL)
  await page.getByLabel('Contraseña').fill(PASSWORD)
  await page.getByRole('button', { name: 'Ingresar' }).click()
  await expect(page.getByRole('link', { name: '+ Nuevo vehículo' })).toBeVisible()
}

async function nuevoVehiculoHastaServicios(page: Page, patente: string) {
  await page.goto('/vehiculos/nuevo')
  await page.getByLabel('Patente').fill(patente)
  await page.getByRole('button', { name: 'Guardar vehículo' }).click()
  await expect(page.getByRole('heading', { name: 'Servicios' })).toBeVisible()
}

// Checkpoint de la Fase 5: un servicio desactivado deja de ofrecerse en órdenes nuevas,
// pero sigue visible en las órdenes que ya lo usaban.
test('catálogo: agregar un servicio propio, usarlo y desactivarlo', async ({ page }) => {
  const servicio = `Servicio E2E ${sufijo()}`
  const patente = `C${sufijo()}`
  await ingresar(page)

  await page.getByRole('link', { name: '⚙ Ajustes' }).click()
  await page.getByRole('button', { name: '+ Agregar servicio' }).click()
  await page.getByLabel('Nombre').fill(servicio)
  await page.getByLabel(/Precio de referencia \(\$\), opcional/).fill('12345')
  await page.getByRole('button', { name: 'Guardar servicio' }).click()
  await expect(page.getByRole('button', { name: `Desactivar ${servicio}` })).toBeVisible()

  // Orden que usa el servicio nuevo, con su precio de referencia precargado
  await nuevoVehiculoHastaServicios(page, patente)
  await page.getByRole('checkbox', { name: servicio }).check()
  await expect(page.getByLabel(`Precio de ${servicio} ($)`)).toHaveValue('12345')
  await page.getByRole('button', { name: 'Crear orden' }).click()
  await expect(page.getByRole('link', { name: new RegExp(patente) })).toBeVisible()

  // Se desactiva
  await page.goto('/ajustes')
  await page.getByRole('button', { name: `Desactivar ${servicio}` }).click()
  await expect(page.getByRole('button', { name: `Activar ${servicio}` })).toBeVisible()

  // Ya no aparece en el selector de una orden nueva...
  await nuevoVehiculoHastaServicios(page, `D${sufijo()}`)
  await expect(page.getByRole('checkbox', { name: 'Balanceo' })).toBeVisible()
  await expect(page.getByRole('checkbox', { name: servicio })).toHaveCount(0)

  // ...pero la orden vieja lo sigue mostrando
  await page.goto('/')
  await page.getByRole('link', { name: new RegExp(patente) }).click()
  await expect(page.getByText(servicio).first()).toBeVisible()
  await expect(page.getByText('Total $ 12.345')).toBeVisible()
})

// Checkpoint de la Fase 6: cargar un producto, venderlo hasta dejarlo en stock negativo,
// y que la venta se registre igual con el aviso, no con un error.
test('stock y ventas: vender hasta stock negativo avisa pero no bloquea', async ({ page }) => {
  const producto = `Producto E2E ${sufijo()}`
  await ingresar(page)

  await page.getByRole('link', { name: 'Stock' }).click()
  await page.getByRole('button', { name: '+ Agregar producto' }).click()
  await page.getByLabel('Nombre').fill(producto)
  await page.getByLabel('Precio ($)').fill('1500')
  await page.getByLabel('Cantidad en stock').fill('1')
  await page.getByRole('button', { name: 'Guardar producto' }).click()
  await expect(page.getByText(producto).first()).toBeVisible()

  // Primera venta: hay stock, sin aviso
  await page.getByRole('link', { name: 'Ventas', exact: true }).click()
  await page.getByRole('button', { name: '+ Nueva venta' }).click()
  await page.getByLabel('Buscar producto').fill(producto)
  await page.getByRole('button', { name: new RegExp(producto) }).click()
  await expect(page.getByLabel(/Precio por unidad/)).toHaveValue('1500')
  await page.getByRole('button', { name: 'Confirmar venta' }).click()
  await expect(page.getByText('Venta registrada')).toBeVisible()
  await expect(page.getByText(/no había stock suficiente/)).toHaveCount(0)

  // Segunda venta: stock en 0, pide 2 con precio cambiado a mano → aviso suave, se registra igual
  await page.getByRole('button', { name: '+ Nueva venta' }).click()
  await page.getByLabel('Buscar producto').fill(producto)
  await page.getByRole('button', { name: new RegExp(producto) }).click()
  await expect(page.getByText('Este producto no tiene stock cargado. Podés venderlo igual.')).toBeVisible()
  await page.getByLabel('Cantidad').fill('2')
  await page.getByLabel(/Precio por unidad/).fill('1400')
  await expect(page.getByText('Total $ 2.800')).toBeVisible()
  await page.getByRole('button', { name: 'Confirmar venta' }).click()
  await expect(page.getByText('Venta registrada')).toBeVisible()
  await expect(page.getByText(/quedó registrada igual y el stock quedó en -2/)).toBeVisible()
  await expect(page.getByRole('alert')).toHaveCount(0)

  // El stock quedó negativo y las dos ventas están en el historial con el precio de cada momento
  await page.getByRole('link', { name: 'Stock' }).click()
  const tarjeta = page.getByRole('listitem').filter({ hasText: producto })
  await expect(tarjeta).toContainText('-2')
  await page.getByRole('link', { name: 'Historial' }).click()
  await page.getByRole('link', { name: 'Ventas', exact: true }).first().click()
  const ventas = page.getByRole('listitem').filter({ hasText: producto })
  await expect(ventas).toHaveCount(2)
  await expect(ventas.first()).toContainText('2 × $ 1.400')
  await expect(ventas.last()).toContainText('1 × $ 1.500')

  // Baja del producto (con confirmación): sale del stock, las ventas quedan
  await page.getByRole('link', { name: 'Stock' }).click()
  await page.getByRole('button', { name: `Eliminar ${producto}` }).click()
  await page.getByRole('button', { name: 'Sí, eliminar' }).click()
  await expect(page.getByText(producto)).toHaveCount(0)
  await page.goto('/historial/ventas')
  await expect(page.getByRole('listitem').filter({ hasText: producto })).toHaveCount(2)
})
