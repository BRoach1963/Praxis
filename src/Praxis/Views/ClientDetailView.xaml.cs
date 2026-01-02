using System.Windows.Controls;

namespace Praxis.Views;

public partial class ClientDetailView : UserControl
{
    public ClientDetailView()
    {
        InitializeComponent();
    }
}

// Simple converter for header text
public class BooleanToStringConverter : System.Windows.Data.IValueConverter
{
    public string TrueValue { get; set; } = "True";
    public string FalseValue { get; set; } = "False";

    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        return value is bool b && b ? TrueValue : FalseValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
