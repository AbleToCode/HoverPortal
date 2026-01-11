// ============================================================================
// HoverPortal - Value Converters
// ============================================================================

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace HoverPortal.Converters;

/// <summary>
/// Boolean 到 Visibility 转换器
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            // 如果参数为 "Invert"，则反转逻辑
            bool invert = parameter?.ToString()?.Equals("Invert", StringComparison.OrdinalIgnoreCase) ?? false;
            bool result = invert ? !boolValue : boolValue;
            return result ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility visibility)
        {
            bool result = visibility == Visibility.Visible;
            bool invert = parameter?.ToString()?.Equals("Invert", StringComparison.OrdinalIgnoreCase) ?? false;
            return invert ? !result : result;
        }
        return false;
    }
}

/// <summary>
/// Null 到 Visibility 转换器 (null 时隐藏，非 null 时显示)
/// 反向模式: null 时显示备用内容
/// </summary>
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isNull = value == null;
        // 默认: null -> Collapsed, 非 null -> Visible
        // 参数 "Invert": null -> Visible (显示备用), 非 null -> Collapsed
        bool invert = parameter?.ToString()?.Equals("Invert", StringComparison.OrdinalIgnoreCase) ?? false;
        
        if (invert)
        {
            return isNull ? Visibility.Visible : Visibility.Collapsed;
        }
        return isNull ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

