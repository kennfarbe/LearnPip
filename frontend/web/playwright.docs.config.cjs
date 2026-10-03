const base = require('./playwright.config.cjs');
module.exports = {
  ...base,
  testDir: './tests/docs',
  fullyParallel: false,
  workers: 1,
  use: { ...base.use, viewport: { width: 1440, height: 1000 }, locale: 'de-DE', timezoneId: 'UTC', colorScheme: 'light' },
};
