# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["sfa_api/sfa_api/sfa_api.csproj", "sfa_api/sfa_api/"]
RUN dotnet restore "sfa_api/sfa_api/sfa_api.csproj"

COPY sfa_api/ sfa_api/
WORKDIR "/src/sfa_api/sfa_api"
RUN dotnet publish "sfa_api.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:${PORT:-8080}
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "sfa_api.dll"]
