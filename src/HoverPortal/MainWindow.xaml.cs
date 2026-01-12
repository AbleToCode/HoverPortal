// ============================================================================
// HoverPortal - Main Window Code-Behind
// ============================================================================

using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using HoverPortal.Interop;
using HoverPortal.Services;
using HoverPortal.ViewModels;
using HoverPortal.Views;

namespace HoverPortal;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private PreviewWindow? _previewWindow;
    private MainViewModel? _viewModel;
    private TrayIconService? _trayIconService;
    
    public MainWindow()
    {
        InitializeComponent();
        
        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
        Closed += MainWindow_Closed;
    }
    
    // ===== 窗口控制事件 (Apple Style Borderless) =====
    
    /// <summary>
    /// 允许拖动窗口标题栏
    /// </summary>
    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 1)
        {
            DragMove();
        }
    }
    
    /// <summary>
    /// 关闭按钮点击 - 隐藏到托盘
    /// </summary>
    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        // 隐藏到托盘而不是关闭
        _trayIconService?.HideToTray();
    }
    
    /// <summary>
    /// 最小化按钮点击 - 最小化到任务栏
    /// </summary>
    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }
    
    // ===== 生命周期 =====
    
    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _viewModel = DataContext as MainViewModel;
        
        if (_viewModel != null)
        {
            _viewModel.HoverStateChanged += OnHoverStateChanged;
        }
        
        // 创建预览窗口 (隐藏状态)
        _previewWindow = new PreviewWindow();
        _previewWindow.Hide();
        
        // 初始化托盘图标服务
        _trayIconService = new TrayIconService(this);
        _trayIconService.RequestOpenSettings += () =>
        {
            _viewModel?.OpenSettingsCommand.Execute(null);
        };
    }
    
    /// <summary>
    /// 窗口关闭中 - 判断是否真正关闭还是隐藏
    /// </summary>
    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        // 如果不是通过托盘退出，则取消关闭并隐藏到托盘
        if (_trayIconService != null && !_trayIconService.IsExiting)
        {
            e.Cancel = true;
            _trayIconService.HideToTray();
            
            // 清理临时缓存以减少后台内存占用 (内存优化)
            IconExtractor.ClearCache();
            GC.Collect(2, GCCollectionMode.Optimized);
        }
    }
    
    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.HoverStateChanged -= OnHoverStateChanged;
            _viewModel.Dispose();
        }
        
        _previewWindow?.Close();
        _trayIconService?.Dispose();
    }
    
    /// <summary>
    /// 处理悬停状态变更
    /// </summary>
    private void OnHoverStateChanged(object? sender, HoverStateChangedEventArgs e)
    {
        if (e.IsHovering && e.HoverResult.Icon != null)
        {
            var icon = e.HoverResult.Icon;
            
            // 获取 DPI 缩放因子 (遵循 dev-rules-1: 考虑 Windows 版本差异)
            var source = PresentationSource.FromVisual(this);
            double dpiScaleX = source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
            double dpiScaleY = source?.CompositionTarget?.TransformToDevice.M22 ?? 1.0;
            
            // 将像素坐标转换为 WPF 设备无关单位 (DIU)
            double iconLeft = icon.Bounds.Left / dpiScaleX;
            double iconRight = icon.Bounds.Right / dpiScaleX;
            double iconTop = icon.Bounds.Top / dpiScaleY;
            
            // 计算预览窗口位置 (在图标右侧)
            double left = iconRight + 8;
            double top = iconTop;
            
            // ===== 多显示器支持 =====
            // 使用 Screen.FromPoint 获取图标所在的显示器 (遵循 dev-rules-1: 防御性编程)
            // 注意: Screen.WorkingArea 返回像素坐标，需要转换为 DIU
            var iconPoint = new System.Drawing.Point(icon.Bounds.Left, icon.Bounds.Top);
            var currentScreen = System.Windows.Forms.Screen.FromPoint(iconPoint);
            var screenWorkArea = currentScreen.WorkingArea;
            
            // 将屏幕边界从像素转换为 DIU
            // 注意: 多显示器可能有不同 DPI，这里使用主显示器 DPI 作为近似值
            // 如需精确支持，后续可添加 GetDpiForMonitor P/Invoke
            double screenLeft = screenWorkArea.Left / dpiScaleX;
            double screenRight = screenWorkArea.Right / dpiScaleX;
            double screenTop = screenWorkArea.Top / dpiScaleY;
            double screenBottom = screenWorkArea.Bottom / dpiScaleY;
            
            // 检查右边界: 如果超出则显示在图标左侧
            if (left + 320 > screenRight)
            {
                left = iconLeft - 320 - 8;
            }
            
            // 确保左边界不超出屏幕
            if (left < screenLeft)
            {
                left = screenLeft + 8;
            }
            
            // 检查下边界
            if (top + 280 > screenBottom)
            {
                top = screenBottom - 280 - 8;
            }
            
            // 确保顶部边界不超出屏幕
            if (top < screenTop)
            {
                top = screenTop + 8;
            }
            
            // 显示预览窗口 - 使用真实的文件夹路径
            string folderPath = icon.FilePath;
            
            // 计算合并的检测区域：包含图标 + 弹窗 + 安全边距
            const int safetyMargin = 20; // 20像素安全边距
            
            // 合并图标区域和弹窗区域
            int unionLeft = Math.Min(icon.Bounds.Left, (int)(left * dpiScaleX)) - safetyMargin;
            int unionTop = Math.Min(icon.Bounds.Top, (int)(top * dpiScaleY)) - safetyMargin;
            int unionRight = Math.Max(icon.Bounds.Right, (int)((left + 320) * dpiScaleX)) + safetyMargin;
            int unionBottom = Math.Max(icon.Bounds.Bottom, (int)((top + 280) * dpiScaleY)) + safetyMargin;
            
            var popupBounds = new RECT
            {
                Left = unionLeft,
                Top = unionTop,
                Right = unionRight,
                Bottom = unionBottom
            };
            
            Dispatcher.Invoke(() =>
            {
                _previewWindow?.ShowWithAnimation(left, top, folderPath);
                // 设置弹出窗口边界，使鼠标可以移动到弹出窗口
                _viewModel?.SetPopupBounds(popupBounds);
            });
        }
        else
        {
            // 隐藏预览窗口
            Dispatcher.Invoke(() =>
            {
                _previewWindow?.HideWithAnimation();
                // 清除弹出窗口边界
                _viewModel?.SetPopupBounds(null);
            });
        }
    }
}
