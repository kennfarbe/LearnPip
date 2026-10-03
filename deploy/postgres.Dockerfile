# Rebuild the unchanged upstream gosu source with a maintained Go standard library.
FROM --platform=$BUILDPLATFORM golang:1.26.8-alpine3.24 AS build
ARG TARGETARCH
RUN apk add --no-cache git
WORKDIR /src
RUN git init . \
    && git remote add origin https://github.com/tianon/gosu.git \
    && git fetch --depth 1 origin 6456aaa0f3c854d199d0f037f068eb97515b7513 \
    && git checkout --detach FETCH_HEAD
RUN CGO_ENABLED=0 GOOS=linux GOARCH=$TARGETARCH go build -trimpath -o /out/gosu .

FROM postgres:18.6-alpine3.24 AS runtime
COPY --from=build /out/gosu /usr/local/bin/gosu
RUN gosu --version && gosu nobody true
