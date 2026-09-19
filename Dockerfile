# Etapa 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar archivos de solución y proyecto
COPY SistemaTickets.sln .
COPY SistemaTickets/SistemaTickets.csproj SistemaTickets/
RUN dotnet restore SistemaTickets.sln

# Copiar todo el código y compilar
COPY . .
RUN dotnet publish SistemaTickets/SistemaTickets.csproj -c Release -o /app/publish

# Etapa 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Puerto expuesto
EXPOSE 8080

# Variable de entorno para escuchar en todas las interfaces
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "SistemaTickets.dll"]