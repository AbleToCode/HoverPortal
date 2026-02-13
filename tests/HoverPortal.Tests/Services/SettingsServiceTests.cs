// ============================================================================
// HoverPortal.Tests - SettingsService 单元测试
// 覆盖: JSON 序列化/反序列化 round-trip, camelCase, enum-as-string,
//       缺失字段回退, 损坏 JSON 处理, 文件 I/O round-trip
// ============================================================================

using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using HoverPortal.Models;
using Xunit;

namespace HoverPortal.Tests.Services;

/// <summary>
/// 由于 SettingsService 是私有构造 + 静态单例，直接使用相同的 JsonSerializerOptions
/// 来验证 AppSettings 的序列化兼容性，并通过临时文件验证文件 I/O 流程。
/// </summary>
public class SettingsServiceTests : IDisposable
{
    /// <summary>
    /// 与 SettingsService 内部保持一致的 JSON 选项
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _tempDir;

    public SettingsServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"HoverPortal_Tests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    // ========== Round-trip ==========

    [Fact]
    public void Serialize_Deserialize_RoundTrip_AllPropertiesPreserved()
    {
        var original = new AppSettings
        {
            LaunchAtStartup = true,
            MinimizeToTray = false,
            CheckForUpdates = false,
            HoverDelayMs = 500,
            AnimationPreset = AnimationPreset.Elegant,
            ShowInFullscreenApps = true,
            CornerRadius = 8,
            WindowOpacity = 75,
            EnableBlurEffect = false
        };

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);

