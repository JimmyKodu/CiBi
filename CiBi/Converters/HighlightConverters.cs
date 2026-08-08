using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace CiBi.Converters;

/// <summary>Converts a bool to one of two brushes given via ConverterParameter "trueColor|falseColor".</summary>
public sealed class BoolToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var b = value is bool bo && bo;
        var (t, f) = Parse(parameter?.ToString());
        return new SolidColorBrush(Color.Parse(b ? t : f));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    internal static (string trueColor, string falseColor) Parse(string? p)
    {
        var parts = p?.Split('|');
        var t = parts is { Length: > 0 } && !string.IsNullOrWhiteSpace(parts[0]) ? parts[0] : "#1428A0";
        var f = parts is { Length: > 1 } && !string.IsNullOrWhiteSpace(parts[1]) ? parts[1] : "#E5E5E5";
        return (t, f);
    }
}

/// <summary>Converts a bool to a Thickness given via ConverterParameter "trueThickness|falseThickness".</summary>
public sealed class BoolToThicknessConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var b = value is bool bo && bo;
        var parts = parameter?.ToString()?.Split('|');
        var s = b
            ? (parts is { Length: > 0 } ? parts[0] : "2")
            : (parts is { Length: > 1 } ? parts[1] : "1");
        return Avalonia.Thickness.Parse(s);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
