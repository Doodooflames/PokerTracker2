@echo off
setlocal enabledelayedexpansion

REM Create release script for PokerTracker2
REM Usage: create-release.bat [version] [release-notes]

if "%1"=="" (
    echo ❌ Please provide a version number
    echo Usage: create-release.bat [version] [release-notes]
    echo Example: create-release.bat 1.0.0 "Initial release"
    pause
    exit /b 1
)

set VERSION=%1
set RELEASE_NOTES=%2
set PROJECT_NAME=PokerTracker2
set GITHUB_REPO=Doodooflames/PokerTracker2
set PUBLISH_DIR=PublishSingle
set RELEASE_TAG=v%VERSION%

echo 🚀 Creating release %RELEASE_TAG% for %PROJECT_NAME%
echo ================================================

REM Step 1: Clean and build
echo 📦 Building project...
dotnet clean
if errorlevel 1 (
    echo ❌ Build failed!
    pause
    exit /b 1
)

dotnet build -c Release
if errorlevel 1 (
    echo ❌ Build failed!
    pause
    exit /b 1
)

REM Step 2: Publish
echo 📤 Publishing application...
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -p:PublishTrimmed=false -o %PUBLISH_DIR%
if errorlevel 1 (
    echo ❌ Publish failed!
    pause
    exit /b 1
)

REM Step 3: Create archive
echo 📁 Creating release archive...
set ARCHIVE_NAME=%PROJECT_NAME%-%VERSION%.zip
set ARCHIVE_PATH=%PUBLISH_DIR%\%ARCHIVE_NAME%

powershell -Command "Compress-Archive -Path '%PUBLISH_DIR%\*' -DestinationPath '%ARCHIVE_PATH%' -Force"
if not exist "%ARCHIVE_PATH%" (
    echo ❌ Failed to create archive!
    pause
    exit /b 1
)

echo ✅ Archive created: %ARCHIVE_NAME%

REM Step 4: Git operations
echo 🏷️ Creating Git tag...
git add .
git commit -m "Release %RELEASE_TAG%"
git tag -a %RELEASE_TAG% -m "Release %RELEASE_TAG%"
git push origin master
git push origin %RELEASE_TAG%

REM Step 5: Try to create GitHub release automatically
echo 🌐 Attempting to create GitHub release automatically...
set GH_PATH=

REM Try to find GitHub CLI
where gh >nul 2>&1
if %errorlevel% == 0 (
    set GH_PATH=gh
) else if exist "C:\Program Files\GitHub CLI\gh.exe" (
    set GH_PATH="C:\Program Files\GitHub CLI\gh.exe"
) else if exist "%LOCALAPPDATA%\GitHub CLI\gh.exe" (
    set GH_PATH="%LOCALAPPDATA%\GitHub CLI\gh.exe"
)

if defined GH_PATH (
    echo ✅ Found GitHub CLI at: %GH_PATH%
    echo 📝 Creating release notes file...
    echo %RELEASE_NOTES% > temp_notes.txt
    echo 🚀 Creating GitHub release...
    %GH_PATH% release create %RELEASE_TAG% %ARCHIVE_PATH% --title "Release %RELEASE_TAG%" --repo %GITHUB_REPO% --notes-file temp_notes.txt
    if %errorlevel% == 0 (
        echo ✅ GitHub release created successfully!
        del temp_notes.txt
    ) else (
        echo ⚠️ GitHub release creation failed. Please create manually:
        del temp_notes.txt
        goto :manual_release
    )
) else (
    echo ⚠️ GitHub CLI not found. Please create the release manually:
    goto :manual_release
)
goto :end

:manual_release
echo 🌐 Please create the GitHub release manually:
echo   1. Go to: https://github.com/%GITHUB_REPO%/releases/new
echo   2. Tag: %RELEASE_TAG%
echo   3. Title: Release %RELEASE_TAG%
echo   4. Upload: %ARCHIVE_PATH%
echo   5. Add release notes: %RELEASE_NOTES%
echo   6. Click "Publish release"

:end

echo.
echo 🎉 Release %RELEASE_TAG% prepared successfully!
echo 📁 Archive: %ARCHIVE_PATH%
echo 🌐 GitHub: https://github.com/%GITHUB_REPO%/releases/tag/%RELEASE_TAG%

pause
