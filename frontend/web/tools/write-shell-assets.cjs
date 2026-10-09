const { readdirSync, writeFileSync } = require('node:fs');
const { join } = require('node:path');

const directory = join(__dirname, '../dist/learnpip-web/browser');
const assets = readdirSync(directory)
  .filter((name) => /\.(?:js|css)$/.test(name))
  .sort();
const fonts = readdirSync(join(directory, 'media'))
  .filter((name) => /^KaTeX_[A-Za-z0-9_-]+\.woff2$/.test(name))
  .sort()
  .map((name) => `media/${name}`);
assets.push(...fonts);
writeFileSync(join(directory, 'shell-assets.json'), JSON.stringify(assets) + '\n');
