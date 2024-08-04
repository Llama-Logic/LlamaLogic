namespace LlamaLogic.LlamaPad.Converters;

class NullOrWhiteSpaceIsCollapsed :
    IValueConverter
{
    public static NullOrWhiteSpaceIsCollapsed Instance { get; } = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        string.IsNullOrWhiteSpace(value as string) ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        Binding.DoNothing;
}
