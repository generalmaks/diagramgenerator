using System.Windows;
using System.Windows.Controls;

namespace OopRgr;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button)
        {
            var tag = button?.Tag.ToString();
            if (tag == "OpenProject")
            {
                Analyzer.GetProject();
            }
        }
    }
}