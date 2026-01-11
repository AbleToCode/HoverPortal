// ============================================================================
// HoverPortal - Main ViewModel
// 遵循 dev-rules-1: MVVM 架构，View 与 Logic 严格分离
// ============================================================================

using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HoverPortal.Interop;
using HoverPortal.Services;
using HoverPortal.Views;

namespace HoverPortal.ViewModels;

/// <summary>
/// 主窗口 ViewModel
/// 协调各服务模块，管理应用程序状态
/// </summary>
public partial class MainViewModel : ObservableObject, IDisposable
{
    // ===== 服务依赖 =====
    private readonly DesktopIconService _iconService;
    private readonly MouseHoverDetector _hoverDetector;
    
    // ===== 可观察属性 =====
    
    [ObservableProperty]
    private bool _isRunning;
    
    [ObservableProperty]
    private int _iconCount;
    
    [ObservableProperty]
    private string _statusMessage = "就绪";
    
    [ObservableProperty]
    private int _hoverThresholdMs = 300;
    
    /// <summary>
    /// 悬停事件 - 供 View 订阅以显示预览窗口
    /// </summary>
    public event EventHandler<HoverStateChangedEventArgs>? HoverStateChanged;
    
    public MainViewModel()
    {
        _iconService = new DesktopIconService();
        _hoverDetector = new MouseHoverDetector(_iconService);
        
        // 订阅悬停事件
        _hoverDetector.HoverStateChanged += OnHoverStateChanged;
    }
    
    // ===== 命令 =====
    
    [RelayCommand]
    private async Task StartAsync()
    {
        if (IsRunning) return;
        
        StatusMessage = "正在扫描桌面图标...";
        
        // 异步刷新图标缓存
        bool success = await _iconService.RefreshIconCacheAsync();
        
        if (success)
        {
            IconCount = _iconService.GetAllIcons().Count;
            StatusMessage = $"已加载 {IconCount} 个图标，监控中...";
            
            // 同步悬停阈值设置
            _hoverDetector.HoverThresholdMs = HoverThresholdMs;
            
            // 启动检测
            _hoverDetector.Start();
            IsRunning = true;
        }
        else
        {
            StatusMessage = "无法获取桌面图标，请检查权限";
        }
    }
    
    [RelayCommand]
    private void Stop()
    {
        _hoverDetector.Stop();
        IsRunning = false;
        StatusMessage = "已停止";
    }
    
    [RelayCommand]
    private async Task RefreshIconsAsync()
    {
        StatusMessage = "正在刷新图标...";
        bool success = await _iconService.RefreshIconCacheAsync();
        
        if (success)
        {
            IconCount = _iconService.GetAllIcons().Count;
            StatusMessage = $"已刷新，共 {IconCount} 个图标";
        }
        else
        {
            StatusMessage = "刷新失败";
        }
    }
    
    [RelayCommand]
    private void OpenSettings()
    {
        var settingsWindow = new SettingsWindow();
        settingsWindow.ShowDialog();
    }
    
    // ===== 私有方法 =====
    
    private void OnHoverStateChanged(object? sender, HoverStateChangedEventArgs e)
    {
        // 转发事件给 View
        HoverStateChanged?.Invoke(this, e);
    }
    
    /// <summary>
    /// 设置弹出窗口边界，用于扩展悬停检测区域
    /// </summary>
    public void SetPopupBounds(RECT? bounds)
    {
        _hoverDetector.SetPopupBounds(bounds);
    }
    
    partial void OnHoverThresholdMsChanged(int value)
    {
        _hoverDetector.HoverThresholdMs = value;
    }
    
    public void Dispose()
    {
        _hoverDetector.HoverStateChanged -= OnHoverStateChanged;
        _hoverDetector.Dispose();
        _iconService.Dispose();
    }
}
