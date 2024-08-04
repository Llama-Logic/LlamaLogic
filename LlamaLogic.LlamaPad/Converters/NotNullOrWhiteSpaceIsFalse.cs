namespace LlamaLogic.LlamaPad.Converters;

class NotNullOrWhiteSpaceIsFalse :
    IValueConverter
{
    public static NotNullOrWhiteSpaceIsFalse Instance { get; } = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        string.IsNullOrWhiteSpace(value as string);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        Binding.DoNothing;
}
