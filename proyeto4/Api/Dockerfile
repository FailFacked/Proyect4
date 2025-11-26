# Imagen base para compilar
# =========================
#   Build Stage
# =========================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia solución y proyectos
COPY *.sln .
COPY . .

# Restaurar dependencias (incluyendo .nuget local si lo usas)
RUN dotnet restore

# Compilar
RUN dotnet publish -c Release -o /app/publish

# =========================
#   Runtime Stage
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "TuProyecto.dll"]
