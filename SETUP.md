# PokerTracker2 C++/Slint Setup Guide

## Prerequisites

Before building the application, you need to install the following tools:

### 1. Visual Studio 2022
- Download from: https://visualstudio.microsoft.com/downloads/
- Install with the **"Desktop development with C++"** workload
- This includes the MSVC compiler and build tools

### 2. CMake
- Install via winget: `winget install Kitware.CMake`
- Or download from: https://cmake.org/download/

### 3. Git
- Download from: https://git-scm.com/download/win
- Required for vcpkg installation

## Quick Setup

The easiest way to set up everything is to use the provided build script:

```powershell
# Set up the development environment (installs vcpkg and Slint)
.\build.ps1 -Setup

# Build the project
.\build.ps1 -Build

# Run the application
.\build.ps1 -Run
```

## Manual Setup

If you prefer to set up manually:

### 1. Install vcpkg
```powershell
git clone https://github.com/Microsoft/vcpkg.git C:\vcpkg
C:\vcpkg\bootstrap-vcpkg.bat
```

### 2. Install Slint
```powershell
C:\vcpkg\vcpkg.exe install slint:x64-windows
```

### 3. Build the Project
```powershell
# Create build directory
mkdir build
cd build

# Configure with CMake
cmake .. -DCMAKE_TOOLCHAIN_FILE=C:/vcpkg/scripts/buildsystems/vcpkg.cmake -DCMAKE_BUILD_TYPE=Release

# Build
cmake --build . --config Release
```

### 4. Run the Application
```powershell
.\Release\PokerTracker2.exe
```

## Project Structure

```
PokerTracker2/
├── src/                    # C++ source files
│   ├── main.cpp           # Application entry point
│   ├── models/            # Data models (Session, Player, Transaction)
│   ├── database/          # Database layer (to be implemented)
│   ├── utils/             # Utility functions (to be implemented)
│   └── ui/                # UI components (to be implemented)
├── ui/                    # Slint UI definitions
│   └── main_window.slint  # Main UI layout
├── CMakeLists.txt         # CMake build configuration
├── vcpkg.json            # vcpkg dependencies
├── build.ps1             # Build script
└── SETUP.md              # This file
```

## Troubleshooting

### CMake not found
- Make sure CMake is installed and in your PATH
- Try restarting your terminal after installation

### Visual Studio not found
- Install Visual Studio 2022 with C++ workload
- Make sure you have the MSVC compiler installed

### vcpkg errors
- Make sure Git is installed
- Run vcpkg commands from an administrator PowerShell

### Build errors
- Make sure all dependencies are installed
- Try cleaning and rebuilding: `.\build.ps1 -Clean` then `.\build.ps1 -Build`

## Next Steps

After successfully building and running the application:

1. **Database Integration**: Implement SQLite3 C API for data persistence
2. **Business Logic**: Port financial calculations from Python
3. **UI Integration**: Connect C++ models to Slint UI components
4. **Testing**: Test on 4K displays for performance
5. **Polish**: Add remaining dialogs and functionality

## Performance Expectations

- **4K Monitors**: Should provide smooth 165Hz+ performance
- **GPU Acceleration**: Native GPU rendering through Slint
- **Memory Efficiency**: C++ provides better memory management
- **Responsiveness**: Immediate mode UI updates 