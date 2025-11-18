# =============================================================================
# Author: Vladyslav Zaiets | https://sarmkadan.com
# CTO & Software Architect
# =============================================================================

FROM mcr.microsoft.com/dotnet/sdk:10 AS builder

WORKDIR /build

# Copy project files
COPY *.csproj ./
COPY . .

# Restore and build
RUN dotnet restore
RUN dotnet publish -c Release -o /app

# Runtime image
FROM mcr.microsoft.com/dotnet/runtime:10

WORKDIR /app

# Copy from builder
COPY --from=builder /app .

# Copy configuration
COPY appsettings.json .
COPY appsettings.local.json* ./

# Create data volumes
RUN mkdir -p /data/tasks /data/backups /var/log/notion-sync

# Health check
HEALTHCHECK --interval=5m --timeout=10s --retries=3 \
    CMD ["dotnet", "run", "--", "status"] || exit 1

# Non-root user for security
RUN useradd -m -u 1000 appuser && \
    chown -R appuser:appuser /app /data /var/log/notion-sync
USER appuser

# Default command
ENTRYPOINT ["dotnet", "NotionTaskSync.dll"]
CMD ["sync"]
