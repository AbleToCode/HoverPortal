// ============================================================================
// HoverPortal - Settings Service
// Phase 5: 个性化与设置
// 遵循 dev-rules-1: 异步 IO，禁止在 UI 线程阻塞
// ============================================================================

using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using HoverPortal.Models;

namespace HoverPortal.Services;

/// <summary>
/// 设置服务 - 异步 JSON 配置持久化
/// 配置文件路径: %LOCALAPPDATA%/HoverPortal/settings.json
/// </summary>
public class SettingsService
{
    private static readonly string AppDataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "HoverPortal");
    
    private static readonly string SettingsFilePath = Path.Combine(AppDataFolder, "settings.json");
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };
    
    // 单例模式
    private static SettingsService? _instance;
    public static SettingsService Instance => _instance ??= new SettingsService();
    
    private AppSettings? _cachedSettings;
    
    private SettingsService() { }
    
    /// <summary>
    /// 异步加载设置
    /// 如果配置文件不存在，返回默认设置
    /// </summary>
    public async Task<AppSettings> LoadAsync()
    {
        if (_cachedSettings != null)
        {
            return _cachedSettings;
        }
        
        try
        {
            if (!File.Exists(SettingsFilePath))
            {
                Debug.WriteLine($"[SettingsService] Config file not found, using defaults");
                _cachedSettings = AppSettings.CreateDefault();
                return _cachedSettings;
            }
            
            var json = await File.ReadAllTextAsync(SettingsFilePath);
            _cachedSettings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) 
                              ?? AppSettings.CreateDefault();
            
            Debug.WriteLine($"[SettingsService] Loaded settings from {SettingsFilePath}");
            return _cachedSettings;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SettingsService] Failed to load settings: {ex.Message}");
            _cachedSettings = AppSettings.CreateDefault();
            return _cachedSettings;
        }
    }
    
    /// <summary>
    /// 异步保存设置
    /// </summary>
    public async Task SaveAsync(AppSettings settings)
    {
        try
        {
            // 确保目录存在
            if (!Directory.Exists(AppDataFolder))
            {
                Directory.CreateDirectory(AppDataFolder);
                Debug.WriteLine($"[SettingsService] Created directory: {AppDataFolder}");
            }
            
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            await File.WriteAllTextAsync(SettingsFilePath, json);
            
            // 更新缓存
            _cachedSettings = settings.Clone();
            
            Debug.WriteLine($"[SettingsService] Saved settings to {SettingsFilePath}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SettingsService] Failed to save settings: {ex.Message}");
            throw; // 让调用者知道保存失败
        }
    }
    
    /// <summary>
    /// 重置为默认设置
    /// </summary>
    public async Task<AppSettings> ResetToDefaultsAsync()
    {
        var defaults = AppSettings.CreateDefault();
        await SaveAsync(defaults);
        return defaults;
    }
    
    /// <summary>
    /// 清除缓存，强制下次从文件加载
    /// </summary>
    public void ClearCache()
    {
        _cachedSettings = null;
    }
    
    /// <summary>
    /// 获取配置文件路径 (用于调试)
    /// </summary>
    public static string GetSettingsFilePath() => SettingsFilePath;
}
