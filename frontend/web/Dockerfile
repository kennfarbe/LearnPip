FROM node:24.21.0-alpine3.24 AS node-toolchain
# pnpm's native package manager replaces npm and its affected bundled libraries.
RUN npm install --global pnpm@12.9.1 --no-audit --no-fund \
    && rm -rf /usr/local/lib/node_modules/npm /usr/local/bin/npm /usr/local/bin/npx \
    && test "$(pnpm --version)" = 12.9.1

FROM node-toolchain AS build
WORKDIR /app
COPY frontend/web/package.json frontend/web/package-lock.json ./
RUN pnpm --config.pm-on-fail=ignore import \
    && pnpm --config.pm-on-fail=ignore install --frozen-lockfile --ignore-scripts
COPY frontend/web/ ./
RUN pnpm --config.pm-on-fail=ignore run build

FROM nginx:1.30.5-alpine3.24 AS runtime
RUN apk add --no-cache "libexpat>=2.8.5-r0" "pcre2>=10.49-r0"
COPY frontend/web/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/learnpip-web/browser/ /usr/share/nginx/html/
EXPOSE 80
