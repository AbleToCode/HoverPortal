// ============================================================================
// HoverPortal - Preview Window Code-Behind
// 遵循 dev-rules-1: 动画使用 GPU 加速的 RenderTransform
// ============================================================================

using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using HoverPortal.Effects;
using HoverPortal.ViewModels;



namespace HoverPortal.Views;

/// <summary>
/// 预览窗口代码后置
/// </summary>
public partial class PreviewWindow : Window
{
    private readonly Storyboard _showStoryboard;
    private readonly Storyboard _hideStoryboard;
    private readonly DispatcherTimer _hideDelayTimer;
    private bool _isClosing;
    private bool _isMouseInside;
    
    public PreviewWindow()
    {
        InitializeComponent();
        
        // 创建动画
        _showStoryboard = CreateShowAnimation();
        _hideStoryboard = CreateHideAnimation();
        
        // 创建延迟隐藏定时器 (200ms 延迟检测)
        _hideDelayTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(200)
        };
        _hideDelayTimer.Tick += OnHideDelayTimerTick;
        
        // 订阅鼠标进入事件
        MouseEnter += Window_MouseEnter;
        
        // 设置数据上下文
        DataContext = new PreviewViewModel();
    }

    
    /// <summary>
    /// 获取或设置 ViewModel
    /// </summary>
    public PreviewViewModel ViewModel => (PreviewViewModel)DataContext;
    
    /// <summary>
    /// 显示预览窗口并播放展开动画
    /// </summary>
    public void ShowWithAnimation(double left, double top, string folderPath)
    {
        // 设置位置
        Left = left;
        Top = top;
        
        // 加载文件列表
        ViewModel.LoadFolder(folderPath);
        
        // 重置状态
        _isClosing = false;
        _isMouseInside = true; // 假设显示时鼠标在窗口区域
        _hideDelayTimer.Stop();
        
        // 显示窗口
        Show();
        
        // 播放展开动画
        _showStoryboard.Begin(this);

    }
    
    /// <summary>
    /// 隐藏窗口并播放收缩动画
    /// </summary>
    public void HideWithAnimation()
    {
        if (_isClosing) return;
        _isClosing = true;
        
        // 清除导航历史
        ViewModel.ClearNavigationHistory();
        
        _hideStoryboard.Completed += (s, e) =>
        {
            if (_isClosing)
            {
                Hide();
                _isClosing = false;
            }
        };
        
        _hideStoryboard.Begin(this);
    }
    
    // ===== 事件处理 =====
    
    private void Window_SourceInitialized(object sender, EventArgs e)
    {
        // 窗口句柄已创建
        // 注意: 不启用 AcrylicHelper.EnableBlur，因为它会设置整个窗口背景
        // MainBorder 已经有半透明背景 (#E0202020)，保持窗口完全透明
        // AcrylicHelper.EnableBlur(this, useMica: false, isDarkMode: true);
    }

    
    private void Window_MouseEnter(object sender, MouseEventArgs e)
    {
        // 鼠标进入，取消隐藏
        _isMouseInside = true;
        _hideDelayTimer.Stop();
    }
    
    private void Window_MouseLeave(object sender, MouseEventArgs e)
    {
        // 鼠标离开，启动延迟隐藏定时器
        _isMouseInside = false;
        _hideDelayTimer.Start();
    }
    
    private void OnHideDelayTimerTick(object? sender, EventArgs e)
    {
        _hideDelayTimer.Stop();
        
        // 只有当鼠标确实不在窗口内时才隐藏
        if (!_isMouseInside)
        {
            HideWithAnimation();
        }
    }

    
    // ===== 动画创建 =====
    
    /// <summary>
    /// 创建展开动画 (弹簧效果)
    /// </summary>
    private Storyboard CreateShowAnimation()
    {
        var storyboard = new Storyboard();
        
        // 缩放 X
        var scaleXAnim = new DoubleAnimation
        {
            From = 0.8,
            To = 1.0,
            Duration = TimeSpan.FromMilliseconds(400),
            EasingFunction = new ElasticEase { Oscillations = 1, Springiness = 5, EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(scaleXAnim, WindowScale);
        Storyboard.SetTargetProperty(scaleXAnim, new PropertyPath(ScaleTransform.ScaleXProperty));
        storyboard.Children.Add(scaleXAnim);
        
        // 缩放 Y
        var scaleYAnim = new DoubleAnimation
        {
            From = 0.8,
            To = 1.0,
            Duration = TimeSpan.FromMilliseconds(400),
            EasingFunction = new ElasticEase { Oscillations = 1, Springiness = 5, EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(scaleYAnim, WindowScale);
        Storyboard.SetTargetProperty(scaleYAnim, new PropertyPath(ScaleTransform.ScaleYProperty));
        storyboard.Children.Add(scaleYAnim);
        
        // 透明度
        var opacityAnim = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = TimeSpan.FromMilliseconds(200),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(opacityAnim, MainBorder);
        Storyboard.SetTargetProperty(opacityAnim, new PropertyPath(OpacityProperty));
        storyboard.Children.Add(opacityAnim);
        
        return storyboard;
    }
    
    /// <summary>
    /// 创建收缩动画
    /// </summary>
    private Storyboard CreateHideAnimation()
    {
        var storyboard = new Storyboard();
        
        // 缩放 X
        var scaleXAnim = new DoubleAnimation
        {
            To = 0.9,
            Duration = TimeSpan.FromMilliseconds(150),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
        };
        Storyboard.SetTarget(scaleXAnim, WindowScale);
        Storyboard.SetTargetProperty(scaleXAnim, new PropertyPath(ScaleTransform.ScaleXProperty));
        storyboard.Children.Add(scaleXAnim);
        
        // 缩放 Y
        var scaleYAnim = new DoubleAnimation
        {
            To = 0.9,
            Duration = TimeSpan.FromMilliseconds(150),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
        };
        Storyboard.SetTarget(scaleYAnim, WindowScale);
        Storyboard.SetTargetProperty(scaleYAnim, new PropertyPath(ScaleTransform.ScaleYProperty));
        storyboard.Children.Add(scaleYAnim);
        
        // 透明度
        var opacityAnim = new DoubleAnimation
        {
            To = 0,
            Duration = TimeSpan.FromMilliseconds(150),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
        };
        Storyboard.SetTarget(opacityAnim, MainBorder);
        Storyboard.SetTargetProperty(opacityAnim, new PropertyPath(OpacityProperty));
        storyboard.Children.Add(opacityAnim);
        
        return storyboard;
    }
}
