// ============================================================================
// HoverPortal - Mouse Hover Detection Service
// 遵循 dev-rules-1:
//   - Hook 回调执行时间控制在 5ms 内
//   - 使用变频轮询: 活跃时 16ms (60Hz), 空闲时 1000ms
// ============================================================================

using System;
using System.Threading.Tasks;
using System.Windows.Threading;
using HoverPortal.Interop;

namespace HoverPortal.Services;

/// <summary>
/// 悬停检测结果
/// </summary>
public record HoverResult(
    DesktopIconInfo? Icon,
    int ScreenX,
    int ScreenY,
    TimeSpan HoverDuration
);

/// <summary>
/// 悬停状态变更事件参数
/// </summary>
public class HoverStateChangedEventArgs : EventArgs
{
    public required HoverResult HoverResult { get; init; }
    public required bool IsHovering { get; init; }
}

/// <summary>
/// 鼠标悬停检测器
/// 使用定时器轮询而非全局 Hook，更轻量级
/// </summary>
public sealed class MouseHoverDetector : IDisposable
{
    // ===== 配置参数 =====
    private const int ActivePollIntervalMs = 16;    // 活跃模式: 约 60Hz
    private const int IdlePollIntervalMs = 100;     // 空闲模式: 10Hz
    private const int DefaultHoverThresholdMs = 300; // 默认悬停阈值
    private const int HideDelayMs = 500;            // 延迟隐藏时间 (0.5秒)
    private const int IconSwitchDelayMs = 300;      // 切换图标延迟时间 (0.3秒)
    
    // ===== 服务依赖 =====
    private readonly DesktopIconService _iconService;
    private readonly DispatcherTimer _pollTimer;
    
    // ===== 状态追踪 =====
    private DesktopIconInfo? _currentHoverIcon;
    private DateTime _hoverStartTime;
    private bool _isHoverTriggered;
    private int _hoverThresholdMs;
    private bool _isDisposed;
    
    // ===== 延迟隐藏机制 =====
    private DispatcherTimer? _hideDelayTimer;
    private (DesktopIconInfo icon, int screenX, int screenY, DateTime startTime)? _pendingHide;
    
    // ===== 弹出窗口边界追踪 =====
    private RECT? _popupBounds;
    
    /// <summary>
    /// 悬停状态变更事件
    /// </summary>
    public event EventHandler<HoverStateChangedEventArgs>? HoverStateChanged;
    
    /// <summary>
    /// 设置弹出窗口边界 (屏幕像素坐标)
    /// 当设置后，鼠标在弹出窗口区域内也视为有效悬停
    /// </summary>
    public void SetPopupBounds(RECT? bounds)
    {
        _popupBounds = bounds;
    }
    
    /// <summary>
    /// 检查点是否在弹出窗口区域内
    /// </summary>
    private bool IsPointInPopup(int screenX, int screenY)
    {
        if (_popupBounds == null) return false;
        var bounds = _popupBounds.Value;
        return screenX >= bounds.Left && screenX <= bounds.Right
            && screenY >= bounds.Top && screenY <= bounds.Bottom;
    }
    
    /// <summary>
    /// 悬停阈值 (毫秒)
    /// </summary>
    public int HoverThresholdMs
    {
        get => _hoverThresholdMs;
        set => _hoverThresholdMs = Math.Max(100, Math.Min(2000, value));
    }
    
    public MouseHoverDetector(DesktopIconService iconService)
    {
        _iconService = iconService;
        _hoverThresholdMs = DefaultHoverThresholdMs;
        
        _pollTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(IdlePollIntervalMs)
        };
        _pollTimer.Tick += OnPollTimerTick;
        
