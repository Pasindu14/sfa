# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy and restore
COPY ["SFA.API/SFA.API.csproj", "SFA.API/"]
RUN dotnet restore "SFA.API/SFA.API.csproj"

# Copy everything and build
COPY . .
WORKDIR "/src/SFA.API"
RUN dotnet publish "SFA.API.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Render uses PORT environment variable
ENV ASPNETCORE_URLS=http://+:${PORT:-8080}
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "SFA.API.dll"]
```

---

**Step 2 — Add `.dockerignore` to project root:**
```
**/.classpath
**/.dockerignore
**/.git
**/.gitignore
**/.vs
**/.vscode
**/bin
**/obj
**/*.user