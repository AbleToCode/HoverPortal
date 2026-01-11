// ============================================================================
// HoverPortal - Settings ViewModel
// Phase 5: 个性化与设置
// 遵循 dev-rules-1: MVVM 架构，View 与 Logic 严格分离
// ============================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HoverPortal.Models;
using HoverPortal.Services;

namespace HoverPortal.ViewModels;

/// <summary>
/// 设置页面 ViewModel
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsService _settingsService;
    private AppSettings? _originalSettings;
    
    /// <summary>
    /// 当前设置 (可编辑)
    /// </summary>
    [ObservableProperty]
    private AppSettings _settings = new();
    
    /// <summary>
    /// 是否有未保存的更改
    /// </summary>
    [ObservableProperty]
    private bool _hasChanges;
    
    /// <summary>
    /// 是否正在加载
    /// </summary>
    [ObservableProperty]
    private bool _isLoading;
    
    /// <summary>
    /// 状态消息
    /// </summary>
    [ObservableProperty]
    private string _statusMessage = string.Empty;
    
    /// <summary>
    /// 动画预设选项列表
    /// </summary>
    public List<AnimationPresetOption> AnimationPresetOptions { get; } = new()
    {
        new AnimationPresetOption(AnimationPreset.Quick, "极速", "最快的动画响应"),
        new AnimationPresetOption(AnimationPreset.Balanced, "均衡", "平衡速度与流畅度"),
        new AnimationPresetOption(AnimationPreset.Elegant, "优雅", "更流畅的动画效果")
    };
    
    /// <summary>
    /// 关闭窗口事件
    /// </summary>
    public event Action? RequestClose;
    
    /// <summary>
    /// 设置已保存事件
    /// </summary>
    public event Action<AppSettings>? SettingsSaved;
    
    public SettingsViewModel()
    {
        _settingsService = SettingsService.Instance;
    }
    
    /// <summary>
    /// 初始化 - 加载设置
    /// </summary>
    public async Task InitializeAsync()
    {
        IsLoading = true;
        StatusMessage = "正在加载设置...";
        
        try
        {
            var settings = await _settingsService.LoadAsync();
            _originalSettings = settings.Clone();
            Settings = settings.Clone();
            
            // 监听设置变化
            Settings.PropertyChanged += (s, e) => HasChanges = true;
            
            StatusMessage = string.Empty;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SettingsViewModel] Load failed: {ex.Message}");
            StatusMessage = "加载设置失败";
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    /// <summary>
    /// 保存设置
    /// </summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        IsLoading = true;
        StatusMessage = "正在保存...";
        
        try
        {
            await _settingsService.SaveAsync(Settings);
            _originalSettings = Settings.Clone();
            HasChanges = false;
            
            StatusMessage = "设置已保存";
            
            // 通知设置已更新
            SettingsSaved?.Invoke(Settings);
            
            // 延迟关闭
            await Task.Delay(500);
            RequestClose?.Invoke();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SettingsViewModel] Save failed: {ex.Message}");
            StatusMessage = "保存失败: " + ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    /// <summary>
    /// 取消并关闭
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        if (HasChanges)
        {
            var result = MessageBox.Show(
                "您有未保存的更改，确定要放弃吗？",
                "确认",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            
            if (result != MessageBoxResult.Yes)
            {
                return;
            }
        }
        
        RequestClose?.Invoke();
    }
    
    /// <summary>
    /// 重置为默认设置
    /// </summary>
    [RelayCommand]
    private async Task ResetToDefaultsAsync()
    {
        var result = MessageBox.Show(
            "确定要将所有设置恢复为默认值吗？",
            "重置设置",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        
        if (result != MessageBoxResult.Yes)
        {
            return;
        }
        
        IsLoading = true;
        StatusMessage = "正在重置...";
        
        try
        {
            var defaults = await _settingsService.ResetToDefaultsAsync();
            Settings.CopyFrom(defaults);
            _originalSettings = defaults.Clone();
            HasChanges = false;
            
            StatusMessage = "已恢复默认设置";
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SettingsViewModel] Reset failed: {ex.Message}");
            StatusMessage = "重置失败";
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    /// <summary>
    /// 打开配置文件位置
    /// </summary>
    [RelayCommand]
    private void OpenConfigFolder()
    {
        try
        {
            var path = SettingsService.GetSettingsFilePath();
            var folder = System.IO.Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(folder))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = folder,
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SettingsViewModel] Open folder failed: {ex.Message}");
        }
    }
}

/// <summary>
/// 动画预设选项 (用于 ComboBox 绑定)
/// </summary>
public record AnimationPresetOption(AnimationPreset Value, string DisplayName, string Description);
