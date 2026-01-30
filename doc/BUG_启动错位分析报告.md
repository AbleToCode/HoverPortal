# Bug 分析报告：开机自启动悬浮框错位

## 1. 问题现象
- **场景**: 应用程序开机自启动 (Silent Mode)。
- **表现**: 鼠标悬停文件夹时，悬浮框显示内容正确，但**位置出现严重偏差**（如图所示，偏离原图标数行）。
- **恢复**: 再次点击程序/刷新后恢复正常。

## 2. 根本原因 (Root Cause)

问题的核心在于 **DPI 缩放因子的获取方式** 与 **窗口可见性状态** 之间的冲突。

### 代码位置
**文件**: `src/HoverPortal/MainWindow.xaml.cs`
**代码段**: Line 158-160
```csharp
var source = PresentationSource.FromVisual(this);
double dpiScaleX = source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
double dpiScaleY = source?.CompositionTarget?.TransformToDevice.M22 ?? 1.0;
```

### 技术分析
1.  **隐形启动**: 当应用开机自启时，调用了 `StartSilently()`。此方法初始化服务并未显示 `MainWindow`（主窗口保持隐藏/最小化到托盘）。
2.  **Visual 连接失效**: 在 WPF 中，`PresentationSource.FromVisual(this)` 依赖于 Visual 对象（此处为 `MainWindow`）被完全加载并连接到渲染源（HwndSource）。
3.  **默认值回退**:由于 `MainWindow` 处于隐藏状态，从未真正显示过，`PresentationSource.FromVisual(this)` 极大概率返回 `null`。
4.  **错误计算**: 代码中的空合并运算符 `?? 1.0` 生效。导致逻辑强制使用 **1.0 (100%)** 的缩放比例进行计算。
5.  **DPI 不一致**:
    - 如果用户的显示器是高分屏（例如 150% 或 200% 缩放）。
    - 逻辑计算认为缩放是 100%。
    - **结果**: 计算出的 `Left/Top` 坐标是以 100% DPI 为基准的。
    - 当 `_previewWindow.ShowWithAnimation(...)` 执行时，新窗口在真实的 150% DPI 屏幕上渲染，WPF 会自动将传入的逻辑坐标再次缩放，或者由于坐标系基准不同导致偏移。
    - **公式误差**: `实际位置 = 目标位置 * (真实DPI / 1.0)`。这种倍率放大会导致图标越靠右/下，偏移量越大。但也可能导致基础坐标系映射错误。

### 为什么刷新后正常？
当用户手动点击托盘图标或进行交互，或者 `MainWindow` 在某些时刻被正确初始化/短暂显示，`PresentationSource` 重新获取到了正确的 HwndSource，从而读取到了正确的 DPI 设置（如 1.25 或 1.5），位置计算随之恢复正常。

## 3. 验证推论
从提供的截图看，偏移是向右下角偏移，这符合“低估了DPI（使用了1.0）但屏幕实际是高DPI”的特征。在高分屏上，逻辑像素与物理像素的转换差异导致了悬浮窗被绘制到了错误的位置。

## 4. 修复建议 (无需代码修改，仅供参考)
在计算坐标时，不应依赖隐藏的主窗口 (`MainWindow`) 来获取 DPI。建议：
1.  **使用 Win32 API**: `GetDpiForMonitor` 或 `GetDpiForWindow` 直接获取鼠标所在屏幕的 DPI。
2.  **使用 PreviewWindow**: `_previewWindow` 是即将显示的窗口，可以使用它的句柄获取 DPI（需确保它已创建句柄）。
3.  **系统级 DPI**: 读取 `VisualTreeHelper.GetDpi(this)` 可能会有同样问题，建议使用 `SystemParameters` 或 Monitor 相关的互操作 API。
