# Changelog

## Unreleased
- Breaking: replace public service factories with `IServiceCollection.AddProDispatch`, assembly-scanned handlers, and `IServiceProvider`-based resolution.
- Add explicit closed and open generic pipeline behavior registration and a runtime dependency on Microsoft.Extensions.DependencyInjection.Abstractions.
- Migrate samples and benchmarks to native dependency injection.

## 0.1.0
- Initial library packaging
- Added sample console app, tests, benchmarks, and CI/CD workflows
- Documentation for getting started, advanced usage, performance, and migration
