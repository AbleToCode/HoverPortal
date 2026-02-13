// ============================================================================
// HoverPortal.Tests - Value Converter 单元测试
// 覆盖: BoolToVisibilityConverter, NullToVisibilityConverter
//       正常值、反转参数、非法输入、ConvertBack
// ============================================================================

using System.Globalization;
using System.Windows;
using FluentAssertions;
using HoverPortal.Converters;
using Xunit;

namespace HoverPortal.Tests.Converters;

public class BoolToVisibilityConverterTests
{
    private readonly BoolToVisibilityConverter _converter = new();

    // ========== Convert ==========

    [Fact]
    public void Convert_True_ReturnsVisible()
    {
        var result = _converter.Convert(true, typeof(Visibility), null, CultureInfo.InvariantCulture);
        result.Should().Be(Visibility.Visible);
    }

    [Fact]
    public void Convert_False_ReturnsCollapsed()
    {
        var result = _converter.Convert(false, typeof(Visibility), null, CultureInfo.InvariantCulture);
        result.Should().Be(Visibility.Collapsed);
    }

    [Fact]
    public void Convert_Invert_True_ReturnsCollapsed()
    {
        var result = _converter.Convert(true, typeof(Visibility), "Invert", CultureInfo.InvariantCulture);
        result.Should().Be(Visibility.Collapsed);
    }

    [Fact]
    public void Convert_Invert_False_ReturnsVisible()
    {
        var result = _converter.Convert(false, typeof(Visibility), "Invert", CultureInfo.InvariantCulture);
        result.Should().Be(Visibility.Visible);
    }

    [Fact]
    public void Convert_InvertCaseInsensitive()
    {
        var result = _converter.Convert(true, typeof(Visibility), "invert", CultureInfo.InvariantCulture);
        result.Should().Be(Visibility.Collapsed);
    }

    [Fact]
    public void Convert_NonBoolValue_ReturnsCollapsed()
    {
        var result = _converter.Convert("not a bool", typeof(Visibility), null, CultureInfo.InvariantCulture);
        result.Should().Be(Visibility.Collapsed);
    }

    [Fact]
    public void Convert_NullValue_ReturnsCollapsed()
    {
        var result = _converter.Convert(null!, typeof(Visibility), null, CultureInfo.InvariantCulture);
        result.Should().Be(Visibility.Collapsed);
    }

    // ========== ConvertBack ==========

    [Fact]
    public void ConvertBack_Visible_ReturnsTrue()
    {
        var result = _converter.ConvertBack(Visibility.Visible, typeof(bool), null, CultureInfo.InvariantCulture);
        result.Should().Be(true);
    }

    [Fact]
    public void ConvertBack_Collapsed_ReturnsFalse()
    {
        var result = _converter.ConvertBack(Visibility.Collapsed, typeof(bool), null, CultureInfo.InvariantCulture);
        result.Should().Be(false);
    }

    [Fact]
    public void ConvertBack_Hidden_ReturnsFalse()
    {
        var result = _converter.ConvertBack(Visibility.Hidden, typeof(bool), null, CultureInfo.InvariantCulture);
        result.Should().Be(false);
    }

    [Fact]
    public void ConvertBack_Invert_Visible_ReturnsFalse()
    {
        var result = _converter.ConvertBack(Visibility.Visible, typeof(bool), "Invert", CultureInfo.InvariantCulture);
        result.Should().Be(false);
    }

    [Fact]
    public void ConvertBack_Invert_Collapsed_ReturnsTrue()
    {
        var result = _converter.ConvertBack(Visibility.Collapsed, typeof(bool), "Invert", CultureInfo.InvariantCulture);
        result.Should().Be(true);
    }

    [Fact]
    public void ConvertBack_NonVisibility_ReturnsFalse()
    {
        var result = _converter.ConvertBack("not visibility", typeof(bool), null, CultureInfo.InvariantCulture);
        result.Should().Be(false);
    }
}

public class NullToVisibilityConverterTests
{
    private readonly NullToVisibilityConverter _converter = new();

    // ========== Convert ==========

    [Fact]
    public void Convert_Null_ReturnsCollapsed()
    {
        var result = _converter.Convert(null!, typeof(Visibility), null, CultureInfo.InvariantCulture);
        result.Should().Be(Visibility.Collapsed);
    }

    [Fact]
    public void Convert_NonNull_ReturnsVisible()
    {
        var result = _converter.Convert("something", typeof(Visibility), null, CultureInfo.InvariantCulture);
        result.Should().Be(Visibility.Visible);
    }

    [Fact]
    public void Convert_NonNull_Object_ReturnsVisible()
    {
        var result = _converter.Convert(42, typeof(Visibility), null, CultureInfo.InvariantCulture);
        result.Should().Be(Visibility.Visible);
    }

    [Fact]
    public void Convert_Invert_Null_ReturnsVisible()
    {
        var result = _converter.Convert(null!, typeof(Visibility), "Invert", CultureInfo.InvariantCulture);
        result.Should().Be(Visibility.Visible);
    }

    [Fact]
    public void Convert_Invert_NonNull_ReturnsCollapsed()
    {
        var result = _converter.Convert("something", typeof(Visibility), "Invert", CultureInfo.InvariantCulture);
        result.Should().Be(Visibility.Collapsed);
    }

    [Fact]
    public void Convert_InvertCaseInsensitive()
    {
        var result = _converter.Convert(null!, typeof(Visibility), "invert", CultureInfo.InvariantCulture);
        result.Should().Be(Visibility.Visible);
    }

    // ========== ConvertBack ==========

    [Fact]
    public void ConvertBack_ThrowsNotImplementedException()
    {
        var act = () => _converter.ConvertBack(Visibility.Visible, typeof(object), null, CultureInfo.InvariantCulture);
        act.Should().Throw<NotImplementedException>();
    }
}
