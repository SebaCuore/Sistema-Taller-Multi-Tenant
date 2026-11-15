import { expect, test } from '@playwright/test'

const EMAIL = process.env.E2E_EMAIL ?? 'admin@ejemplo.com'
const PASSWORD = process.env.E2E_PASSWORD ?? 'clave-de-prueba-1'
const NUEVA = 'otra-clave-de-prueba-2'

async function ingresar(page: import('@playwright/test').Page, password: string) {
  await page.goto('/login')
  await page.getByLabel('Email').fill(EMAIL)
  await page.getByLabel('Contraseña').fill(password)
  await page.getByRole('button', { name: 'Ingresar' }).click()
  await expect(page.getByRole('link', { name: '+ Nuevo vehículo' })).toBeVisible()
}

async function cambiarPassword(page: import('@playwright/test').Page, actual: string, nueva: string) {
  await page.getByRole('link', { name: '⚙ Ajustes' }).click()
  await page.getByRole('button', { name: 'Cambiar contraseña' }).click()
  await page.getByLabel('Contraseña actual').fill(actual)
  await page.getByLabel(/Contraseña nueva/).fill(nueva)
  await page.getByLabel('Repetir contraseña nueva').fill(nueva)
  await page.getByRole('button', { name: 'Guardar contraseña' }).click()
  await expect(page.getByRole('status')).toContainText('Contraseña cambiada')
}

// Checkpoint de la Fase 6.5: cambiar la contraseña desde Ajustes, cerrar sesión y entrar con la nueva.
// Al final se vuelve a la contraseña original para no dejar la base de pruebas con otra clave.
test('seguridad: cambiar la contraseña, salir y entrar con la nueva', async ({ page }) => {
  await ingresar(page, PASSWORD)
  await cambiarPassword(page, PASSWORD, NUEVA)

  try {
    await page.getByRole('button', { name: 'Listo' }).click()
    await page.getByRole('button', { name: 'Cerrar sesión' }).click()
    await expect(page.getByRole('heading', { name: 'Ingresar al taller' })).toBeVisible()

    // La contraseña vieja ya no sirve
    await page.getByLabel('Email').fill(EMAIL)
    await page.getByLabel('Contraseña').fill(PASSWORD)
    await page.getByRole('button', { name: 'Ingresar' }).click()
    await expect(page.getByRole('alert')).toHaveText('Usuario o contraseña incorrectos')

    await ingresar(page, NUEVA)
  } finally {
    // Restaurar la contraseña original (si la sesión sigue abierta; si no, entrar con la nueva).
    if (!(await page.getByRole('link', { name: '⚙ Ajustes' }).isVisible())) await ingresar(page, NUEVA)
    await cambiarPassword(page, NUEVA, PASSWORD)
  }
})

// Checkpoint de la Fase 6.5: pasados los 10 intentos por minuto, el login avisa "demasiados intentos".
// El límite es por IP y cuenta también los logins de los otros E2E de la misma corrida, por eso se
// insiste hasta ver el aviso (como mucho 12 veces). Es el último test: correr con --workers=1 y no
// repetir la corrida antes de que pase un minuto.
test('seguridad: demasiados intentos de login muestran el aviso', async ({ page }) => {
  await page.goto('/login')
  const aviso = page.getByRole('alert')
  for (let i = 1; i <= 12; i++) {
    await page.getByLabel('Email').fill('nadie@ejemplo.com')
    await page.getByLabel('Contraseña').fill('incorrecta-' + i)
    await page.getByRole('button', { name: 'Ingresar' }).click()
    await expect(aviso).toBeVisible()
    if ((await aviso.textContent())?.startsWith('Demasiados intentos')) break
    await expect(aviso).toHaveText('Usuario o contraseña incorrectos')
  }
  await expect(aviso).toHaveText('Demasiados intentos. Esperá un minuto y probá de nuevo.')
})