        // 初始化延迟隐藏计时器
        _hideDelayTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(HideDelayMs)
        };
        _hideDelayTimer.Tick += OnHideDelayTimerTick;
    }
    
    /// <summary>
    /// 启动检测
    /// </summary>
    public void Start()
    {
        if (_isDisposed) return;
        _pollTimer.Start();
    }
    
    /// <summary>
    /// 停止检测
    /// </summary>
    public void Stop()
    {
        _pollTimer.Stop();
        ResetHoverState();
    }
    
    // ===== 核心检测逻辑 =====
    
    /// <summary>
    /// 轮询回调 - 必须在 5ms 内完成 (dev-rules-1 要求)
    /// </summary>
    private void OnPollTimerTick(object? sender, EventArgs e)
    {
        // 获取当前鼠标位置
        if (!NativeMethods.GetCursorPos(out var cursorPos))
        {
            return;
        }
        
        // 检查鼠标是否在某个文件夹图标上
        var icon = _iconService.GetFolderIconAtPoint(cursorPos.X, cursorPos.Y);
        
        if (icon != null)
        {
            // 鼠标在图标上
            HandleMouseOnIcon(icon, cursorPos.X, cursorPos.Y);
            
            // 切换到活跃轮询频率
            if (_pollTimer.Interval.TotalMilliseconds != ActivePollIntervalMs)
            {
                _pollTimer.Interval = TimeSpan.FromMilliseconds(ActivePollIntervalMs);
            }
        }
        else
        {
            // 鼠标不在任何图标上，但可能在弹出窗口内
            if (IsPointInPopup(cursorPos.X, cursorPos.Y))
            {
                // 鼠标在弹出窗口内，保持活跃状态，不触发离开事件
                if (_pollTimer.Interval.TotalMilliseconds != ActivePollIntervalMs)
                {
                    _pollTimer.Interval = TimeSpan.FromMilliseconds(ActivePollIntervalMs);
                }
                return; // 不处理离开逻辑
            }
            
            // 鼠标不在图标上也不在弹出窗口内
            HandleMouseLeftIcon(cursorPos.X, cursorPos.Y);
            
            // 切换到空闲轮询频率
            if (_pollTimer.Interval.TotalMilliseconds != IdlePollIntervalMs)
            {
                _pollTimer.Interval = TimeSpan.FromMilliseconds(IdlePollIntervalMs);
            }
        }
    }
    
    private void HandleMouseOnIcon(DesktopIconInfo icon, int screenX, int screenY)
    {
        // 取消任何挂起的延迟隐藏 (鼠标回到图标上)
        CancelPendingHide();
        
        // 验证图标位置是否仍然有效 (检测图标是否被移动)
        if (!_iconService.ValidateIconPosition(icon))
        {
            // 图标已移动，缓存已失效，异步刷新缓存
            _ = RefreshCacheAsync();
            // 重置当前悬停状态
            if (_isHoverTriggered && _currentHoverIcon != null)
            {
                RaiseHoverStateChanged(
                    _currentHoverIcon,
                    screenX,
                    screenY,
                    DateTime.Now - _hoverStartTime,
                    isHovering: false
                );
            }
            ResetHoverState();
            return;
        }
        
        // 检查是否是同一个图标
        if (_currentHoverIcon?.Index == icon.Index)
        {
            // 继续悬停，检查是否达到阈值
            if (!_isHoverTriggered)
            {
                var hoverDuration = DateTime.Now - _hoverStartTime;
                if (hoverDuration.TotalMilliseconds >= _hoverThresholdMs)
                {
                    // 触发悬停事件
                    _isHoverTriggered = true;
                    RaiseHoverStateChanged(icon, screenX, screenY, hoverDuration, isHovering: true);
                }
            }
        }
        else
        {
            // 切换到新图标
            if (_isHoverTriggered && _currentHoverIcon != null)
            {
                // 使用延迟隐藏旧悬浮框 (0.3秒)
                _pendingHide = (_currentHoverIcon, screenX, screenY, _hoverStartTime);
                if (_hideDelayTimer != null)
                {
                    _hideDelayTimer.Interval = TimeSpan.FromMilliseconds(IconSwitchDelayMs);
                    _hideDelayTimer.Start();
                }
            }
            
            _currentHoverIcon = icon;
            _hoverStartTime = DateTime.Now;
            _isHoverTriggered = false;
        }
    }
    
    /// <summary>
    /// 异步刷新图标缓存
    /// </summary>
    private async Task RefreshCacheAsync()
    {
        await _iconService.RefreshIconCacheAsync();
        System.Diagnostics.Debug.WriteLine("[MouseHoverDetector] Icon cache refreshed due to position change");
    }
    
    private void HandleMouseLeftIcon(int screenX, int screenY)
    {
        if (_currentHoverIcon != null)
        {
            if (_isHoverTriggered)
            {
                // 启动延迟隐藏计时器 (0.5秒延迟)
                _pendingHide = (_currentHoverIcon, screenX, screenY, _hoverStartTime);
                if (_hideDelayTimer != null)
                {
                    _hideDelayTimer.Interval = TimeSpan.FromMilliseconds(HideDelayMs);
                    _hideDelayTimer.Start();
                }
            }
            
            ResetHoverState();
        }
    }
    
    /// <summary>
    /// 延迟隐藏计时器回调
    /// </summary>
    private void OnHideDelayTimerTick(object? sender, EventArgs e)
    {
        // 停止计时器 (单次触发)
        _hideDelayTimer?.Stop();
        
        if (_pendingHide.HasValue)
        {
            var (icon, screenX, screenY, startTime) = _pendingHide.Value;
            
            // 触发真正的离开事件
            RaiseHoverStateChanged(
                icon,
                screenX,
                screenY,
                DateTime.Now - startTime,
                isHovering: false
            );
            
            _pendingHide = null;
        }
    }
    
    /// <summary>
    /// 取消挂起的延迟隐藏
    /// </summary>
    private void CancelPendingHide()
    {
        _hideDelayTimer?.Stop();
        _pendingHide = null;
    }
    
    private void ResetHoverState()
    {
        _currentHoverIcon = null;
        _isHoverTriggered = false;
        _hoverStartTime = DateTime.MinValue;
        _popupBounds = null; // 也清除弹出窗口边界
    }
    
    private void RaiseHoverStateChanged(
        DesktopIconInfo icon, 
        int screenX, 
        int screenY, 
        TimeSpan hoverDuration, 
        bool isHovering)
    {
        HoverStateChanged?.Invoke(this, new HoverStateChangedEventArgs
        {
            HoverResult = new HoverResult(icon, screenX, screenY, hoverDuration),
            IsHovering = isHovering
        });
    }
    
    public void Dispose()
    {
        if (!_isDisposed)
        {
            _pollTimer.Stop();
            _pollTimer.Tick -= OnPollTimerTick;
            
            // 清理延迟隐藏计时器
            if (_hideDelayTimer != null)
            {
                _hideDelayTimer.Stop();
                _hideDelayTimer.Tick -= OnHideDelayTimerTick;
                _hideDelayTimer = null;
            }
            _pendingHide = null;
            
            _isDisposed = true;
        }
    }
}