        deserialized.Should().NotBeNull();
        deserialized!.LaunchAtStartup.Should().Be(original.LaunchAtStartup);
        deserialized.MinimizeToTray.Should().Be(original.MinimizeToTray);
        deserialized.CheckForUpdates.Should().Be(original.CheckForUpdates);
        deserialized.HoverDelayMs.Should().Be(original.HoverDelayMs);
        deserialized.AnimationPreset.Should().Be(original.AnimationPreset);
        deserialized.ShowInFullscreenApps.Should().Be(original.ShowInFullscreenApps);
        deserialized.CornerRadius.Should().Be(original.CornerRadius);
        deserialized.WindowOpacity.Should().Be(original.WindowOpacity);
        deserialized.EnableBlurEffect.Should().Be(original.EnableBlurEffect);
    }

    [Fact]
    public void Serialize_DefaultSettings_ProducesStableOutput()
    {
        var defaults = AppSettings.CreateDefault();
        var json1 = JsonSerializer.Serialize(defaults, JsonOptions);
        var json2 = JsonSerializer.Serialize(AppSettings.CreateDefault(), JsonOptions);

        json1.Should().Be(json2, "serializing default settings twice should produce identical JSON");
    }

    // ========== camelCase ==========

    [Fact]
    public void Serialize_UsesCamelCase_PropertyNames()
    {
        var settings = AppSettings.CreateDefault();
        var json = JsonSerializer.Serialize(settings, JsonOptions);

        json.Should().Contain("\"hoverDelayMs\"");
        json.Should().Contain("\"animationPreset\"");
        json.Should().Contain("\"launchAtStartup\"");
        json.Should().Contain("\"cornerRadius\"");
        json.Should().Contain("\"windowOpacity\"");

        // Should NOT contain PascalCase
        json.Should().NotContain("\"HoverDelayMs\"");
        json.Should().NotContain("\"AnimationPreset\"");
    }

    // ========== Enum as string ==========

    [Fact]
    public void Serialize_AnimationPreset_AsString()
    {
        var settings = new AppSettings { AnimationPreset = AnimationPreset.Elegant };
        var json = JsonSerializer.Serialize(settings, JsonOptions);

        json.Should().Contain("\"Elegant\"");
        json.Should().NotContain("2"); // numeric value of Elegant
    }

    [Theory]
    [InlineData("\"Quick\"", AnimationPreset.Quick)]
    [InlineData("\"Balanced\"", AnimationPreset.Balanced)]
    [InlineData("\"Elegant\"", AnimationPreset.Elegant)]
    public void Deserialize_AnimationPreset_FromString(string enumString, AnimationPreset expected)
    {
        var json = $"{{ \"animationPreset\": {enumString} }}";
        var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);

        settings.Should().NotBeNull();
        settings!.AnimationPreset.Should().Be(expected);
    }

    // ========== Missing fields ==========

    [Fact]
    public void Deserialize_MissingFields_UsesPropertyDefaults()
    {
        // Minimal JSON with only one field
        var json = """{ "hoverDelayMs": 999 }""";

        var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);

        settings.Should().NotBeNull();
        settings!.HoverDelayMs.Should().Be(999);

        // All other fields use their C# default values
        settings.LaunchAtStartup.Should().BeFalse();
        settings.MinimizeToTray.Should().BeTrue();
        settings.CheckForUpdates.Should().BeTrue();
        settings.AnimationPreset.Should().Be(AnimationPreset.Balanced);
        settings.CornerRadius.Should().Be(16);
        settings.WindowOpacity.Should().Be(90);
        settings.EnableBlurEffect.Should().BeTrue();
    }

    [Fact]
    public void Deserialize_EmptyObject_AllDefaults()
    {
        var json = "{}";
        var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);

        settings.Should().NotBeNull();
        var defaults = AppSettings.CreateDefault();

        settings!.HoverDelayMs.Should().Be(defaults.HoverDelayMs);
        settings.AnimationPreset.Should().Be(defaults.AnimationPreset);
        settings.CornerRadius.Should().Be(defaults.CornerRadius);
    }

    // ========== Invalid JSON ==========

    [Fact]
    public void Deserialize_InvalidJson_ThrowsJsonException()
    {
        var brokenJson = "{ this is not valid json }}}";

        var act = () => JsonSerializer.Deserialize<AppSettings>(brokenJson, JsonOptions);

        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void Deserialize_NullJson_ThrowsArgumentNullException()
    {
        var act = () => JsonSerializer.Deserialize<AppSettings>((string)null!, JsonOptions);

        act.Should().Throw<ArgumentNullException>();
    }

    // ========== File I/O round-trip ==========

    [Fact]
    public async Task SaveLoad_FileRoundTrip_AllPropertiesPreserved()
    {
        var filePath = Path.Combine(_tempDir, "settings.json");

        var original = new AppSettings
        {
            LaunchAtStartup = true,
            HoverDelayMs = 700,
            AnimationPreset = AnimationPreset.Quick,
            CornerRadius = 4,
            WindowOpacity = 80,
            EnableBlurEffect = false
        };

        // Save
        var json = JsonSerializer.Serialize(original, JsonOptions);
        await File.WriteAllTextAsync(filePath, json);

        // Load
        var loadedJson = await File.ReadAllTextAsync(filePath);
        var loaded = JsonSerializer.Deserialize<AppSettings>(loadedJson, JsonOptions);

        loaded.Should().NotBeNull();
        loaded!.LaunchAtStartup.Should().Be(original.LaunchAtStartup);
        loaded.HoverDelayMs.Should().Be(original.HoverDelayMs);
        loaded.AnimationPreset.Should().Be(original.AnimationPreset);
        loaded.CornerRadius.Should().Be(original.CornerRadius);
        loaded.WindowOpacity.Should().Be(original.WindowOpacity);
        loaded.EnableBlurEffect.Should().Be(original.EnableBlurEffect);
    }

    [Fact]
    public async Task Load_FileNotFound_FallbackToDefault()
    {
        var filePath = Path.Combine(_tempDir, "nonexistent.json");

        File.Exists(filePath).Should().BeFalse();

        // Simulate SettingsService behavior: if file not found, return default
        var settings = File.Exists(filePath)
            ? JsonSerializer.Deserialize<AppSettings>(await File.ReadAllTextAsync(filePath), JsonOptions)
            : AppSettings.CreateDefault();

        settings.Should().NotBeNull();
        settings!.HoverDelayMs.Should().Be(300);
        settings.AnimationPreset.Should().Be(AnimationPreset.Balanced);
    }
}
