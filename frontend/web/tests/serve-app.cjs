const http = require('node:http');
const fs = require('node:fs');
const path = require('node:path');
const root = path.resolve(__dirname, '../dist/learnpip-web/browser');
const types = {
  '.html': 'text/html',
  '.js': 'text/javascript',
  '.css': 'text/css',
  '.json': 'application/json',
  '.webmanifest': 'application/manifest+json',
  '.png': 'image/png',
  '.svg': 'image/svg+xml',
};
http
  .createServer((request, response) => {
    const pathname = new URL(request.url, 'http://localhost').pathname;
    const file = path.resolve(root, '.' + pathname);
    if (!file.startsWith(root + path.sep) && file !== root) {
      response.writeHead(403).end();
      return;
    }
    let target = file;
    if (!fs.existsSync(file) || !fs.statSync(file).isFile()) target = path.join(root, 'index.html');
    response.writeHead(200, {
      'Content-Type': types[path.extname(target)] || 'application/octet-stream',
    });
    fs.createReadStream(target).pipe(response);
  })
  .listen(4173, '127.0.0.1');
