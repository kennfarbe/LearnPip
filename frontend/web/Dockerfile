FROM node:24-alpine AS build
WORKDIR /app
COPY frontend/web/package.json frontend/web/package-lock.json ./
RUN npm ci
COPY frontend/web/ ./
RUN npm run build

FROM nginx:1.29-alpine AS runtime
COPY frontend/web/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/learnpip-web/browser/ /usr/share/nginx/html/
EXPOSE 80
