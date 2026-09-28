FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY backend/src/LearnPip.Api/LearnPip.Api.csproj backend/src/LearnPip.Api/
RUN dotnet restore backend/src/LearnPip.Api/LearnPip.Api.csproj
COPY backend/src/LearnPip.Api/ backend/src/LearnPip.Api/
RUN dotnet publish backend/src/LearnPip.Api/LearnPip.Api.csproj \
    --configuration Release --no-restore --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "LearnPip.Api.dll"]
