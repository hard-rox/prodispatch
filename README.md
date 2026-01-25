# ProDispatch

[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=ProDispatch&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=ProDispatch)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=ProDispatch&metric=coverage)](https://sonarcloud.io/summary/new_code?id=ProDispatch)

ProDispatch is a lightweight, in-process dispatcher inspired by MediatR. It supports commands, queries, notifications, and pluggable pipeline behaviors without requiring a heavy dependency injection container.

## Highlights
- Commands, queries, and notifications with simple interfaces
- Pipeline behaviors for logging, validation, and cross-cutting concerns
- Fan-out notifications with multiple handlers
- No external runtime dependencies; ships as a single library
- Samples, tests, benchmarks, and CI/CD ready for NuGet publishing

## Getting Started
1) Build the solution:
```
dotnet build ProDispatch.slnx
```
2) Run the sample console app:
```
dotnet run --project samples/ProDispatch.Examples.Console/ProDispatch.Examples.Console.csproj
```
3) Run tests:
```
dotnet test ProDispatch.slnx
```
4) Run benchmarks:
```
dotnet run -c Release --project benchmarks/ProDispatch.Benchmarks/ProDispatch.Benchmarks.csproj
```

## Project Layout
- Library: src/ProDispatch
- Sample: samples/ProDispatch.Examples.Console
- Tests: tests/ProDispatch.Tests and tests/ProDispatch.IntegrationTests
- Benchmarks: benchmarks/ProDispatch.Benchmarks
- Docs: docs (see getting-started, advanced-usage, performance, migration-from-mediatr)

## Usage
- Register handlers and behaviors with SimpleServiceFactory
- Create an InProcessDispatcher with the factory
- Use SendAsync for commands/queries and PublishAsync for notifications

See docs/getting-started.md for a full walkthrough and docs/advanced-usage.md for customization tips.

## Release and CI/CD
- GitHub Actions build, test, and Sonar analysis via .github/workflows/ci.yml
- Security scanning via .github/workflows/codeql.yml
- NuGet packaging/release automation via .github/workflows/release.yml

## Contributing
Please read CONTRIBUTING.md and CODE_OF_CONDUCT.md before opening issues or pull requests. Suggestions and improvements are welcome.
