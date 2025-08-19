param(
    [Parameter(Mandatory=$false)]
    [switch]$Build,
    [Parameter(Mandatory=$false)]
    [switch]$Run,
    [Parameter(Mandatory=$false)]
    [switch]$Clean
)

Write-Host "=== PokerTracker2 WinUI 3 Build Script ===" -ForegroundColor Green

if ($Clean) {
    Write-Host "Cleaning project..." -ForegroundColor Yellow
    dotnet clean PokerTracker2/PokerTracker2.csproj
    if (Test-Path "PokerTracker2/bin") {
        Remove-Item -Recurse -Force "PokerTracker2/bin"
    }
    if (Test-Path "PokerTracker2/obj") {
        Remove-Item -Recurse -Force "PokerTracker2/obj"
    }
    Write-Host "Clean completed!" -ForegroundColor Green
    exit 0
}

if ($Build) {
    Write-Host "Building PokerTracker2..." -ForegroundColor Yellow
    
    # Restore packages
    Write-Host "Restoring packages..." -ForegroundColor Cyan
    dotnet restore PokerTracker2/PokerTracker2.csproj
    
    # Build the project
    Write-Host "Building project..." -ForegroundColor Cyan
    dotnet build PokerTracker2/PokerTracker2.csproj --configuration Release --no-restore
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Build completed successfully!" -ForegroundColor Green
        Write-Host "Executable location: PokerTracker2/bin/Release/net8.0-windows/PokerTracker2.exe" -ForegroundColor Cyan
    } else {
        Write-Host "Build failed!" -ForegroundColor Red
        exit 1
    }
}

    if ($Run) {
        Write-Host "Running PokerTracker2..." -ForegroundColor Yellow

        $exePath = "PokerTracker2/bin/Release/net8.0-windows/PokerTracker2.exe"

        if (Test-Path $exePath) {
            Write-Host "Starting application..." -ForegroundColor Cyan
            & $exePath
            $exitCode = $LASTEXITCODE
            if ($exitCode -ne 0) {
                Write-Host "Application exited with code: $exitCode" -ForegroundColor Red
            }
        } else {
            Write-Host "Executable not found. Building first..." -ForegroundColor Yellow
            dotnet build PokerTracker2/PokerTracker2.csproj --configuration Release
            if (Test-Path $exePath) {
                Write-Host "Starting application..." -ForegroundColor Cyan
                & $exePath
                $exitCode = $LASTEXITCODE
                if ($exitCode -ne 0) {
                    Write-Host "Application exited with code: $exitCode" -ForegroundColor Red
                }
            } else {
                Write-Host "Failed to build or find executable!" -ForegroundColor Red
                exit 1
            }
        }
    }

if (-not $Build -and -not $Run -and -not $Clean) {
    Write-Host "Usage:" -ForegroundColor Yellow
    Write-Host "  .\build.ps1 -Build    # Build the project" -ForegroundColor White
    Write-Host "  .\build.ps1 -Run      # Run the application" -ForegroundColor White
    Write-Host "  .\build.ps1 -Clean    # Clean build artifacts" -ForegroundColor White
    Write-Host "  .\build.ps1 -Build -Run  # Build and run" -ForegroundColor White
} 