# Developer Guide

## Solution
- Solution: ProDispatch.slnx
- Library: src/ProDispatch/ProDispatch.csproj
- Sample: samples/ProDispatch.Examples.Console
- Tests: tests/ProDispatch.Tests and tests/ProDispatch.IntegrationTests
- Benchmarks: benchmarks/ProDispatch.Benchmarks

## Commands
- Restore: `dotnet restore ProDispatch.slnx`
- Build: `dotnet build ProDispatch.slnx -c Release`
- Test with coverage: `dotnet test ProDispatch.slnx -c Release /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura`
- Run sample: `dotnet run --project samples/ProDispatch.Examples.Console/ProDispatch.Examples.Console.csproj`
- Run benchmarks: `dotnet run -c Release --project benchmarks/ProDispatch.Benchmarks/ProDispatch.Benchmarks.csproj`

## Coding Notes
- Register pipeline behaviors outermost first; the dispatcher wraps them in reverse order.
- ValidationBehavior runs only when requests implement IValidatable.
- Notifications fan out to all handlers and are awaited together.
- Keep handlers asynchronous and honor CancellationToken.

## CI/CD
- CI build/test/Sonar: .github/workflows/ci.yml
- Code scanning: .github/workflows/codeql.yml
- Release + NuGet push: .github/workflows/release.yml (requires NUGET_API_KEY secret)

## Docs
- Getting started: docs/getting-started.md
- Advanced usage: docs/advanced-usage.md
- Performance: docs/performance.md
- Migration: docs/migration-from-mediatr.md
