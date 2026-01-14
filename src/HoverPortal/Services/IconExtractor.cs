// ============================================================================
// HoverPortal - System Icon Extractor Service
// 遵循 dev-rules-1: 使用 SafeHandle 管理图标资源
// 参考: FluentWPF IconHelper.cs
// ============================================================================

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace HoverPortal.Services;

/// <summary>
/// 系统文件图标提取服务
/// 使用 SHGetFileInfo 获取系统关联图标
/// </summary>
public static class IconExtractor
{
    // ===== 图标缓存 =====
    private const int MaxCacheSize = 50; // LRU 缓存大小限制
    private static readonly ConcurrentDictionary<string, ImageSource?> _iconCache = new();
    private static readonly Queue<string> _cacheOrder = new(); // LRU 淘汰顺序追踪
    private static readonly object _cacheLock = new();
    
    // ===== P/Invoke 常量 =====
    private const uint SHGFI_ICON = 0x100;
    private const uint SHGFI_LARGEICON = 0x0;
    private const uint SHGFI_SMALLICON = 0x1;
    private const uint SHGFI_USEFILEATTRIBUTES = 0x10;
    
    private const uint FILE_ATTRIBUTE_NORMAL = 0x80;
    private const uint FILE_ATTRIBUTE_DIRECTORY = 0x10;
    
    /// <summary>
    /// File types that have unique per-file icons (not extension-based)
    /// These files embed or reference specific icons that differ for each file
    /// </summary>
    private static readonly HashSet<string> UniqueIconExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".lnk",  // Windows shortcuts - each has its target's icon
        ".exe",  // Executables - each has embedded icon
        ".url",  // URL shortcuts - each may have favicon
        ".ico",  // Icon files - each is unique
        ".scr"   // Screen savers - each has embedded icon
    };
    
    // ===== P/Invoke 结构体 =====
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct SHFILEINFO
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    }
    
    // ===== P/Invoke 声明 =====
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SHGetFileInfo(
        string pszPath,
        uint dwFileAttributes,
        ref SHFILEINFO psfi,
        uint cbFileInfo,
        uint uFlags
    );
    
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr hIcon);
    
    /// <summary>
    /// 获取文件图标 (带缓存)
    /// </summary>
    /// <param name="path">文件或文件夹路径</param>
    /// <param name="isDirectory">是否为文件夹</param>
    /// <param name="largeIcon">是否获取大图标</param>
    public static ImageSource? GetIcon(string path, bool isDirectory = false, bool largeIcon = true)
    {
        // 使用扩展名作为缓存键 (相同扩展名图标相同)
        // 特殊文件类型（.lnk, .exe等）使用完整路径作为缓存键，因为每个文件有独立图标
        string cacheKey;
        if (isDirectory)
        {
            // 文件夹使用路径作为键，支持自定义图标 (如 desktop.ini)
            cacheKey = $"dir_{path}";
        }
        else
        {
            var ext = Path.GetExtension(path);
            if (UniqueIconExtensions.Contains(ext))
            {
                // 特殊文件类型使用完整路径作为缓存键
                cacheKey = $"unique_{path}";
            }
            else
            {
                // 普通文件类型使用扩展名作为缓存键 (e.g., .txt, .pdf)
                cacheKey = ext.ToLowerInvariant();
                if (string.IsNullOrEmpty(cacheKey))
                    cacheKey = ".file";
            }
        }
        
        cacheKey = $"{cacheKey}_{(largeIcon ? "L" : "S")}";
        
        // LRU 缓存管理
        if (_iconCache.TryGetValue(cacheKey, out var cachedIcon))
        {
            return cachedIcon;
        }
        
        var icon = ExtractIcon(path, isDirectory, largeIcon);
        
        lock (_cacheLock)
        {
            // 超出限制时移除最旧条目
            while (_iconCache.Count >= MaxCacheSize && _cacheOrder.Count > 0)
            {
                var oldestKey = _cacheOrder.Dequeue();
                _iconCache.TryRemove(oldestKey, out _);
            }
            
            if (_iconCache.TryAdd(cacheKey, icon))
            {
                _cacheOrder.Enqueue(cacheKey);
            }
        }
        
        return icon;
    }
    
    /// <summary>
    /// 从系统提取图标
    /// </summary>
    private static ImageSource? ExtractIcon(string path, bool isDirectory, bool largeIcon)
    {
        var shinfo = new SHFILEINFO();
        uint flags = SHGFI_ICON;
        flags |= largeIcon ? SHGFI_LARGEICON : SHGFI_SMALLICON;
        
        // 判断是否需要读取实际文件以获取其图标
        var ext = Path.GetExtension(path);
        bool needsActualFileIcon = isDirectory || UniqueIconExtensions.Contains(ext);
        
        // 仅对普通文件使用 SHGFI_USEFILEATTRIBUTES (更快但返回通用扩展名图标)
        // 文件夹、快捷方式和可执行文件需要读取实际文件图标
        if (!needsActualFileIcon)
        {
            flags |= SHGFI_USEFILEATTRIBUTES;
        }
        
        uint attributes = isDirectory ? FILE_ATTRIBUTE_DIRECTORY : FILE_ATTRIBUTE_NORMAL;
        
        IntPtr result = SHGetFileInfo(
            path,
            attributes,
            ref shinfo,
            (uint)Marshal.SizeOf(shinfo),
            flags
        );
        
        if (result == IntPtr.Zero || shinfo.hIcon == IntPtr.Zero)
        {
            return null;
        }
        
        try
        {
            // 将 HICON 转换为 WPF ImageSource
            var imageSource = Imaging.CreateBitmapSourceFromHIcon(
                shinfo.hIcon,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions()
            );
            
            // 冻结以提高跨线程性能
            imageSource.Freeze();
            
            return imageSource;
        }
        finally
        {
            // 释放图标句柄 (dev-rules-1: 确保资源释放)
            DestroyIcon(shinfo.hIcon);
        }
    }
    
    /// <summary>
    /// 清空图标缓存
    /// </summary>
    public static void ClearCache()
    {
        lock (_cacheLock)
        {
            _iconCache.Clear();
            _cacheOrder.Clear();
        }
    }
}
