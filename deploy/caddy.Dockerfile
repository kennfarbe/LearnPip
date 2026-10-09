# Rebuild the unchanged standard Caddy modules with the patched Go runtime.
FROM --platform=$BUILDPLATFORM golang:1.26.9-alpine3.24 AS build
ARG TARGETARCH
ENV GOTOOLCHAIN=local
WORKDIR /src
# A separate build module keeps Caddy's exact dependency version in binary/SBOM metadata.
RUN go mod init learnpip-caddy \
    && go get github.com/caddyserver/caddy/v2/cmd/caddy@v2.11.7 \
    && CGO_ENABLED=0 GOOS=linux GOARCH=$TARGETARCH go build -trimpath \
       -o /out/caddy github.com/caddyserver/caddy/v2/cmd/caddy

FROM caddy:2.11.7-alpine AS runtime
COPY --from=build /out/caddy /usr/bin/caddy
RUN caddy version && caddy list-modules | grep -q '^http.handlers.reverse_proxy$'
