# Developer Guide

## Solution
- Solution file: **ProDispatch.slnx** (modern solution format)
- Library: **src/ProDispatch** with namespace `ProDispatch.*`
- Sample: **samples/ProDispatch.Examples.Console** (demonstrates library usage)
- Unit tests: **tests/ProDispatch.Tests** (xUnit)
- Integration tests: **tests/ProDispatch.IntegrationTests** (xUnit)
- Benchmarks: **benchmarks/ProDispatch.Benchmarks** (BenchmarkDotNet)

## Commands
```bash
# Restore dependencies
dotnet restore ProDispatch.slnx

# Build library and projects
dotnet build ProDispatch.slnx -c Release

# Run all tests
dotnet test ProDispatch.slnx -c Release

# Run tests with coverage report
dotnet test ProDispatch.slnx -c Release /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura

# Run the sample console app
dotnet run --project samples/ProDispatch.Examples.Console/ProDispatch.Examples.Console.csproj

# Run performance benchmarks
dotnet run -c Release --project benchmarks/ProDispatch.Benchmarks/ProDispatch.Benchmarks.csproj
```

## Project Structure

### Shared Props Files
- **Directory.Build.props**: Global MSBuild settings (nullable, implicit usings, warnings-as-errors in Release)
- **Directory.Packages.props**: Centralized package versioning for all projects
- **build/common.props**: Shared metadata (company, repository, license)

### GlobalUsings
Each project includes a `GlobalUsings.cs` file to reduce repetitive using declarations:
- **src/ProDispatch**: Core abstractions and implementations
- **samples/ProDispatch.Examples.Console**: Example handlers and patterns
- **tests/ProDispatch.Tests**: xUnit test utilities
- **tests/ProDispatch.IntegrationTests**: xUnit test utilities
- **benchmarks/ProDispatch.Benchmarks**: BenchmarkDotNet attributes

## Coding Notes
- Register pipeline behaviors outermost first; the dispatcher wraps them in reverse order (last registered runs innermost).
- ValidationBehavior runs only when requests implement IValidatable; validation is side-effect free.
- Notifications fan out to all handlers and are awaited together with Task.WhenAll.
- Keep handlers asynchronous and honor CancellationToken; use Task.Delay for demo purposes only.
- All public APIs require XML documentation; release builds treat warnings as errors.

## CI/CD
- CI build/test/Sonar: .github/workflows/ci.yml
- Code scanning: .github/workflows/codeql.yml
- Release + NuGet push: .github/workflows/release.yml (requires NUGET_API_KEY secret)

## Docs
- Getting started: docs/getting-started.md
- Advanced usage: docs/advanced-usage.md
- Performance: docs/performance.md
- Migration: docs/migration-from-mediatr.md
