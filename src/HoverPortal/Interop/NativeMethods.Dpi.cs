// ============================================================================
// HoverPortal - NativeMethods DPI Extensions
// ============================================================================

using System;
using System.Runtime.InteropServices;

namespace HoverPortal.Interop;

internal static partial class NativeMethods
{
    // ===== Monitor Constants =====
    public const int MONITOR_DEFAULTTONULL = 0;
    public const int MONITOR_DEFAULTTOPRIMARY = 1;
    public const int MONITOR_DEFAULTTONEAREST = 2;

    // ===== DPI Types =====
    public enum MONITOR_DPI_TYPE
    {
        MDT_EFFECTIVE_DPI = 0,
        MDT_ANGULAR_DPI = 1,
        MDT_RAW_DPI = 2,
        MDT_DEFAULT = MDT_EFFECTIVE_DPI
    }

    // ===== P/Invoke Declarations =====

    [DllImport("user32.dll")]
    public static extern IntPtr MonitorFromPoint(POINT pt, int dwFlags);

    [DllImport("shcore.dll")]
    public static extern int GetDpiForMonitor(IntPtr hmonitor, MONITOR_DPI_TYPE dpiType, out uint dpiX, out uint dpiY);

    /// <summary>
    /// Helper to get DPI scale for a specific point on screen
    /// </summary>
    public static (double ScaleX, double ScaleY) GetDpiScaleForPoint(POINT pt)
    {
        try
        {
            // 1. Get monitor handle from point
            IntPtr hMonitor = MonitorFromPoint(pt, MONITOR_DEFAULTTONEAREST);
            
            if (hMonitor != IntPtr.Zero)
            {
                // 2. Get DPI for valid monitor
                uint dpiX, dpiY;
                int result = GetDpiForMonitor(hMonitor, MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI, out dpiX, out dpiY);
                
                if (result == 0) // S_OK
                {
                    return (dpiX / 96.0, dpiY / 96.0);
                }
            }
        }
        catch (Exception)
        {
            // Fallback to system default if Shcore functions fail (e.g. Win7)
            // or other interop issues occur
        }

        // Default fallback (100% scale)
        return (1.0, 1.0);
    }
}
