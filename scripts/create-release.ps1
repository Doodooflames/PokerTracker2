# PowerShell script to create a new release of PokerTracker2
# This script builds the project, publishes it, and creates a GitHub release

param(
    [Parameter(Mandatory=$true)]
    [string]$Version,
    
    [Parameter(Mandatory=$false)]
    [string]$ReleaseNotes = "",
    
    [Parameter(Mandatory=$false)]
    [switch]$Draft,
    
    [Parameter(Mandatory=$false)]
    [switch]$NoZip
)

# Configuration
$ProjectName = "PokerTracker2"
$GitHubRepo = "Doodooflames/PokerTracker2"
$PublishDir = "PublishSingle"
$ReleaseTag = "v$Version"

Write-Host "🚀 Creating release $ReleaseTag for $ProjectName" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green

# Step 1: Clean and build the project
Write-Host "📦 Building project..." -ForegroundColor Yellow
dotnet clean
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed!" -ForegroundColor Red
    exit 1
}

dotnet build -c Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed!" -ForegroundColor Red
    exit 1
}

# Step 2: Publish the application
Write-Host "📤 Publishing application..." -ForegroundColor Yellow
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -p:PublishTrimmed=false -o $PublishDir
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Publish failed!" -ForegroundColor Red
    exit 1
}

# Step 3: Create release archive (optional)
if ($NoZip) {
    Write-Host "🚫 Skipping zip creation - will upload EXE directly" -ForegroundColor Yellow
    $ArchivePath = Join-Path $PublishDir "PokerTracker2.exe"
    $UploadFile = $ArchivePath
} else {
    Write-Host "📁 Creating release archive..." -ForegroundColor Yellow
    $ArchiveName = "$ProjectName-$Version.zip"
    $ArchivePath = Join-Path $PublishDir $ArchiveName
    
    # Clean out old zip files before creating new archive
    Write-Host "🧹 Cleaning old zip files..." -ForegroundColor Yellow
    Get-ChildItem -Path $PublishDir -Filter "*.zip" | Remove-Item -Force
    
    # Create archive with only the executable
    Write-Host "📦 Creating archive with executable only..." -ForegroundColor Yellow
    if (Get-Command "7z" -ErrorAction SilentlyContinue) {
        7z a -tzip $ArchivePath "$PublishDir\*.exe"
    } else {
        Compress-Archive -Path "$PublishDir\*.exe" -DestinationPath $ArchivePath -Force
    }
    
    if (-not (Test-Path $ArchivePath)) {
        Write-Host "❌ Failed to create archive!" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "✅ Archive created: $ArchiveName" -ForegroundColor Green
    $UploadFile = $ArchivePath
}

# Step 4: Commit and tag the release
Write-Host "🏷️ Creating Git tag..." -ForegroundColor Yellow
git add .
git commit -m "Release $ReleaseTag"
git tag -a $ReleaseTag -m "Release $ReleaseTag"
git push origin master
git push origin $ReleaseTag

# Step 5: Create GitHub release using GitHub CLI
Write-Host "🌐 Creating GitHub release..." -ForegroundColor Yellow

# Try to find GitHub CLI in common locations
$GhPath = $null
if (Get-Command "gh" -ErrorAction SilentlyContinue) {
    $GhPath = "gh"
} elseif (Test-Path "C:\Program Files\GitHub CLI\gh.exe") {
    $GhPath = "C:\Program Files\GitHub CLI\gh.exe"
} elseif (Test-Path "$env:LOCALAPPDATA\GitHub CLI\gh.exe") {
    $GhPath = "$env:LOCALAPPDATA\GitHub CLI\gh.exe"
}

if ($GhPath) {
    $DraftFlag = if ($Draft) { "--draft" } else { "" }
    $NotesFlag = if ($ReleaseNotes) { "--notes '$ReleaseNotes'" } else { "" }
    
    $GhCommand = "& '$GhPath' release create $ReleaseTag $UploadFile $DraftFlag $NotesFlag --title 'Release $ReleaseTag' --repo $GitHubRepo"
    Write-Host "Running: $GhCommand" -ForegroundColor Cyan
    Invoke-Expression $GhCommand
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ GitHub release created successfully!" -ForegroundColor Green
    } else {
        Write-Host "⚠️ GitHub release creation failed. Please create manually:" -ForegroundColor Yellow
        Write-Host "   Go to: https://github.com/$GitHubRepo/releases/new" -ForegroundColor Cyan
        Write-Host "   Tag: $ReleaseTag" -ForegroundColor Cyan
        Write-Host "   Title: Release $ReleaseTag" -ForegroundColor Cyan
        Write-Host "   Upload: $ArchivePath" -ForegroundColor Cyan
    }
} else {
    Write-Host "⚠️ GitHub CLI not found. Please install it or create the release manually:" -ForegroundColor Yellow
    Write-Host "   Go to: https://github.com/$GitHubRepo/releases/new" -ForegroundColor Cyan
    Write-Host "   Tag: $ReleaseTag" -ForegroundColor Cyan
    Write-Host "   Title: Release $ReleaseTag" -ForegroundColor Cyan
    Write-Host "   Upload: $ArchivePath" -ForegroundColor Cyan
}

Write-Host "🎉 Release $ReleaseTag created successfully!" -ForegroundColor Green
if ($NoZip) {
    Write-Host "📁 Executable: $UploadFile" -ForegroundColor Cyan
} else {
    Write-Host "📁 Archive: $ArchivePath" -ForegroundColor Cyan
}
Write-Host "🌐 GitHub: https://github.com/$GitHubRepo/releases/tag/$ReleaseTag" -ForegroundColor Cyan
