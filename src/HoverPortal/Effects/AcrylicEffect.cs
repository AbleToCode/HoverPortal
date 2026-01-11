// ============================================================================
// HoverPortal - Acrylic/Mica Background Effect
// 遵循 dev-rules-1: 检查 Windows 版本兼容性，提供优雅降级
// 参考: iNKORE.UI.WPF.Modern DWMAPI.cs
// ============================================================================

using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace HoverPortal.Effects;

/// <summary>
/// 系统背景类型 (Windows 11 22H2+)
/// </summary>
public enum SystemBackdropType
{
    Auto = 0,
    None = 1,
    MainWindow = 2,    // Mica
    TransientWindow = 3,  // Acrylic
    TabbedWindow = 4   // Tabbed
}

/// <summary>
/// 亚克力/Mica 毛玻璃效果助手类
/// 支持 Windows 10/11 版本兼容性处理
/// </summary>
public static class AcrylicHelper
{
    // ===== DWM API 常量 =====
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;
    private const int DWMWA_MICA_EFFECT = 1029;
    
    // ===== SetWindowCompositionAttribute 结构 (Win10) =====
    private enum AccentState
    {
        ACCENT_DISABLED = 0,
        ACCENT_ENABLE_GRADIENT = 1,
        ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
        ACCENT_ENABLE_BLURBEHIND = 3,
        ACCENT_ENABLE_ACRYLICBLURBEHIND = 4,
        ACCENT_INVALID_STATE = 5
    }
    
    [StructLayout(LayoutKind.Sequential)]
    private struct AccentPolicy
    {
        public AccentState AccentState;
        public int AccentFlags;
        public int GradientColor;
        public int AnimationId;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    private struct WindowCompositionAttributeData
    {
        public int Attribute;
        public IntPtr Data;
        public int SizeOfData;
    }
    
    // ===== P/Invoke =====
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
    
    [DllImport("user32.dll")]
    private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);
    
    /// <summary>
    /// 为窗口启用毛玻璃效果
    /// </summary>
    /// <param name="window">目标 WPF 窗口</param>
    /// <param name="useMica">使用 Mica (Win11) 或 Acrylic</param>
    /// <param name="isDarkMode">是否使用暗色模式</param>
    public static bool EnableBlur(Window window, bool useMica = false, bool isDarkMode = true)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero)
        {
            // 窗口尚未创建句柄，等待 SourceInitialized 事件
            return false;
        }
        
        // 设置窗口为透明背景
        window.Background = Brushes.Transparent;
        
        // 检查 Windows 版本
        var osVersion = Environment.OSVersion.Version;
        
        if (osVersion.Build >= 22621) // Windows 11 22H2+
        {
            return EnableWin11Backdrop(hwnd, useMica ? SystemBackdropType.MainWindow : SystemBackdropType.TransientWindow, isDarkMode);
        }
        else if (osVersion.Build >= 17763) // Windows 10 1809+
        {
            return EnableWin10Acrylic(hwnd, isDarkMode);
        }
        else
        {
            // 降级: 不支持系统级毛玻璃
            window.Background = new SolidColorBrush(isDarkMode 
                ? Color.FromArgb(230, 32, 32, 32) 
                : Color.FromArgb(230, 240, 240, 240));
            return false;
        }
    }
    
    /// <summary>
    /// Windows 11 22H2+ Mica/Acrylic 效果
    /// </summary>
    private static bool EnableWin11Backdrop(IntPtr hwnd, SystemBackdropType backdropType, bool isDarkMode)
    {
        try
        {
            // 启用暗色模式标题栏
            int darkMode = isDarkMode ? 1 : 0;
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));
            
            // 设置背景类型
            int backdrop = (int)backdropType;
            int result = DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref backdrop, sizeof(int));
            
            return result == 0; // S_OK
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// Windows 10 Acrylic Blur 效果
    /// </summary>
    private static bool EnableWin10Acrylic(IntPtr hwnd, bool isDarkMode)
    {
        try
        {
            // 创建 AccentPolicy
            var accent = new AccentPolicy
            {
                AccentState = AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND,
                // ARGB 颜色: 半透明暗色/亮色
                GradientColor = isDarkMode ? unchecked((int)0xC0202020) : unchecked((int)0xC0F0F0F0)
            };
            
            int accentSize = Marshal.SizeOf(accent);
            IntPtr accentPtr = Marshal.AllocHGlobal(accentSize);
            
            try
            {
                Marshal.StructureToPtr(accent, accentPtr, false);
                
                var data = new WindowCompositionAttributeData
                {
                    Attribute = 19, // WCA_ACCENT_POLICY
                    Data = accentPtr,
                    SizeOfData = accentSize
                };
                
                SetWindowCompositionAttribute(hwnd, ref data);
                return true;
            }
            finally
            {
                Marshal.FreeHGlobal(accentPtr);
            }
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// 禁用毛玻璃效果
    /// </summary>
    public static void DisableBlur(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero) return;
        
        var osVersion = Environment.OSVersion.Version;
        
        if (osVersion.Build >= 22621)
        {
            int backdrop = (int)SystemBackdropType.None;
            DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref backdrop, sizeof(int));
        }
    }
}
