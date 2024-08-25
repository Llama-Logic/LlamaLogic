namespace LlamaLogic.LlamaPadPreview.Converters;

class FalseIsCollapsed :
    IValueConverter
{
    public static readonly FalseIsCollapsed Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is bool b && b ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is Visibility v && v != Visibility.Collapsed;
}
