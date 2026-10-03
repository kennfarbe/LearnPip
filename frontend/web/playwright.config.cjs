const { defineConfig } = require('@playwright/test');
module.exports = defineConfig({
  testDir: './tests/ui',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: 0,
  workers: 2,
  use: {
    launchOptions: process.env.PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH
      ? { executablePath: process.env.PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH, args: ['--disable-gpu'] }
      : {},
    baseURL: 'http://127.0.0.1:4173',
    serviceWorkers: 'block',
    trace: 'retain-on-failure',
  },
  webServer: {
    command: 'node tests/serve-app.cjs',
    url: 'http://127.0.0.1:4173',
    reuseExistingServer: !process.env.CI,
  },
});
