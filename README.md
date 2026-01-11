# 🚀 HoverPortal

**Desktop Folder Quick Preview Tool | 桌面文件夹快速预览工具**

一个优雅的 Windows 桌面增强插件，当你将鼠标悬停在桌面文件夹图标上时，自动展开并显示文件夹内容预览。

![Windows](https://img.shields.io/badge/Platform-Windows%2010%2F11-blue)
![.NET](https://img.shields.io/badge/.NET-7.0-purple)
![WPF](https://img.shields.io/badge/Framework-WPF-green)
![License](https://img.shields.io/badge/License-MIT-yellow)

---

## ✨ 功能特点

### 核心功能
- 🖱️ **悬停预览** - 鼠标悬停在桌面文件夹上自动展开预览
- 📁 **文件快速访问** - 直接点击预览窗口中的文件即可打开
- 🎨 **Apple 风格 UI** - 圆角卡片、毛玻璃效果、流畅动画
- ⚙️ **丰富设置** - 悬停延迟、窗口透明度、动画速度可调

### 系统集成
- 🔔 **系统托盘** - 最小化到托盘，后台静默运行
- 🖥️ **多显示器支持** - 自动适配多屏幕布局
- 📐 **DPI 感知** - 完美支持高分屏

---

## 📦 安装

### 系统要求
- Windows 10 (1903+) / Windows 11
- .NET 7.0 Runtime

### 从源码构建

```bash
# 克隆仓库
git clone https://github.com/YourUsername/HoverPortal.git
cd HoverPortal

# 构建项目
dotnet build src/HoverPortal/HoverPortal.csproj

# 运行
dotnet run --project src/HoverPortal/HoverPortal.csproj
```

---

## 🎮 使用方法

1. **启动应用** - 运行 HoverPortal
2. **点击「启动监控」** - 开始监听桌面图标悬停
3. **悬停预览** - 将鼠标移到桌面上的任意文件夹图标上
4. **点击文件** - 在预览窗口中直接点击文件即可打开

### 窗口控制
| 按钮 | 功能 |
|------|------|
| ⚙️ | 打开设置 |
| − | 最小化到任务栏 |
| × | 隐藏到系统托盘 |

### 托盘菜单
- **显示主窗口** - 恢复主界面
- **设置** - 打开设置窗口
- **退出** - 完全退出应用

---

## ⚙️ 设置选项

### 通用
- 开机自动启动
- 最小化到托盘
- 自动检查更新

### 行为
- **悬停延迟** (100ms - 1000ms)
- **动画预设** (快速 / 平衡 / 优雅)
- 全屏模式下显示

### 外观
- **圆角大小** (0 - 24px)
- **窗口透明度** (70% - 100%)
- 模糊背景效果 (Acrylic/Mica)

---

## 🏗️ 技术架构

```
HoverPortal/
├── src/HoverPortal/
│   ├── Effects/        # 视觉效果 (Acrylic)
│   ├── Interop/        # Win32 P/Invoke
│   ├── Models/         # 数据模型
│   ├── Resources/      # XAML 资源
│   ├── Services/       # 核心服务
│   │   ├── DesktopIconService.cs    # 桌面图标检测
│   │   ├── MouseHoverDetector.cs    # 鼠标悬停检测
│   │   ├── SettingsService.cs       # 设置持久化
│   │   └── TrayIconService.cs       # 系统托盘
│   ├── ViewModels/     # MVVM ViewModels
│   └── Views/          # WPF 窗口
└── doc/                # 文档
```

### 核心技术
- **WPF** - UI 框架
- **CommunityToolkit.Mvvm** - MVVM 工具包
- **CsWin32** - Win32 API 源生成器
- **System.Windows.Forms** - 系统托盘支持

---

## 🔧 开发

### 前置要求
- Visual Studio 2022 或 VS Code
- .NET 7.0 SDK
- Windows 10/11 SDK

### 构建调试

```bash
# 调试模式运行
dotnet run --project src/HoverPortal/HoverPortal.csproj

# 发布 Release 版本
dotnet publish src/HoverPortal/HoverPortal.csproj -c Release
```

---

## 📄 开源协议

本项目采用 [MIT License](LICENSE) 开源协议。

---

## 🙏 致谢

- [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) - MVVM 工具包
- [CsWin32](https://github.com/microsoft/CsWin32) - Win32 P/Invoke 生成器

---

<p align="center">
  Made with ❤️ for Windows Desktop Enhancement
</p>
