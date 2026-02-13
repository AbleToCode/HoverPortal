// ============================================================================
// HoverPortal.Tests - AppSettings 单元测试
// 覆盖: Clone, CopyFrom, GetAnimationDurationMs, CreateDefault, INPC
// ============================================================================

using System.ComponentModel;
using FluentAssertions;
using HoverPortal.Models;
using Xunit;

namespace HoverPortal.Tests.Models;

public class AppSettingsTests
{
    // ========== CreateDefault ==========

    [Fact]
    public void CreateDefault_ReturnsInstanceWithExpectedDefaults()
    {
        var settings = AppSettings.CreateDefault();

        settings.LaunchAtStartup.Should().BeFalse();
        settings.MinimizeToTray.Should().BeTrue();
        settings.CheckForUpdates.Should().BeTrue();
        settings.HoverDelayMs.Should().Be(300);
        settings.AnimationPreset.Should().Be(AnimationPreset.Balanced);
        settings.ShowInFullscreenApps.Should().BeFalse();
        settings.CornerRadius.Should().Be(16);
        settings.WindowOpacity.Should().Be(90);
        settings.EnableBlurEffect.Should().BeTrue();
    }

    // ========== Clone ==========

    [Fact]
    public void Clone_AllPropertiesCopied()
    {
        var original = CreateCustomSettings();
        var clone = original.Clone();

        clone.LaunchAtStartup.Should().Be(original.LaunchAtStartup);
        clone.MinimizeToTray.Should().Be(original.MinimizeToTray);
        clone.CheckForUpdates.Should().Be(original.CheckForUpdates);
        clone.HoverDelayMs.Should().Be(original.HoverDelayMs);
        clone.AnimationPreset.Should().Be(original.AnimationPreset);
        clone.ShowInFullscreenApps.Should().Be(original.ShowInFullscreenApps);
        clone.CornerRadius.Should().Be(original.CornerRadius);
        clone.WindowOpacity.Should().Be(original.WindowOpacity);
        clone.EnableBlurEffect.Should().Be(original.EnableBlurEffect);
    }

    [Fact]
    public void Clone_ProducesIndependentCopy_ModifyCloneDoesNotAffectOriginal()
    {
        var original = AppSettings.CreateDefault();
        var clone = original.Clone();

        clone.HoverDelayMs = 999;
        clone.AnimationPreset = AnimationPreset.Elegant;
        clone.CornerRadius = 0;

        original.HoverDelayMs.Should().Be(300);
        original.AnimationPreset.Should().Be(AnimationPreset.Balanced);
        original.CornerRadius.Should().Be(16);
    }

    [Fact]
    public void Clone_ProducesIndependentCopy_ModifyOriginalDoesNotAffectClone()
    {
        var original = AppSettings.CreateDefault();
        var clone = original.Clone();

        original.HoverDelayMs = 100;
        original.WindowOpacity = 70;

        clone.HoverDelayMs.Should().Be(300);
        clone.WindowOpacity.Should().Be(90);
    }

    // ========== CopyFrom ==========

    [Fact]
    public void CopyFrom_AllPropertiesSynced()
    {
        var source = CreateCustomSettings();
        var target = AppSettings.CreateDefault();

        target.CopyFrom(source);

        target.LaunchAtStartup.Should().Be(source.LaunchAtStartup);
        target.MinimizeToTray.Should().Be(source.MinimizeToTray);
        target.CheckForUpdates.Should().Be(source.CheckForUpdates);
        target.HoverDelayMs.Should().Be(source.HoverDelayMs);
        target.AnimationPreset.Should().Be(source.AnimationPreset);
        target.ShowInFullscreenApps.Should().Be(source.ShowInFullscreenApps);
        target.CornerRadius.Should().Be(source.CornerRadius);
        target.WindowOpacity.Should().Be(source.WindowOpacity);
        target.EnableBlurEffect.Should().Be(source.EnableBlurEffect);
    }

    [Fact]
    public void CopyFrom_DoesNotShareReference_ModifySourceAfterCopy()
    {
        var source = CreateCustomSettings();
        var target = AppSettings.CreateDefault();
        target.CopyFrom(source);

        // Modify source after copy
        source.HoverDelayMs = 1000;
        source.AnimationPreset = AnimationPreset.Quick;

        target.HoverDelayMs.Should().Be(500);
        target.AnimationPreset.Should().Be(AnimationPreset.Elegant);
    }

    // ========== GetAnimationDurationMs ==========

    [Theory]
    [InlineData(AnimationPreset.Quick, 150)]
    [InlineData(AnimationPreset.Balanced, 300)]
    [InlineData(AnimationPreset.Elegant, 500)]
    public void GetAnimationDurationMs_ReturnsCorrectDuration(AnimationPreset preset, int expectedMs)
    {
        var settings = new AppSettings { AnimationPreset = preset };
        settings.GetAnimationDurationMs().Should().Be(expectedMs);
    }

    [Fact]
    public void GetAnimationDurationMs_UndefinedEnum_FallsBackTo300()
    {
        var settings = new AppSettings { AnimationPreset = (AnimationPreset)999 };
        settings.GetAnimationDurationMs().Should().Be(300);
    }

    // ========== PropertyChanged (INPC) ==========

    [Fact]
    public void PropertyChanged_Fires_WhenHoverDelayMsChanges()
    {
        var settings = new AppSettings();
        var changedProperties = new List<string>();
        settings.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName!);

        settings.HoverDelayMs = 500;

        changedProperties.Should().Contain(nameof(AppSettings.HoverDelayMs));
    }

    [Fact]
    public void PropertyChanged_Fires_WhenAnimationPresetChanges()
    {
        var settings = new AppSettings();
        var changedProperties = new List<string>();
        settings.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName!);

        settings.AnimationPreset = AnimationPreset.Elegant;

        changedProperties.Should().Contain(nameof(AppSettings.AnimationPreset));
    }

    [Fact]
    public void PropertyChanged_DoesNotFire_WhenSameValueSet()
    {
        var settings = AppSettings.CreateDefault();
        var firedCount = 0;
        settings.PropertyChanged += (_, _) => firedCount++;

        settings.HoverDelayMs = 300; // same as default

        firedCount.Should().Be(0);
    }

    // ========== Helpers ==========

    private static AppSettings CreateCustomSettings() => new()
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
}
