# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG PROJECT=LinkedinBot.Console
WORKDIR /src

# Copy solution and ALL project files (better layer caching)
COPY LinkedinBot.sln .
COPY LinkedinBot.DTO/LinkedinBot.DTO.csproj LinkedinBot.DTO/
COPY LinkedinBot.Domain/LinkedinBot.Domain.csproj LinkedinBot.Domain/
COPY LinkedinBot.Infra.Interfaces/LinkedinBot.Infra.Interfaces.csproj LinkedinBot.Infra.Interfaces/
COPY LinkedinBot.Infra/LinkedinBot.Infra.csproj LinkedinBot.Infra/
COPY LinkedinBot.Application/LinkedinBot.Application.csproj LinkedinBot.Application/
COPY LinkedinBot.Console/LinkedinBot.Console.csproj LinkedinBot.Console/
COPY LinkedinBot.Worker/LinkedinBot.Worker.csproj LinkedinBot.Worker/

RUN dotnet restore

# Copy everything else and build
COPY . .
RUN dotnet publish ${PROJECT}/${PROJECT}.csproj -c Release -o /app/publish --no-restore

# Create entrypoint script for the target project
RUN echo "#!/bin/sh\nexec dotnet /app/${PROJECT}.dll \"\$@\"" > /app/publish/entrypoint.sh && \
    chmod +x /app/publish/entrypoint.sh

# Stage 2: Runtime with Playwright browsers
FROM mcr.microsoft.com/playwright/dotnet:v1.49.0-noble AS runtime
WORKDIR /app

# Install .NET 8.0 runtime (Playwright image may not include it)
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
        dotnet-runtime-8.0 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# Create directories for mounted volumes
RUN mkdir -p /app/user-data /app/logs

# Default: headless mode in Docker
ENV Browser__Headless=true
ENV Browser__Channel=chromium

ENTRYPOINT ["/app/entrypoint.sh"]
