# syntax=docker/dockerfile:1.7

# ---------- build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution + central package management first for better layer caching
COPY Directory.Build.props Directory.Packages.props global.json ./
COPY RagSystem.sln ./

# Copy project files so `dotnet restore` gets a cacheable layer
COPY src/RagSystem.Domain/RagSystem.Domain.csproj src/RagSystem.Domain/
COPY src/RagSystem.Application/RagSystem.Application.csproj src/RagSystem.Application/
COPY src/RagSystem.Infrastructure/RagSystem.Infrastructure.csproj src/RagSystem.Infrastructure/
COPY src/RagSystem.Api/RagSystem.Api.csproj src/RagSystem.Api/
RUN dotnet restore src/RagSystem.Api/RagSystem.Api.csproj

# Copy the rest of the source and publish
COPY src/ src/
RUN dotnet publish src/RagSystem.Api/RagSystem.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ---------- runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "RagSystem.Api.dll"]
