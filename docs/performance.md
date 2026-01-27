# Performance

## Benchmarks
Run benchmarks locally:
```
dotnet run -c Release --project benchmarks/ProDispatch.Benchmarks/ProDispatch.Benchmarks.csproj
```
The default suite measures dispatcher overhead with and without pipeline behaviors using BenchmarkDotNet.

## Tips
- Keep behaviors lightweight; heavy I/O should be asynchronous.
- Prefer struct-like DTOs for requests/results to reduce allocations.
- Cache reflection artifacts if you add custom resolvers.
- Use Release builds and ReadyToRun when shipping console hosts.

## Profiling
- dotnet-trace / dotnet-counters for quick sampling
- PerfView or dotTrace for deeper analysis
- Add metrics behavior to track per-request latency in production
