// ============================================================================
// HoverPortal - System Tray Icon Service
// 系统托盘图标服务 - 实现后台静默运行
// ============================================================================

using System;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;

namespace HoverPortal.Services;

/// <summary>
/// 系统托盘图标服务
/// 管理托盘图标、右键菜单和窗口显示/隐藏
/// </summary>
public class TrayIconService : IDisposable
{
    private NotifyIcon? _notifyIcon;
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
        // 创建托盘图标
        _notifyIcon = new NotifyIcon
        {
            // 使用系统图标作为托盘图标
            Icon = SystemIcons.Application,
            Visible = true,
            Text = "HoverPortal - 桌面悬浮预览"
        };
        
        // 创建右键菜单
        var contextMenu = new ContextMenuStrip();
        
        // 显示主窗口
        var showItem = new ToolStripMenuItem("显示主窗口");
        showItem.Click += (s, e) => ShowMainWindow();
        showItem.Font = new Font(showItem.Font, System.Drawing.FontStyle.Bold);
        contextMenu.Items.Add(showItem);
        
        contextMenu.Items.Add(new ToolStripSeparator());
        
        // 设置
        var settingsItem = new ToolStripMenuItem("设置");
        settingsItem.Click += (s, e) => 
        {
            ShowMainWindow();
            RequestOpenSettings?.Invoke();
        };
        contextMenu.Items.Add(settingsItem);
        
        contextMenu.Items.Add(new ToolStripSeparator());
        
        // 退出
        var exitItem = new ToolStripMenuItem("退出");
        exitItem.Click += (s, e) => ExitApplication();
        contextMenu.Items.Add(exitItem);
        
        _notifyIcon.ContextMenuStrip = contextMenu;
        
        // 双击托盘图标显示主窗口
        _notifyIcon.DoubleClick += (s, e) => ShowMainWindow();
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
    public void ShowBalloonTip(string title, string text, ToolTipIcon icon = ToolTipIcon.Info, int timeout = 3000)
    {
        _notifyIcon?.ShowBalloonTip(timeout, title, text, icon);
    }
    
    public void Dispose()
    {
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }
    }
}
