// ============================================================================
// HoverPortal - Preview Window Code-Behind
// 遵循 dev-rules-1: MVVM 架构，View 与 Logic 严格分离
// 遵循 dev-rules-1: UI 动效优先考虑 GPU 加速
// ============================================================================

using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using HoverPortal.Effects;
using HoverPortal.ViewModels;

namespace HoverPortal.Views;

/// <summary>
/// PreviewWindow.xaml 的交互逻辑
/// </summary>
public partial class PreviewWindow : Window
{
    private readonly PreviewViewModel _viewModel;
    private Storyboard? _showStoryboard;
    private Storyboard? _hideStoryboard;
    private bool _isAnimating;
    
    public PreviewWindow()
    {
        InitializeComponent();
        
        _viewModel = new PreviewViewModel();
        DataContext = _viewModel;
        
        // 初始化动画 (遵循 dev-rules-1: 交互丝滑度)
        InitializeAnimations();
    }
    
    /// <summary>
    /// 初始化显示/隐藏动画
    /// </summary>
    private void InitializeAnimations()
    {
        var springEase = (ElasticEase)FindResource("SpringEase");
        var smoothEase = (QuadraticEase)FindResource("SmoothEase");
        
        // ===== 显示动画 =====
        _showStoryboard = new Storyboard();
        
        // 淡入
        var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200))
        {
            EasingFunction = smoothEase
        };
        Storyboard.SetTarget(fadeIn, MainBorder);
        Storyboard.SetTargetProperty(fadeIn, new PropertyPath("Opacity"));
        _showStoryboard.Children.Add(fadeIn);
        
        // 缩放 X
        var scaleXIn = new DoubleAnimation(0.8, 1, TimeSpan.FromMilliseconds(350))
        {
            EasingFunction = springEase
        };
        Storyboard.SetTarget(scaleXIn, WindowScale);
        Storyboard.SetTargetProperty(scaleXIn, new PropertyPath("ScaleX"));
        _showStoryboard.Children.Add(scaleXIn);
        
        // 缩放 Y
        var scaleYIn = new DoubleAnimation(0.8, 1, TimeSpan.FromMilliseconds(350))
        {
            EasingFunction = springEase
        };
        Storyboard.SetTarget(scaleYIn, WindowScale);
        Storyboard.SetTargetProperty(scaleYIn, new PropertyPath("ScaleY"));
        _showStoryboard.Children.Add(scaleYIn);
        
        // ===== 隐藏动画 =====
        _hideStoryboard = new Storyboard();
        
        // 淡出
        var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(150))
        {
            EasingFunction = smoothEase
        };
        Storyboard.SetTarget(fadeOut, MainBorder);
        Storyboard.SetTargetProperty(fadeOut, new PropertyPath("Opacity"));
        _hideStoryboard.Children.Add(fadeOut);
        
        // 缩放 X
        var scaleXOut = new DoubleAnimation(1, 0.9, TimeSpan.FromMilliseconds(150))
        {
            EasingFunction = smoothEase
        };
        Storyboard.SetTarget(scaleXOut, WindowScale);
        Storyboard.SetTargetProperty(scaleXOut, new PropertyPath("ScaleX"));
        _hideStoryboard.Children.Add(scaleXOut);
        
        // 缩放 Y
        var scaleYOut = new DoubleAnimation(1, 0.9, TimeSpan.FromMilliseconds(150))
        {
            EasingFunction = smoothEase
        };
        Storyboard.SetTarget(scaleYOut, WindowScale);
        Storyboard.SetTargetProperty(scaleYOut, new PropertyPath("ScaleY"));
        _hideStoryboard.Children.Add(scaleYOut);
        
        _hideStoryboard.Completed += (s, e) =>
        {
            _isAnimating = false;
            Hide();
        };
        
        _showStoryboard.Completed += (s, e) =>
        {
            _isAnimating = false;
        };
    }
    
    /// <summary>
    /// 显示窗口并播放动画
    /// </summary>
    public void ShowWithAnimation(double left, double top, string folderPath)
    {
        if (_isAnimating) return;
        
        // 设置窗口位置
        Left = left;
        Top = top;
        
        // 加载文件夹内容
        _viewModel.ClearNavigationHistory();
        _viewModel.LoadFolder(folderPath);
        
        // 重置动画初始状态
        MainBorder.Opacity = 0;
        WindowScale.ScaleX = 0.8;
        WindowScale.ScaleY = 0.8;
        
        // 显示窗口
        Show();
        
        // 播放动画
        _isAnimating = true;
        _showStoryboard?.Begin(this);
    }
    
    /// <summary>
    /// 隐藏窗口并播放动画
    /// </summary>
    public void HideWithAnimation()
    {
        if (_isAnimating || !IsVisible) return;
        
        _isAnimating = true;
        _hideStoryboard?.Begin(this);
    }
    
    /// <summary>
    /// 窗口源初始化 - 设置无激活窗口样式
    /// 遵循 dev-rules-1: P/Invoke 调用
    /// </summary>
    private void Window_SourceInitialized(object sender, EventArgs e)
    {
        // 设置为工具窗口，不在任务栏显示，不抢夺焦点
        var hwnd = new WindowInteropHelper(this).Handle;
        var extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);
        
        // 注意: 不调用 AcrylicHelper.EnableBlur() 
        // DWM 亚克力效果会应用于整个矩形窗口，导致出现外部方框
        // 使用 WPF 原生透明窗口 + 半透明 Border 即可实现无边框效果
    }
    
    /// <summary>
    /// 鼠标离开窗口
    /// </summary>
    private void Window_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        // 鼠标离开时不立即隐藏，由 MainViewModel 的 hover 检测逻辑统一管理
        // 这里留空，保持窗口显示状态
    }
    
    /// <summary>
    /// 右键点击 - 返回上级文件夹
    /// </summary>
    private void MainBorder_MouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        _viewModel.NavigateToParentCommand.Execute(null);
        e.Handled = true; // 防止事件冒泡触发系统右键菜单
    }
    
    #region Win32 API
    
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_NOACTIVATE = 0x08000000;
    
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
    
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
    
    #endregion
}
