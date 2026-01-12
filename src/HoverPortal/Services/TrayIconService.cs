// ============================================================================
// HoverPortal - System Tray Icon Service
// 系统托盘图标服务 - 实现后台静默运行
// ============================================================================

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Resources;
using Hardcodet.Wpf.TaskbarNotification;
using Application = System.Windows.Application;

namespace HoverPortal.Services;

/// <summary>
/// 系统托盘图标服务
/// 管理托盘图标、右键菜单和窗口显示/隐藏
/// </summary>
public class TrayIconService : IDisposable
{
    private TaskbarIcon? _taskbarIcon;
    private readonly Window _mainWindow;
    private bool _isExiting = false;
    
    /// <summary>
    /// 是否正在退出应用程序
    /// </summary>
    public bool IsExiting => _isExiting;
    
    /// <summary>
    /// 请求打开设置窗口
    /// </summary>
    public event Action? RequestOpenSettings;
    
    public TrayIconService(Window mainWindow)
    {
        _mainWindow = mainWindow;
        InitializeTrayIcon();
    }
    
    private void InitializeTrayIcon()
    {
        // 创建WPF风格的托盘图标
        _taskbarIcon = new TaskbarIcon
        {
            ToolTipText = "HoverPortal - 桌面悬浮预览"
        };
        
        // 加载自定义图标
        LoadCustomIcon();
        
        // 创建WPF风格右键菜单
        var contextMenu = CreateStyledContextMenu();
        _taskbarIcon.ContextMenu = contextMenu;
        
        // 双击托盘图标显示主窗口
        _taskbarIcon.TrayMouseDoubleClick += (s, e) => ShowMainWindow();
    }
    
    private void LoadCustomIcon()
    {
        try
        {
            // 从嵌入式资源加载PNG并转换为Icon
            var resourceUri = new Uri("pack://application:,,,/Resources/app_icon.png", UriKind.Absolute);
            var streamInfo = Application.GetResourceStream(resourceUri);
            
            if (streamInfo != null)
            {
                using var stream = streamInfo.Stream;
                using var bitmap = new System.Drawing.Bitmap(stream);
                
                // 调整图标大小为16x16（标准托盘图标尺寸）
                using var resized = new System.Drawing.Bitmap(bitmap, new System.Drawing.Size(16, 16));
                var hIcon = resized.GetHicon();
                _taskbarIcon!.Icon = System.Drawing.Icon.FromHandle(hIcon);
                return;
            }
        }
        catch (Exception)
        {
            // 忽略加载错误，使用备选图标
        }
        
        // 备选：使用程序关联图标
        try
        {
            var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
            if (!string.IsNullOrEmpty(exePath))
            {
                _taskbarIcon!.Icon = System.Drawing.Icon.ExtractAssociatedIcon(exePath);
                return;
            }
        }
        catch { }
        
        // 最终备选：默认系统图标
        _taskbarIcon!.Icon = System.Drawing.SystemIcons.Application;
    }
    
    private ContextMenu CreateStyledContextMenu()
    {
        var contextMenu = new ContextMenu();
        
        // 应用现代样式
        if (Application.Current.TryFindResource("TrayContextMenuStyle") is Style menuStyle)
        {
            contextMenu.Style = menuStyle;
        }
        
        // 显示主窗口 (粗体)
        var showItem = new MenuItem
        {
            Header = "显示主窗口",
            Icon = new TextBlock { Text = "🏠", FontSize = 14 }
        };
        if (Application.Current.TryFindResource("TrayMenuItemBoldStyle") is Style boldStyle)
        {
            showItem.Style = boldStyle;
        }
        showItem.Click += (s, e) => ShowMainWindow();
        contextMenu.Items.Add(showItem);
        
        // 分隔线
        var separator1 = new Separator();
        if (Application.Current.TryFindResource("TrayMenuSeparatorStyle") is Style separatorStyle)
        {
            separator1.Style = separatorStyle;
        }
        contextMenu.Items.Add(separator1);
        
        // 设置
        var settingsItem = new MenuItem
        {
            Header = "设置",
            Icon = new TextBlock { Text = "⚙️", FontSize = 14 }
        };
        if (Application.Current.TryFindResource("TrayMenuItemStyle") is Style itemStyle)
        {
            settingsItem.Style = itemStyle;
        }
        settingsItem.Click += (s, e) =>
        {
            ShowMainWindow();
            RequestOpenSettings?.Invoke();
        };
        contextMenu.Items.Add(settingsItem);
        
        // 分隔线
        var separator2 = new Separator();
        if (Application.Current.TryFindResource("TrayMenuSeparatorStyle") is Style sepStyle2)
        {
            separator2.Style = sepStyle2;
        }
        contextMenu.Items.Add(separator2);
        
        // 退出 (红色)
        var exitItem = new MenuItem
        {
            Header = "退出",
            Icon = new TextBlock { Text = "🚪", FontSize = 14 }
        };
        if (Application.Current.TryFindResource("TrayMenuItemDangerStyle") is Style dangerStyle)
        {
            exitItem.Style = dangerStyle;
        }
        exitItem.Click += (s, e) => ExitApplication();
        contextMenu.Items.Add(exitItem);
        
        return contextMenu;
    }
    
    /// <summary>
    /// 显示主窗口
    /// </summary>
    public void ShowMainWindow()
    {
        _mainWindow.Show();
        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
        _mainWindow.Focus();
    }
    
    /// <summary>
    /// 隐藏主窗口到托盘
    /// </summary>
    public void HideToTray()
    {
        _mainWindow.Hide();
    }
    
    /// <summary>
    /// 退出应用程序
    /// </summary>
    public void ExitApplication()
    {
        _isExiting = true;
        Dispose();
        Application.Current.Shutdown();
    }
    
    /// <summary>
    /// 显示托盘气泡通知
    /// </summary>
    public void ShowBalloonTip(string title, string text, BalloonIcon icon = BalloonIcon.Info, int timeout = 3000)
    {
        _taskbarIcon?.ShowBalloonTip(title, text, icon);
    }
    
    public void Dispose()
    {
        if (_taskbarIcon != null)
        {
            _taskbarIcon.Dispose();
            _taskbarIcon = null;
        }
    }
}
