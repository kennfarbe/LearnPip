FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY backend/src/LearnPip.Worker/LearnPip.Worker.csproj backend/src/LearnPip.Worker/
COPY backend/src/LearnPip.Data/LearnPip.Data.csproj backend/src/LearnPip.Data/
RUN dotnet restore backend/src/LearnPip.Worker/LearnPip.Worker.csproj
COPY backend/src/LearnPip.Worker/ backend/src/LearnPip.Worker/
COPY backend/src/LearnPip.Data/ backend/src/LearnPip.Data/
RUN dotnet publish backend/src/LearnPip.Worker/LearnPip.Worker.csproj \
    --configuration Release --no-restore --output /app/publish

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "LearnPip.Worker.dll"]
