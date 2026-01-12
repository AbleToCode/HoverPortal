// ============================================================================
// HoverPortal - Desktop Icon Detection Service
// 遵循 dev-rules-1: 
//   - 使用 SafeHandle 封装非托管资源
//   - 异步操作避免阻塞 UI 线程
//   - Hook 回调执行时间控制在 5ms 内
// 参考实现: ShareX/ShareX DesktopIconManager.cs
// ============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using HoverPortal.Interop;


namespace HoverPortal.Services;

/// <summary>
/// 桌面图标信息
/// </summary>
public sealed class DesktopIconInfo
{
    public int Index { get; init; }
    public string FilePath { get; init; } = string.Empty;
    public RECT Bounds { get; init; }
    public bool IsFolder { get; init; }
    
    public DesktopIconInfo(int index, string filePath, RECT bounds, bool isFolder)
    {
        Index = index;
        FilePath = filePath;
        Bounds = bounds;
        IsFolder = isFolder;
    }
}



/// <summary>
/// 桌面图标检测服务
/// 负责获取桌面图标位置和文件路径的映射关系
/// </summary>
public sealed class DesktopIconService : IDisposable
{
    // ===== 缓存数据 =====
    private readonly Dictionary<int, DesktopIconInfo> _iconCache = new();
    private IntPtr _listViewHandle;
    private bool _isDisposed;
    
    // ===== 桌面路径缓存 =====
    private readonly string _desktopPath;
    private readonly string _publicDesktopPath;
    private Dictionary<string, string> _desktopItems = new(); // name -> fullPath
    
