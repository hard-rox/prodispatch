Param(
    [switch]$SkipTests
)

Write-Host "Restoring..." -ForegroundColor Cyan
& dotnet restore ..\ProDispatch.slnx

Write-Host "Building..." -ForegroundColor Cyan
& dotnet build ..\ProDispatch.slnx -c Release --no-restore

if (-not $SkipTests) {
    Write-Host "Testing..." -ForegroundColor Cyan
    & dotnet test ..\ProDispatch.slnx -c Release --no-build
}
