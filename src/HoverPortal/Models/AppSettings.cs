// ============================================================================
// HoverPortal - Application Settings Model
// Phase 5: 个性化与设置
// 遵循 dev-rules-1: MVVM 架构，使用 CommunityToolkit.Mvvm
// ============================================================================

using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace HoverPortal.Models;

/// <summary>
/// 动画预设枚举
/// </summary>
public enum AnimationPreset
{
    /// <summary>极速 - 最短动画时间</summary>
    Quick,
    
    /// <summary>均衡 - 默认动画时间</summary>
    Balanced,
    
    /// <summary>优雅 - 较长动画时间，更流畅</summary>
    Elegant
}

/// <summary>
/// 应用程序配置数据模型
/// 使用 ObservableObject 支持 MVVM 双向绑定
/// </summary>
public partial class AppSettings : ObservableObject
{
    // ========== 通用设置 (General) ==========
    
    /// <summary>
    /// 开机自动启动
    /// </summary>
    [ObservableProperty]
    private bool _launchAtStartup = false;
    
    /// <summary>
    /// 最小化到系统托盘
    /// </summary>
    [ObservableProperty]
    private bool _minimizeToTray = true;
    
    /// <summary>
    /// 自动检查更新
    /// </summary>
    [ObservableProperty]
    private bool _checkForUpdates = true;
    
    // ========== 行为设置 (Behavior) ==========
    
    /// <summary>
    /// 悬停触发延迟 (毫秒)
    /// 范围: 100-1000ms
    /// </summary>
    [ObservableProperty]
    private int _hoverDelayMs = 300;
    
    /// <summary>
    /// 动画预设
    /// </summary>
    [ObservableProperty]
    private AnimationPreset _animationPreset = AnimationPreset.Balanced;
    
    /// <summary>
    /// 全屏应用时是否显示预览
    /// </summary>
    [ObservableProperty]
    private bool _showInFullscreenApps = false;
    
    // ========== 外观设置 (Appearance) ==========
    
    /// <summary>
    /// 预览窗口圆角半径 (像素)
    /// 范围: 0-24px
    /// </summary>
    [ObservableProperty]
    private int _cornerRadius = 16;
    
    /// <summary>
    /// 预览窗口不透明度 (百分比)
    /// 范围: 70-100%
    /// </summary>
    [ObservableProperty]
    private int _windowOpacity = 90;
    
    /// <summary>
    /// 启用模糊背景效果 (Acrylic/Mica)
    /// </summary>
    [ObservableProperty]
    private bool _enableBlurEffect = true;
    
    // ========== 方法 ==========
    
    /// <summary>
    /// 创建默认配置实例
    /// </summary>
    public static AppSettings CreateDefault() => new();
    
    /// <summary>
    /// 深拷贝当前设置
    /// </summary>
    public AppSettings Clone()
    {
        return new AppSettings
        {
            LaunchAtStartup = this.LaunchAtStartup,
            MinimizeToTray = this.MinimizeToTray,
            CheckForUpdates = this.CheckForUpdates,
            HoverDelayMs = this.HoverDelayMs,
            AnimationPreset = this.AnimationPreset,
            ShowInFullscreenApps = this.ShowInFullscreenApps,
            CornerRadius = this.CornerRadius,
            WindowOpacity = this.WindowOpacity,
            EnableBlurEffect = this.EnableBlurEffect
        };
    }
    
    /// <summary>
    /// 从另一个设置实例复制值
    /// </summary>
    public void CopyFrom(AppSettings other)
    {
        LaunchAtStartup = other.LaunchAtStartup;
        MinimizeToTray = other.MinimizeToTray;
        CheckForUpdates = other.CheckForUpdates;
        HoverDelayMs = other.HoverDelayMs;
        AnimationPreset = other.AnimationPreset;
        ShowInFullscreenApps = other.ShowInFullscreenApps;
        CornerRadius = other.CornerRadius;
        WindowOpacity = other.WindowOpacity;
        EnableBlurEffect = other.EnableBlurEffect;
    }
    
    /// <summary>
    /// 获取动画预设对应的动画时长 (毫秒)
    /// </summary>
    public int GetAnimationDurationMs() => AnimationPreset switch
    {
        AnimationPreset.Quick => 150,
        AnimationPreset.Balanced => 300,
        AnimationPreset.Elegant => 500,
        _ => 300
    };
}
