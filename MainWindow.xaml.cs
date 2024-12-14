using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace OopRgr;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private ScaleTransform _scaleTransform = new ScaleTransform();

    public MainWindow()
    {
        InitializeComponent();
        ImageViewer.LayoutTransform = _scaleTransform;
    }

    private void Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button || sender is MenuItem menuItem)
        {
            string? tag;
            if (sender is Button button2)
            {
                tag = button2.Tag.ToString();
            }
            else if (sender is MenuItem menuItem2)
            {
                tag = menuItem2.Tag.ToString();
            }
            else
            {
                return;
            }

            if (tag == "OpenProject")
            {
                Analyzer.GetProject();
                ImageViewer.Source = Analyzer.DiagramImage;
                DiagramTextBox.Text = Analyzer._umlDiagram.ToString();
            }
        }
    }

    private void DiagramViewbox_OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
        {
            if (e.Delta > 0)
            {
                _scaleTransform.ScaleX *= 1.1;
                _scaleTransform.ScaleY *= 1.1;
            }
            else
            {
                _scaleTransform.ScaleX /= 1.1;
                _scaleTransform.ScaleY /= 1.1;
            }

            e.Handled = true;
        }
    }

    private void ReloadDiagram(object sender, RoutedEventArgs e)
    {
        Analyzer.ChangeDiagram(DiagramTextBox.Text);
        ImageViewer.Source = Analyzer.DiagramImage;
    }
}