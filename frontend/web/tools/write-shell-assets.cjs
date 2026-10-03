const { readdirSync, writeFileSync } = require('node:fs');
const { join } = require('node:path');

const directory = join(__dirname, '../dist/learnpip-web/browser');
const assets = readdirSync(directory)
  .filter((name) => /\.(?:js|css)$/.test(name))
  .sort();
writeFileSync(join(directory, 'shell-assets.json'), JSON.stringify(assets) + '\n');
