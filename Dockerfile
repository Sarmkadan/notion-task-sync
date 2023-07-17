# =============================================================================
# Author: Vladyslav Zaiets | https://sarmkadan.com
# CTO & Software Architect
# =============================================================================

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine3.24 AS builder

WORKDIR /build

# Copy project files
COPY *.csproj ./
COPY . .

# Restore and build
RUN dotnet restore
RUN dotnet publish -c Release -o /app

# Runtime image
FROM mcr.microsoft.com/dotnet/runtime:10.0-alpine3.24

WORKDIR /app

# Copy from builder
COPY --from=builder /app .

# Copy configuration
COPY appsettings.json .
COPY appsettings.local.json* ./

# Create data volumes
RUN mkdir -p /data/tasks /data/backups /var/log/notion-sync

# Health check - check if the app binary exists
HEALTHCHECK --interval=5m --timeout=10s --retries=3 \
CMD ["sh", "-c", "test -f /app/NotionTaskSync.dll && echo 'Healthy' || exit 1"]

# Non-root user for security
RUN adduser -D -u 1000 appuser && \
    chown -R appuser:appuser /app /data /var/log/notion-sync
USER appuser

# Default command - run sync by default
ENTRYPOINT ["dotnet", "NotionTaskSync.dll"]
CMD ["sync"]