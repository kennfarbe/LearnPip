# Custom Caddy builder with a provenance-pinned security backport for
# CVE-2026-116587 in the legacy go-jose/v3 transitive dependency.
FROM --platform=$BUILDPLATFORM golang:1.26.10-alpine3.24 AS build
ARG TARGETARCH
ENV GOTOOLCHAIN=local
RUN apk add --no-cache git python3
WORKDIR /src
COPY scripts/patch-go-jose-v3.py /tmp/patch-go-jose-v3.py
COPY security/go-jose-v3-backport_test.go /tmp/go-jose-v3-backport_test.go

# Pin the exact upstream tag commit; fail if upstream source changes.
RUN mkdir -p /src/go-jose-v3 \
    && cd /src/go-jose-v3 \
    && git init -q \
    && git remote add origin https://github.com/go-jose/go-jose.git \
    && git fetch --depth 1 origin be2f6548701d4ba561cd61c7d4a2f1289b66c273 \
    && git checkout --quiet --detach FETCH_HEAD \
    && test "$(git rev-parse HEAD)" = be2f6548701d4ba561cd61c7d4a2f1289b66c273 \
    && python3 /tmp/patch-go-jose-v3.py /src/go-jose-v3 \
    && cp /tmp/go-jose-v3-backport_test.go /src/go-jose-v3/learnpip_security_test.go \
    && gofmt -w jwk.go shared.go learnpip_security_test.go \
    && go test -run '^TestLearnPip' -count=1 .

# Use the patched source rather than the vulnerable upstream v3.0.5 module.
# This keeps the official module's API and the Caddy local-CA functionality.
RUN go mod init learnpip-caddy \
    && go mod edit -replace=github.com/go-jose/go-jose/v3=./go-jose-v3 \
    && go get github.com/caddyserver/caddy/v2/cmd/caddy@v2.11.7 \
    && go get golang.org/x/net@v0.60.0 \
    && go get github.com/go-jose/go-jose/v4@v4.1.7 \
    && CGO_ENABLED=0 GOOS=linux GOARCH=$TARGETARCH go build -trimpath \
       -o /out/caddy github.com/caddyserver/caddy/v2/cmd/caddy \
    && go version -m /out/caddy | grep -Eq 'golang.org/x/net[[:space:]]+v0[.]60[.]0[[:space:]]' \
    && go version -m /out/caddy | grep -F './go-jose-v3'

FROM caddy:2.11.7-alpine AS runtime
RUN apk upgrade --no-cache
COPY --from=build /out/caddy /usr/bin/caddy
RUN caddy version && caddy list-modules | grep -q '^http.handlers.reverse_proxy$'
