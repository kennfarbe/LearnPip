FROM node:24.21.0-alpine3.24 AS build
WORKDIR /app
COPY frontend/web/package.json frontend/web/package-lock.json ./
RUN npm ci
COPY frontend/web/ ./
RUN npm run build

FROM nginx:1.30.5-alpine3.24 AS runtime
RUN apk add --no-cache "libexpat>=2.8.5-r0" "pcre2>=10.49-r0"
COPY frontend/web/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/learnpip-web/browser/ /usr/share/nginx/html/
EXPOSE 80
