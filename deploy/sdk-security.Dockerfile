# Build-only SDK image validated with the same patch updates used by the API and worker build stages.
FROM mcr.microsoft.com/dotnet/sdk:10.0
RUN apt-get update \
    && apt-get upgrade -y \
    && rm -rf /var/lib/apt/lists/*
