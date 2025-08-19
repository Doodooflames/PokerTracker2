# 🚀 Release & Auto-Update Guide

This guide explains how to create releases and how the auto-update system works in PokerTracker2.

## **📋 Prerequisites**

### **Required Tools:**
- **Git** (already set up)
- **GitHub CLI** (optional, for automated releases)
- **7-Zip** (optional, for better compression)

### **GitHub CLI Installation:**
```bash
# Windows (using winget)
winget install GitHub.cli

# Or download from: https://cli.github.com/
```

## **🔧 Creating a Release**

### **Method 1: Using PowerShell Script (Recommended)**
```powershell
# Navigate to project root
cd C:\Users\domin\source\repos\PokerTracker2

# Create release with version and notes
.\scripts\create-release.ps1 -Version "1.0.1" -ReleaseNotes "Bug fixes and performance improvements"

# Create draft release
.\scripts\create-release.ps1 -Version "1.0.1" -ReleaseNotes "Bug fixes" -Draft
```

### **Method 2: Using Batch File**
```cmd
# Navigate to project root
cd C:\Users\domin\source\repos\PokerTracker2

# Create release
scripts\create-release.bat "1.0.1" "Bug fixes and performance improvements"
```

### **Method 3: Manual Process**
1. **Update Version in Project File:**
   ```xml
   <!-- In PokerTracker2.csproj -->
   <Version>1.0.1</Version>
   <AssemblyVersion>1.0.1.0</AssemblyVersion>
   <FileVersion>1.0.1.0</FileVersion>
   ```

2. **Build and Publish:**
   ```bash
   dotnet clean
   dotnet build -c Release
   dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -o PublishSingle
   ```

3. **Create Archive:**
   ```bash
   # Using PowerShell
   Compress-Archive -Path "PublishSingle\*" -DestinationPath "PublishSingle\PokerTracker2-1.0.1.zip" -Force
   ```

4. **Git Operations:**
   ```bash
   git add .
   git commit -m "Release v1.0.1"
   git tag -a v1.0.1 -m "Release v1.0.1"
   git push origin master
   git push origin v1.0.1
   ```

5. **Create GitHub Release:**
   - Go to: https://github.com/Doodooflames/PokerTracker2/releases/new
   - Tag: `v1.0.1`
   - Title: `Release v1.0.1`
   - Upload: `PokerTracker2-1.0.1.zip`
   - Add release notes
   - Click "Publish release"

## **🔄 Auto-Update System**

### **How It Works:**
1. **Update Check:** App queries GitHub API for latest release
2. **Version Comparison:** Compares current version with latest
3. **Download:** Downloads update if newer version available
4. **Installation:** Uses Squirrel.Windows to install update
5. **Restart:** App restarts with new version

### **Update Check Triggers:**
- **Manual:** User clicks "🔍 Check for Updates" button
- **Automatic:** Can be implemented to check on app startup

### **Update Flow:**
```
User clicks "Check for Updates"
    ↓
App queries GitHub API
    ↓
Version comparison
    ↓
If update available → Show UpdateDialog
    ↓
User clicks "Download & Install"
    ↓
Download update file
    ↓
Install using Squirrel.Windows
    ↓
Restart application
```

## **📁 Release File Structure**

### **Required Files:**
- **Executable:** `PokerTracker2.exe` (self-contained)
- **Dependencies:** All .NET runtime dependencies included
- **Archive:** `PokerTracker2-{version}.zip`

### **Publish Settings:**
```xml
<PublishSingleFile>true</PublishSingleFile>
<SelfContained>true</SelfContained>
<PublishTrimmed>true</PublishTrimmed>
<PublishReadyToRun>true</PublishReadyToRun>
<IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
```

## **🔒 Security Considerations**

### **GitHub Releases:**
- **Public repository:** Anyone can download updates
- **Private repository:** Only authenticated users can access
- **Release assets:** Automatically scanned for malware by GitHub

