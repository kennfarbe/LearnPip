const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');
const root = path.resolve(__dirname, '../../..');
const gallery = path.join(root, 'docs/screenshots');
const names = ['overview', 'question-editor', 'catalogs', 'learning-mobile-dark', 'admin-password'];
const hash = (bytes) => crypto.createHash('sha256').update(bytes).digest('hex');
function files(relative) {
  const absolute = path.join(root, relative);
  if (!fs.statSync(absolute).isDirectory()) return [relative];
  return fs.readdirSync(absolute).sort().flatMap((name) => files(`${relative}/${name}`));
}
const inputs = [
  'frontend/web/src', 'frontend/web/public', 'frontend/web/angular.json',
  'frontend/web/package.json', 'frontend/web/package-lock.json',
  'frontend/web/playwright.config.cjs', 'frontend/web/playwright.docs.config.cjs',
  'frontend/web/tests/docs', 'frontend/web/tests/ui/fixtures.cjs',
  'frontend/web/tests/serve-app.cjs', 'frontend/web/tools/docs-screenshots.cjs',
].flatMap(files).sort();
const sourceHash = hash(Buffer.concat(inputs.flatMap((file) => [Buffer.from(`${file}\0`), fs.readFileSync(path.join(root, file))])));
function images() {
  return Object.fromEntries(names.map((name) => {
    const bytes = fs.readFileSync(path.join(gallery, `${name}.png`));
    if (!bytes.subarray(0, 8).equals(Buffer.from('89504e470d0a1a0a', 'hex'))) throw new Error(`Invalid PNG: ${name}`);
    const width = bytes.readUInt32BE(16);
    const height = bytes.readUInt32BE(20);
    if (width < 320 || height < 400) throw new Error(`Incomplete screenshot: ${name}`);
    return [`${name}.png`, { sha256: hash(bytes), width, height }];
  }));
}
const manifest = { sourceHash, images: images() };
const target = path.join(gallery, 'manifest.json');
if (process.argv.includes('--record')) {
  fs.writeFileSync(target, `${JSON.stringify(manifest, null, 2)}\n`);
  console.log('Documentation screenshots recorded.');
} else {
  if (JSON.stringify(JSON.parse(fs.readFileSync(target, 'utf8'))) !== JSON.stringify(manifest)) {
    throw new Error('Documentation screenshots are outdated. Run npm run docs:screenshots in frontend/web and commit the images and manifest.');
  }
  console.log('Documentation screenshots match the current UI sources.');
}
