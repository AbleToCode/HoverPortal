// ============================================================================
// HoverPortal - Startup Manager Service
// 负责管理 Windows 开机自启动功能
// 遵循 dev-rules-1: 使用 Microsoft.Win32.Registry API
// ============================================================================

using System;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Win32;

namespace HoverPortal.Services;

/// <summary>
/// 管理 Windows 开机自启动功能
/// 通过注册表实现: HKCU\Software\Microsoft\Windows\CurrentVersion\Run
/// </summary>
public static class StartupManager
{
    private const string RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "HoverPortal";
    
    /// <summary>
    /// 设置开机自启动状态
    /// </summary>
    /// <param name="enabled">是否启用开机自启动</param>
    /// <returns>操作是否成功</returns>
    public static bool SetStartupEnabled(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, writable: true);
            if (key == null)
            {
                Debug.WriteLine("[StartupManager] Failed to open registry key");
                return false;
            }
            
            if (enabled)
            {
                // 获取当前可执行文件路径
                string exePath = GetExecutablePath();
                
                // 格式: "\"C:\path\to\HoverPortal.exe\" --startup"
                // 包含引号以处理路径中的空格
                string startupValue = $"\"{exePath}\" --startup";
                
                key.SetValue(AppName, startupValue, RegistryValueKind.String);
                Debug.WriteLine($"[StartupManager] Enabled startup: {startupValue}");
            }
            else
            {
                // 删除注册表项
                key.DeleteValue(AppName, throwOnMissingValue: false);
                Debug.WriteLine("[StartupManager] Disabled startup");
            }
            
            return true;
        }
        catch (System.Security.SecurityException ex)
        {
            Debug.WriteLine($"[StartupManager] Security exception: {ex.Message}");
            return false;
        }
        catch (UnauthorizedAccessException ex)
        {
            Debug.WriteLine($"[StartupManager] Unauthorized access: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[StartupManager] Error: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// 检查当前是否已启用开机自启动
    /// </summary>
    /// <returns>是否已启用</returns>
    public static bool IsStartupEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, writable: false);
            if (key == null)
            {
                return false;
            }
            
            var value = key.GetValue(AppName);
            return value != null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[StartupManager] Error checking startup status: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// 同步设置和注册表状态
    /// 用于应用启动时确保一致性
    /// </summary>
    /// <param name="settingValue">设置中的值</param>
    public static void SyncWithSettings(bool settingValue)
    {
        bool registryValue = IsStartupEnabled();
        
        if (settingValue != registryValue)
        {
            Debug.WriteLine($"[StartupManager] Syncing: setting={settingValue}, registry={registryValue}");
            SetStartupEnabled(settingValue);
        }
    }
    
    /// <summary>
    /// 获取当前可执行文件路径
    /// </summary>
    private static string GetExecutablePath()
    {
        // 优先使用进程路径（适用于发布后的应用）
        string? processPath = Environment.ProcessPath;
        if (!string.IsNullOrEmpty(processPath))
        {
            return processPath;
        }
        
        // 后备方案：使用入口程序集位置
        var assembly = Assembly.GetEntryAssembly();
        if (assembly != null)
        {
            return assembly.Location;
        }
        
        // 最后方案
        return Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
    }
}