    public DesktopIconService()
    {
        _desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        _publicDesktopPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory);
    }

    
    /// <summary>
    /// 异步刷新桌面图标缓存
    /// 遵循 dev-rules-1: 使用 Task.Run 避免阻塞 UI 线程
    /// </summary>
    public async Task<bool> RefreshIconCacheAsync()
    {
        return await Task.Run(() => RefreshIconCacheInternal()).ConfigureAwait(false);
    }
    
    /// <summary>
    /// 检查屏幕坐标是否在桌面 ListView 上
    /// 防止在其他窗口上时误触发悬浮框
    /// </summary>
    public bool IsPointOnDesktop(int screenX, int screenY)
    {
        if (!IsListViewValid()) return false;
        
        // 获取鼠标位置下的窗口
        var pt = new POINT { X = screenX, Y = screenY };
        IntPtr hwnd = NativeMethods.WindowFromPoint(pt);
        
        if (hwnd == IntPtr.Zero) return false;
        
        // 检查是否是桌面 ListView 本身
        if (hwnd == _listViewHandle) return true;
        
        // 检查窗口的根祖先是否是桌面 ListView
        // GA_ROOT = 2: 获取窗口的根祖先
        IntPtr root = NativeMethods.GetAncestor(hwnd, 2);
        return root == _listViewHandle;
    }
    
    /// <summary>
    /// 获取指定屏幕坐标下的文件夹图标
    /// </summary>
    public DesktopIconInfo? GetFolderIconAtPoint(int screenX, int screenY)
    {
        // 首先检查鼠标是否真的在桌面上
        if (!IsPointOnDesktop(screenX, screenY))
        {
            return null;
        }
        
        foreach (var kvp in _iconCache)
        {
            var icon = kvp.Value;
            if (icon.IsFolder && icon.Bounds.Contains(screenX, screenY))
            {
                return icon;
            }
        }
        return null;
    }
    
    /// <summary>
    /// 获取所有缓存的图标信息
    /// </summary>
    public IReadOnlyCollection<DesktopIconInfo> GetAllIcons()
    {
        return _iconCache.Values;
    }
    
    /// <summary>
    /// 验证 ListView 句柄是否仍然有效
    /// </summary>
    public bool IsListViewValid()
    {
        return _listViewHandle != IntPtr.Zero && NativeMethods.IsWindow(_listViewHandle);
    }
    
    // ===== 私有实现 =====
    
    private bool RefreshIconCacheInternal()
    {
        try
        {
            // 1. 获取桌面 ListView 句柄
            _listViewHandle = GetDesktopListViewHandle();
            if (_listViewHandle == IntPtr.Zero)
            {
                return false;
            }
            
            // 2. 获取图标数量
            int iconCount = (int)NativeMethods.SendMessage(
                _listViewHandle, 
                NativeMethods.LVM_GETITEMCOUNT, 
                IntPtr.Zero, 
                IntPtr.Zero
            );
            
            if (iconCount <= 0)
            {
                return false;
            }
            
            // 3. 获取 Explorer 进程 ID
            NativeMethods.GetWindowThreadProcessId(_listViewHandle, out uint explorerPid);
            
            // 4. 打开进程并读取图标位置
            using var processHandle = OpenExplorerProcess(explorerPid);
            if (processHandle == null || processHandle.IsInvalid)
            {
                return false;
            }
            
            // 5. 预先枚举桌面项目 (用于路径匹配)
            _desktopItems = EnumerateDesktopItems();
            
            // 6. 清空旧缓存并读取新数据
            _iconCache.Clear();

            
            for (int i = 0; i < iconCount; i++)
            {
                var iconInfo = ReadIconInfoFromRemoteProcess(processHandle.DangerousGetHandle(), i);
                if (iconInfo != null)
                {
                    _iconCache[i] = iconInfo;
                }
            }
            
            return _iconCache.Count > 0;
        }
        catch (Exception)
        {
            // 遵循 dev-rules-1: 异常处理，防止进程崩溃
            return false;
        }
    }
    
    /// <summary>
    /// 获取桌面 ListView 句柄
    /// 参考 ShareX 实现，支持 Win10/Win11 两种窗口层级
    /// </summary>
    private static IntPtr GetDesktopListViewHandle()
    {
        // 尝试从 Progman 获取
        IntPtr progman = NativeMethods.FindWindow("Progman", null);
        IntPtr defView = IntPtr.Zero;
        
        if (progman != IntPtr.Zero)
        {
            defView = NativeMethods.FindWindowEx(progman, IntPtr.Zero, "SHELLDLL_DefView", null);
        }
        
        // 如果失败，遍历所有 WorkerW (Windows 11 / Wallpaper Engine 兼容)
        if (defView == IntPtr.Zero)
        {
            IntPtr workerW = IntPtr.Zero;
            while ((workerW = NativeMethods.FindWindowEx(IntPtr.Zero, workerW, "WorkerW", null)) != IntPtr.Zero)
            {
                defView = NativeMethods.FindWindowEx(workerW, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (defView != IntPtr.Zero)
                {
                    break;
                }
            }
        }
        
        // 从 DefView 获取 SysListView32
        if (defView != IntPtr.Zero)
        {
            return NativeMethods.FindWindowEx(defView, IntPtr.Zero, "SysListView32", "FolderView");
        }
        
        return IntPtr.Zero;
    }
    
    /// <summary>
    /// 打开 Explorer 进程
    /// </summary>
    private static SafeProcessHandle? OpenExplorerProcess(uint processId)
    {
        IntPtr handle = NativeMethods.OpenProcess(
            NativeMethods.PROCESS_VM_OPERATION | 
            NativeMethods.PROCESS_VM_READ | 
            NativeMethods.PROCESS_VM_WRITE,
            false,
            processId
        );
        
        return handle != IntPtr.Zero ? new SafeProcessHandle(handle) : null;
    }
    
    /// <summary>
    /// 从远程进程读取单个图标的信息
    /// 使用跨进程内存操作获取 RECT 和文件路径
    /// </summary>
    private DesktopIconInfo? ReadIconInfoFromRemoteProcess(IntPtr processHandle, int index)
    {
        // 分配远程内存用于存储 RECT 结构
        int rectSize = Marshal.SizeOf<RECT>();
        IntPtr remoteRect = NativeMethods.VirtualAllocEx(
            processHandle,
            IntPtr.Zero,
            (uint)rectSize,
            NativeMethods.MEM_COMMIT | NativeMethods.MEM_RESERVE,
            NativeMethods.PAGE_READWRITE
        );
        
        if (remoteRect == IntPtr.Zero)
        {
            return null;
        }
        
        try
        {
            // 写入 left 值为 LVIR_BOUNDS，请求整个图标区域
            IntPtr localRect = Marshal.AllocHGlobal(rectSize);
            try
            {
                var rect = new RECT { Left = NativeMethods.LVIR_BOUNDS };
                Marshal.StructureToPtr(rect, localRect, false);
                
                NativeMethods.WriteProcessMemory(
                    processHandle,
                    remoteRect,
                    localRect,
                    (uint)rectSize,
                    out _
                );
                
                // 发送 LVM_GETITEMRECT 消息
                IntPtr result = NativeMethods.SendMessage(
                    _listViewHandle,
                    NativeMethods.LVM_GETITEMRECT,
                    (IntPtr)index,
                    remoteRect
                );
                
                if (result == IntPtr.Zero)
                {
                    return null;
                }
                
                // 读回 RECT 数据
                NativeMethods.ReadProcessMemory(
                    processHandle,
                    remoteRect,
                    localRect,
                    (uint)rectSize,
                    out _
                );
                
                var bounds = Marshal.PtrToStructure<RECT>(localRect);
                
                // 将客户区坐标转换为屏幕坐标
                var topLeft = new POINT { X = bounds.Left, Y = bounds.Top };
                var bottomRight = new POINT { X = bounds.Right, Y = bounds.Bottom };
                
                NativeMethods.ClientToScreen(_listViewHandle, ref topLeft);
                NativeMethods.ClientToScreen(_listViewHandle, ref bottomRight);
                
                var screenBounds = new RECT
                {
                    Left = topLeft.X,
                    Top = topLeft.Y,
                    Right = bottomRight.X,
                    Bottom = bottomRight.Y
                };
                
                // 获取图标名称
                string? iconName = ReadIconTextFromRemoteProcess(processHandle, index);
                
                if (string.IsNullOrEmpty(iconName))
                {
                    return null;
                }
                
                // 通过图标名称匹配桌面文件路径
                string filePath = string.Empty;
                bool isFolder = false;
                
                if (_desktopItems.TryGetValue(iconName, out string? path))
                {
                    filePath = path;
                    isFolder = Directory.Exists(filePath);
                }
                
                // 只返回有效路径
                if (!string.IsNullOrEmpty(filePath))
                {
                    return new DesktopIconInfo(index, filePath, screenBounds, isFolder);
                }
                
                return null;

            }
            finally
            {
                Marshal.FreeHGlobal(localRect);
            }
        }
        finally
        {
            // 确保释放远程内存 (遵循 dev-rules-1)
            NativeMethods.VirtualFreeEx(processHandle, remoteRect, 0, NativeMethods.MEM_RELEASE);
        }
    }
    
    /// <summary>
    /// 从远程进程读取图标文本名称
    /// 使用 LVM_GETITEMTEXT 消息
    /// </summary>
    private string? ReadIconTextFromRemoteProcess(IntPtr processHandle, int index)
    {
        const int MAX_TEXT_LENGTH = 260;
        
        // 计算需要分配的总内存大小 (LVITEM 结构 + 文本缓冲区)
        int lvitemSize = Marshal.SizeOf<LVITEM>();
        int totalSize = lvitemSize + MAX_TEXT_LENGTH * 2; // Unicode 字符 = 2 bytes
        
        IntPtr remoteMemory = NativeMethods.VirtualAllocEx(
            processHandle,
            IntPtr.Zero,
            (uint)totalSize,
            NativeMethods.MEM_COMMIT | NativeMethods.MEM_RESERVE,
            NativeMethods.PAGE_READWRITE
        );
        
        if (remoteMemory == IntPtr.Zero)
        {
            return null;
        }
        
        try
        {
            IntPtr remoteTextBuffer = remoteMemory + lvitemSize;
            
            // 准备 LVITEM 结构
            var lvitem = new LVITEM
            {
                mask = NativeMethods.LVIF_TEXT,
                iItem = index,
                iSubItem = 0,
                pszText = remoteTextBuffer,
                cchTextMax = MAX_TEXT_LENGTH
            };
            
            // 分配本地内存
            IntPtr localLvitem = Marshal.AllocHGlobal(lvitemSize);
            IntPtr localTextBuffer = Marshal.AllocHGlobal(MAX_TEXT_LENGTH * 2);
            
            try
            {
                // 写入 LVITEM 到远程进程
                Marshal.StructureToPtr(lvitem, localLvitem, false);
                NativeMethods.WriteProcessMemory(
                    processHandle,
                    remoteMemory,
                    localLvitem,
                    (uint)lvitemSize,
                    out _
                );
                
                // 发送 LVM_GETITEMTEXT 消息
                IntPtr textLength = NativeMethods.SendMessage(
                    _listViewHandle,
                    NativeMethods.LVM_GETITEMTEXTW,
                    (IntPtr)index,
                    remoteMemory
                );
                
                if (textLength == IntPtr.Zero)
                {
                    return null;
                }
                
                // 清零本地缓冲区，防止垃圾数据
                for (int i = 0; i < MAX_TEXT_LENGTH * 2; i++)
                {
                    Marshal.WriteByte(localTextBuffer, i, 0);
                }
                
                // 读取文本内容
                NativeMethods.ReadProcessMemory(
                    processHandle,
                    remoteTextBuffer,
                    localTextBuffer,
                    (uint)(MAX_TEXT_LENGTH * 2),
                    out _
                );
                
                // 转换为字符串
                string iconName = Marshal.PtrToStringUni(localTextBuffer) ?? string.Empty;
                
                // 调试输出
                System.Diagnostics.Debug.WriteLine($"[DesktopIconService] Icon {index}: '{iconName}'");
                
                return iconName;

            }
            finally
            {
                Marshal.FreeHGlobal(localLvitem);
                Marshal.FreeHGlobal(localTextBuffer);
            }
        }
        finally
        {
            NativeMethods.VirtualFreeEx(processHandle, remoteMemory, 0, NativeMethods.MEM_RELEASE);
        }
    }

    
    /// <summary>
    /// 枚举桌面上的所有项目
    /// 合并用户桌面和公共桌面
    /// 同时存储带扩展名和不带扩展名的键以匹配桌面图标显示名称
    /// </summary>
    private Dictionary<string, string> EnumerateDesktopItems()
    {
        var items = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        
        // 辅助方法：添加项目到字典（同时添加带扩展名和不带扩展名的版本）
        void AddItem(string fullPath)
        {
            var nameWithExt = Path.GetFileName(fullPath);
            var nameWithoutExt = Path.GetFileNameWithoutExtension(fullPath);
            
            if (!string.IsNullOrEmpty(nameWithExt) && !items.ContainsKey(nameWithExt))
            {
                items[nameWithExt] = fullPath;
            }
            
            // 同时添加不带扩展名的版本（桌面图标通常不显示扩展名）
            if (!string.IsNullOrEmpty(nameWithoutExt) && !items.ContainsKey(nameWithoutExt))
            {
                items[nameWithoutExt] = fullPath;
            }
        }
        
        // 枚举用户桌面
        try
        {
            foreach (var path in Directory.EnumerateFileSystemEntries(_desktopPath))
            {
                AddItem(path);
            }
        }
        catch { }
        
        // 枚举公共桌面
        try
        {
            foreach (var path in Directory.EnumerateFileSystemEntries(_publicDesktopPath))
            {
                AddItem(path);
            }
        }
        catch { }
        

        return items;
    }

    
    public void Dispose()
    {
        if (!_isDisposed)
        {
            _iconCache.Clear();
            _listViewHandle = IntPtr.Zero;
            _isDisposed = true;
        }
    }
}
