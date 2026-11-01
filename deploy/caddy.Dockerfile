# Rebuild the unchanged standard Caddy modules with the patched Go runtime.
FROM --platform=$BUILDPLATFORM golang:1.26.10-alpine3.24 AS build
ARG TARGETARCH
ENV GOTOOLCHAIN=local
WORKDIR /src
# A separate build module keeps Caddy's exact dependency version in binary/SBOM metadata.
RUN go mod init learnpip-caddy \
    && go get github.com/caddyserver/caddy/v2/cmd/caddy@v2.11.7 \
    && go get golang.org/x/net@v0.60.0 \
    && go get github.com/go-jose/go-jose/v4@v4.1.6 \
    && CGO_ENABLED=0 GOOS=linux GOARCH=$TARGETARCH go build -trimpath \
       -o /out/caddy github.com/caddyserver/caddy/v2/cmd/caddy \
    && go version -m /out/caddy | grep -Eq 'golang.org/x/net[[:space:]]+v0[.]60[.]0[[:space:]]'

FROM caddy:2.11.7-alpine AS runtime
RUN apk upgrade --no-cache
COPY --from=build /out/caddy /usr/bin/caddy
RUN caddy version && caddy list-modules | grep -q '^http.handlers.reverse_proxy$'
