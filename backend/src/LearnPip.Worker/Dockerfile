FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY backend/src/LearnPip.Worker/LearnPip.Worker.csproj backend/src/LearnPip.Worker/
RUN dotnet restore backend/src/LearnPip.Worker/LearnPip.Worker.csproj
COPY backend/src/LearnPip.Worker/ backend/src/LearnPip.Worker/
RUN dotnet publish backend/src/LearnPip.Worker/LearnPip.Worker.csproj \
    --configuration Release --no-restore --output /app/publish

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "LearnPip.Worker.dll"]
