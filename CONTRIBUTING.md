# Contributing to Notion Task Sync

We love your input! We want to make contributing to this project as easy and transparent as possible.

## How to Contribute

### Fork, Clone, and Create a Branch

1. Fork the repository on GitHub.
2. Clone your fork locally:
   ```bash
   git clone https://github.com/your-username/notion-task-sync.git
   cd notion-task-sync
   ```
3. Create a branch for your feature or bugfix:
   ```bash
   git checkout -b feature/my-awesome-feature
   ```

### Development Requirements

- **.NET 10.0 SDK** or newer — [download here](https://dotnet.microsoft.com/download)

### Building Locally

```bash
# Restore NuGet packages
dotnet restore

# Build in Release configuration
dotnet build --configuration Release

# Or build in Debug for development
dotnet build
```

### Running Tests

We value testing. Make sure all existing tests pass and add new ones for your changes.

```bash
# Run all tests
dotnet test

# Run with verbose output
dotnet test --verbosity normal

# Run with TRX report (useful for CI)
dotnet test --logger "trx;LogFileName=test-results.trx"
```

Test results are written to `**/TestResults/` directories.

### Running Locally

```bash
dotnet run -- sync
dotnet run -- status
dotnet run -- --help
```

### Submitting a Pull Request (PR)

1. Commit your changes and push them to your fork.
2. Open a Pull Request against the `main` branch of this repository.
3. Ensure your PR description clearly describes the problem and solution.
4. All CI checks must pass before a PR can be merged.

## Code Style

- Follow the existing C# coding conventions used throughout the project.
- The `.editorconfig` at the root enforces indentation and formatting — most editors apply it automatically.
- Use XML documentation comments for public APIs, classes, and methods.
- **KEEP ALL author headers** - DO NOT remove them! If you edit a file that has an author header at the top, leave it completely intact.

## Issues and Bug Reports

We use GitHub Issues to track public bugs and feature requests. When reporting an issue, please include:
- A clear, descriptive title.
- Detailed reproduction steps so we can observe the issue.
- The expected behavior versus what actually happened.
- Relevant logs or stack traces.

## License

By contributing, you agree that your contributions will be licensed under the project's MIT License.