// ============================================================================
// HoverPortal - SafeHandles for Win32 Interop
// 遵循 dev-rules-1: 使用 SafeHandle 包装所有非托管资源
// ============================================================================

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace HoverPortal.Interop;

/// <summary>
/// ListView Item 结构体 - 用于 LVM_GETITEMTEXT
/// </summary>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct LVITEM
{
    public uint mask;
    public int iItem;
    public int iSubItem;
    public uint state;
    public uint stateMask;
    public IntPtr pszText;
    public int cchTextMax;
    public int iImage;
    public IntPtr lParam;
    public int iIndent;
    public int iGroupId;
    public uint cColumns;
    public IntPtr puColumns;
}

/// <summary>
/// 安全封装的进程句柄，确保 OpenProcess 返回的句柄被正确释放
/// </summary>

public sealed class SafeProcessHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public SafeProcessHandle() : base(true) { }

    public SafeProcessHandle(IntPtr handle, bool ownsHandle = true) : base(ownsHandle)
    {
        SetHandle(handle);
    }

    protected override bool ReleaseHandle()
    {
        return NativeMethods.CloseHandle(handle);
    }
}

/// <summary>
/// 安全封装的远程进程内存分配，确保 VirtualAllocEx 分配的内存被正确释放
/// </summary>
public sealed class SafeRemoteMemoryHandle : SafeHandle
{
    private readonly IntPtr _processHandle;
    
    public SafeRemoteMemoryHandle(IntPtr processHandle) : base(IntPtr.Zero, true)
    {
        _processHandle = processHandle;
    }

    public override bool IsInvalid => handle == IntPtr.Zero;

    public void SetRemoteMemory(IntPtr remoteMemory)
    {
        SetHandle(remoteMemory);
    }

    protected override bool ReleaseHandle()
    {
        if (handle == IntPtr.Zero || _processHandle == IntPtr.Zero)
            return true;
            
        return NativeMethods.VirtualFreeEx(_processHandle, handle, 0, NativeMethods.MEM_RELEASE);
    }
}

/// <summary>
/// Windows API 常量和 P/Invoke 声明
/// 使用 CsWin32 生成的签名作为参考，手动添加必要的补充
/// </summary>
internal static partial class NativeMethods
{
    // ===== 内存操作常量 =====
    public const uint MEM_COMMIT = 0x1000;
    public const uint MEM_RESERVE = 0x2000;
    public const uint MEM_RELEASE = 0x8000;
    public const uint PAGE_READWRITE = 0x04;
    
    // ===== 进程访问权限 =====
    public const uint PROCESS_VM_OPERATION = 0x0008;
    public const uint PROCESS_VM_READ = 0x0010;
    public const uint PROCESS_VM_WRITE = 0x0020;
    public const uint PROCESS_QUERY_INFORMATION = 0x0400;
    
    // ===== ListView 消息 =====
    public const int LVM_FIRST = 0x1000;
    public const int LVM_GETITEMCOUNT = LVM_FIRST + 4;
    public const int LVM_GETITEMRECT = LVM_FIRST + 14;
    public const int LVM_GETITEMPOSITION = LVM_FIRST + 16;
    public const int LVM_GETITEMW = LVM_FIRST + 75;
    public const int LVM_GETITEMTEXTW = LVM_FIRST + 115; // 获取项目文本 (Unicode)
    
    // ===== LVITEM 掩码 =====
    public const int LVIF_TEXT = 0x0001;
    
    // ===== RECT 边界类型 =====
    public const int LVIR_BOUNDS = 0;
    public const int LVIR_ICON = 1;
    public const int LVIR_LABEL = 2;

    
    // ===== P/Invoke 声明 =====
    
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);
    
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, 
        string? lpszClass, string? lpszWindow);
    
    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsWindow(IntPtr hWnd);
    
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsWindowVisible(IntPtr hWnd);
    
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetCursorPos(out POINT lpPoint);
    
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);
    
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

    
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr OpenProcess(uint dwDesiredAccess, 
        [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, uint dwProcessId);
    
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CloseHandle(IntPtr hObject);
    
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, 
        uint dwSize, uint flAllocationType, uint flProtect);
    
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, 
        uint dwSize, uint dwFreeType);
    
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, 
        IntPtr lpBuffer, uint nSize, out int lpNumberOfBytesRead);
    
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, 
        IntPtr lpBuffer, uint nSize, out int lpNumberOfBytesWritten);
    
    // ===== 结构体定义 (仅供内部 P/Invoke 使用) =====
    
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct LVITEMW
    {
        public uint mask;
        public int iItem;
        public int iSubItem;
        public uint state;
        public uint stateMask;
        public IntPtr pszText;
        public int cchTextMax;
        public int iImage;
        public IntPtr lParam;
    }
}

// ===== 公共结构体定义 (可被其他程序集使用) =====

/// <summary>
/// 表示屏幕坐标点
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct POINT
{
    public int X;
    public int Y;
}

/// <summary>
/// 表示矩形区域
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct RECT
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;
    
    public readonly int Width => Right - Left;
    public readonly int Height => Bottom - Top;
    
    public readonly bool Contains(int x, int y)
    {
        return x >= Left && x < Right && y >= Top && y < Bottom;
    }
}

