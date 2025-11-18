# =============================================================================
# Author: Vladyslav Zaiets | https://sarmkadan.com
# CTO & Software Architect
# =============================================================================

.PHONY: help build test run clean docker push install

# Default target
.DEFAULT_GOAL := help

# Variables
PROJECT_NAME := notion-task-sync
DOCKER_REGISTRY := docker.io
DOCKER_IMAGE := $(PROJECT_NAME)
VERSION := $(shell git describe --tags --always 2>/dev/null || echo "0.0.1-dev")
BUILD_CONFIG := Release

help: ## Display this help screen
	@echo "Notion Task Sync - Makefile Commands"
	@echo "===================================="
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | sort | awk 'BEGIN {FS = ":.*?## "}; {printf "\033[36m%-20s\033[0m %s\n", $$1, $$2}'

build: ## Build the application
	@echo "Building $(PROJECT_NAME)..."
	dotnet clean -c $(BUILD_CONFIG) 2>/dev/null || true
	dotnet restore
	dotnet build -c $(BUILD_CONFIG) --no-restore

test: ## Run unit tests
	@echo "Running tests..."
	dotnet test --no-build -c $(BUILD_CONFIG) --logger "console;verbosity=minimal"

test-verbose: ## Run tests with detailed output
	@echo "Running tests (verbose)..."
	dotnet test --no-build -c $(BUILD_CONFIG) --logger "console;verbosity=detailed"

test-coverage: ## Run tests with coverage analysis
	@echo "Running tests with coverage..."
	dotnet test --no-build -c $(BUILD_CONFIG) /p:CollectCoverage=true /p:CoverageFormat=cobertura

publish: ## Publish release build
	@echo "Publishing $(PROJECT_NAME) v$(VERSION)..."
	dotnet publish -c $(BUILD_CONFIG) -o ./bin/publish

run: ## Run the application
	@echo "Running $(PROJECT_NAME)..."
	dotnet run -- sync

run-dev: ## Run in development mode
	@echo "Running in development mode..."
	ASPNETCORE_ENVIRONMENT=Development dotnet run -- sync --verbose

configure: ## Run configuration wizard
	@echo "Starting configuration..."
	dotnet run -- configure

status: ## Show sync status
	@echo "Checking status..."
	dotnet run -- status

history: ## Show sync history
	@echo "Showing sync history..."
	dotnet run -- history --limit 50

backup: ## Create a backup
	@echo "Creating backup..."
	dotnet run -- backup create --name "manual-backup-$$(date +%Y%m%d_%H%M%S)"

restore: ## Restore from backup (prompts for path)
	@echo "Restoring from backup..."
	dotnet run -- backup restore --path "./backups/$$(ls -t backups | head -1)"

clean: ## Clean build artifacts
	@echo "Cleaning build artifacts..."
	dotnet clean -c $(BUILD_CONFIG)
	rm -rf bin/ obj/
	find . -name "*.dll" -o -name "*.exe" | xargs rm -f

format: ## Format code with dotnet format
	@echo "Formatting code..."
	dotnet format

lint: ## Run code analysis
	@echo "Running code analysis..."
	dotnet build /p:EnforceCodeStyleInBuild=true

docker-build: ## Build Docker image
	@echo "Building Docker image: $(DOCKER_IMAGE):$(VERSION)"
	docker build -t $(DOCKER_IMAGE):$(VERSION) .
	docker tag $(DOCKER_IMAGE):$(VERSION) $(DOCKER_IMAGE):latest

docker-run: ## Run Docker container
	@echo "Running Docker container..."
	docker run -it --rm \
		-e NOTION_API_KEY=$${NOTION_API_KEY} \
		-e NOTION_DATABASE_ID=$${NOTION_DATABASE_ID} \
		-v $$(pwd)/tasks:/data/tasks \
		$(DOCKER_IMAGE):latest

docker-compose-up: ## Start Docker Compose stack
	@echo "Starting Docker Compose services..."
	docker-compose up -d

docker-compose-down: ## Stop Docker Compose stack
	@echo "Stopping Docker Compose services..."
	docker-compose down

docker-compose-logs: ## View Docker Compose logs
	@echo "Showing Docker Compose logs..."
	docker-compose logs -f notion-sync

docker-push: docker-build ## Push Docker image to registry
	@echo "Pushing to registry..."
	docker tag $(DOCKER_IMAGE):$(VERSION) $(DOCKER_REGISTRY)/$(DOCKER_IMAGE):$(VERSION)
	docker push $(DOCKER_REGISTRY)/$(DOCKER_IMAGE):$(VERSION)
	docker push $(DOCKER_REGISTRY)/$(DOCKER_IMAGE):latest

install-tools: ## Install required tools
	@echo "Installing required tools..."
	dotnet tool install -g dotnet-format
	dotnet tool install -g coverlet.console

deps: ## Check for outdated dependencies
	@echo "Checking for outdated dependencies..."
	dotnet list package --outdated

update-deps: ## Update all packages to latest versions
	@echo "Updating packages..."
	dotnet list package --outdated
	dotnet package update

install: publish ## Install as global tool
	@echo "Installing $(PROJECT_NAME) as global tool..."
	dotnet tool install -g --add-source ./bin/publish NotionTaskSync

uninstall: ## Uninstall global tool
	@echo "Uninstalling $(PROJECT_NAME)..."
	dotnet tool uninstall -g NotionTaskSync

release: test format ## Prepare release (test and format)
	@echo "Release preparation complete!"
	@echo "Changes to commit:"
	@git diff --name-only
	@echo ""
	@echo "Next steps:"
	@echo "  1. Review changes above"
	@echo "  2. Commit: git commit -m 'v$(VERSION)'"
	@echo "  3. Tag: git tag -a v$(VERSION) -m 'Release $(VERSION)'"
	@echo "  4. Push: git push && git push --tags"

version: ## Show version
	@echo "Version: $(VERSION)"

docs: ## Build documentation (if Sphinx is installed)
	@echo "Building documentation..."
	cd docs && make html 2>/dev/null || echo "Sphinx not installed"

all: clean build test ## Build and run all tests
	@echo "✓ All checks passed!"

.PHONY: all help build test run clean docker push
