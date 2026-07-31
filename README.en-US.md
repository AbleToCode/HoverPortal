

# 🚀 HoverPortal

**Desktop Folder Quick Preview Tool**

An elegant Windows desktop enhancement plugin that automatically expands and displays a preview of folder contents when you hover your mouse over desktop folder icons.

![Windows](https://img.shields.io/badge/Platform-Windows%2010%2F11-blue)
![.NET](https://img.shields.io/badge/.NET-7.0-purple)
![WPF](https://img.shields.io/badge/Framework-WPF-green)
![License](https://img.shields.io/badge/License-MIT-yellow)

---

## ✨ Features

### Core Features
- 🖱️ **Hover Preview** - Automatically expands and shows a preview when hovering over desktop folders
- 📁 **Quick File Access** - Click directly on files in the preview window to open them
- 🎨 **Apple-style UI** - Rounded cards, frosted glass effects, smooth animations
- ⚙️ **Rich Settings** - Adjustable hover delay, window opacity, and animation speed

### System Integration
- 🔔 **System Tray** - Minimizes to tray, runs silently in the background
- 🖥️ **Multi-Monitor Support** - Automatically adapts to multi-screen layouts
- 📐 **DPI Awareness** - Perfect support for high-DPI displays

---

## 📦 Installation

### System Requirements
- Windows 10 (1903+) / Windows 11
- .NET 7.0 Runtime

### Build from Source

```bash
# Clone repository
git clone https://github.com/YourUsername/HoverPortal.git
cd HoverPortal

# Build project
dotnet build src/HoverPortal/HoverPortal.csproj

# Run
dotnet run --project src/HoverPortal/HoverPortal.csproj
```

---

## 🎮 Usage

1. **Launch App** - Run HoverPortal
2. **Click "Start Monitoring"** - Begin listening for desktop icon hover
3. **Hover Preview** - Move your mouse over any folder icon on the desktop
4. **Click File** - Click directly on a file in the preview window to open it

### Window Controls
| Button | Function |
|--------|----------|
| ⚙️ | Open Settings |
| − | Minimize to taskbar |
| × | Hide to system tray |

### Tray Menu
- **Show Main Window** - Restore the main interface
- **Settings** - Open settings window
- **Exit** - Completely quit the app

---

## ⚙️ Settings Options

### General
- Auto-start on boot
- Minimize to tray
- Auto-check for updates

### Behavior
- **Hover Delay** (100ms - 1000ms)
- **Animation Presets** (Fast / Balanced / Smooth)
- Show in fullscreen mode

### Appearance
- **Corner Radius** (0 - 24px)
- **Window Opacity** (70% - 100%)
- Blur background effect (Acrylic/Mica)

---

## 🏗️ Technical Architecture

```
HoverPortal/
├── src/HoverPortal/
│   ├── Effects/        # Visual Effects (Acrylic)
│   ├── Interop/        # Win32 P/Invoke
│   ├── Models/         # Data Models
│   ├── Resources/      # XAML Resources
│   ├── Services/       # Core Services
│   │   ├── DesktopIconService.cs    # Desktop Icon Detection
│   │   ├── MouseHoverDetector.cs    # Mouse Hover Detection
│   │   ├── SettingsService.cs       # Settings Persistence
│   │   └── TrayIconService.cs       # System Tray
│   ├── ViewModels/     # MVVM ViewModels
│   └── Views/          # WPF Windows
└── doc/                # Documentation
```

### Core Technologies
- **WPF** - UI Framework
- **CommunityToolkit.Mvvm** - MVVM Toolkit
- **CsWin32** - Win32 API Source Generator
- **System.Windows.Forms** - System Tray Support

---

## 🔧 Development

### Prerequisites
- Visual Studio 2022 or VS Code
- .NET 7.0 SDK
- Windows 10/11 SDK

### Build & Debug

```bash
# Run in debug mode
dotnet run --project src/HoverPortal/HoverPortal.csproj

# Publish Release version
dotnet publish src/HoverPortal/HoverPortal.csproj -c Release
```

---

## 📄 License

This project is licensed under the [MIT License](LICENSE).

---

## 🙏 Acknowledgments

- [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) - MVVM Toolkit
- [CsWin32](https://github.com/microsoft/CsWin32) - Win32 P/Invoke Generator

---

<p align="center">
  Made with ❤️ for Windows Desktop Enhancement
</p>