### **Update Verification:**
- **Source verification:** Updates come from official GitHub repository
- **Hash verification:** Can implement SHA256 checksums
- **Digital signatures:** Can add code signing for enterprise use

## **🚨 Troubleshooting**

### **Common Issues:**

#### **Build Fails:**
```bash
# Clean solution
dotnet clean
dotnet restore
dotnet build -c Release
```

#### **Publish Fails:**
```bash
# Check runtime identifier
dotnet --list-runtimes

# Use specific runtime
dotnet publish -r win-x64 --self-contained true
```

#### **Git Tag Issues:**
```bash
# Delete local tag
git tag -d v1.0.1

# Delete remote tag
git push origin --delete v1.0.1

# Recreate tag
git tag -a v1.0.1 -m "Release v1.0.1"
git push origin v1.0.1
```

#### **Auto-Update Not Working:**
- Check internet connection
- Verify GitHub API access
- Check app logs for errors
- Ensure release has executable asset

### **Debug Auto-Update:**
```csharp
// In MainWindow.xaml.cs
private async void CheckForUpdates_Click(object sender, RoutedEventArgs e)
{
    try
    {
        var updateInfo = await _autoUpdateService.CheckForUpdatesAsync();
        LoggingService.Instance.Info($"Update check result: {updateInfo?.IsUpdateAvailable}", "MainWindow");
        // ... rest of method
    }
    catch (Exception ex)
    {
        LoggingService.Instance.Error($"Update check failed: {ex}", "MainWindow");
    }
}
```

## **📈 Version Management**

### **Semantic Versioning:**
- **Major.Minor.Patch** (e.g., 1.2.3)
- **Major:** Breaking changes
- **Minor:** New features, backward compatible
- **Patch:** Bug fixes, backward compatible

### **Version Update Process:**
1. Update `PokerTracker2.csproj`
2. Update `AssemblyVersion` and `FileVersion`
3. Commit changes
4. Create release tag
5. Build and publish
6. Create GitHub release

### **Example Version Bump:**
```xml
<!-- Before -->
<Version>1.0.0</Version>
<AssemblyVersion>1.0.0.0</AssemblyVersion>
<FileVersion>1.0.0.0</FileVersion>

<!-- After -->
<Version>1.1.0</Version>
<AssemblyVersion>1.1.0.0</AssemblyVersion>
<FileVersion>1.1.0.0</FileVersion>
```

## **🎯 Best Practices**

### **Release Checklist:**
- [ ] Update version numbers
- [ ] Test build and publish
- [ ] Create meaningful release notes
- [ ] Tag release in Git
- [ ] Create GitHub release
- [ ] Verify auto-update works
- [ ] Test on clean machine

### **Release Notes Format:**
```markdown
## What's New
- Feature 1 description
- Feature 2 description

## Bug Fixes
- Fixed issue with session loading
- Resolved player profile update problem

## Improvements
- Better error handling
- Performance optimizations

## Breaking Changes
- None in this release
```

### **Testing Updates:**
1. **Clean Install:** Test on machine without previous version
2. **Update Path:** Test updating from previous version
3. **Rollback:** Ensure old version still works if needed
4. **Cross-Platform:** Test on different Windows versions

## **🔮 Future Enhancements**

### **Planned Features:**
- **Automatic update checks** on app startup
- **Update notifications** in system tray
- **Delta updates** for smaller downloads
- **Rollback functionality** to previous versions
- **Update scheduling** for non-intrusive updates

### **Advanced Features:**
- **Beta channel** for testing releases
- **Staged rollouts** to percentage of users
- **Update analytics** and telemetry
- **Enterprise deployment** tools

---

## **📞 Support**

If you encounter issues with releases or auto-updates:

1. **Check logs** in the app's debug console
2. **Review this guide** for troubleshooting steps
3. **Check GitHub Issues** for known problems
4. **Create new issue** with detailed error information

**Happy releasing! 🎉**
