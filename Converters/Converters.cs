using System.Globalization;
using Discretion.Models;

namespace Discretion.Converters;

public sealed class InvertedBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is not true;
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => value is not true;
}

public sealed class IsNotNullConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is not null;
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

public sealed class IsNullConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is null;
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

public sealed class StatusToColorConverter : IValueConverter
{
    public bool Background { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var key = value switch
        {
            AgreementStatus.Executed => Background ? "SuccessLight" : "Success",
            AgreementStatus.Expired => Background ? "Hairline" : "Muted",
            _ => Background ? "WarningLight" : "Warning"
        };
        return Application.Current?.Resources.TryGetValue(key, out var color) == true ? color : Colors.Gray;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
