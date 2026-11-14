import { defineConfig } from '@playwright/test'

// E2E contra el sistema real: necesita Postgres, la API (http://localhost:5108) y un admin creado con
// `dotnet run -- crear-admin`. Credenciales por E2E_EMAIL / E2E_PASSWORD. Vite lo levanta Playwright.
// PLAYWRIGHT_CHROMIUM_PATH permite usar un Chromium ya instalado en vez del que descarga Playwright.
// E2E_BASE_URL apunta los tests a otro despliegue (por ejemplo el ensayo de producción en
// http://localhost:8080); en ese caso no se levanta Vite.
const baseURL = process.env.E2E_BASE_URL

export default defineConfig({
  testDir: './e2e',
  timeout: 60_000,
  retries: 0,
  use: {
    baseURL: baseURL ?? 'http://localhost:5173',
    viewport: { width: 390, height: 844 },
    launchOptions: { executablePath: process.env.PLAYWRIGHT_CHROMIUM_PATH || undefined },
  },
  webServer: baseURL
    ? undefined
    : {
        command: 'npm run dev',
        url: 'http://localhost:5173',
        reuseExistingServer: true,
      },
})
