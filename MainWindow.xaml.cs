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
        if (sender is Button button)
        {
            var tag = button?.Tag.ToString();
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
                _scaleTransform.ScaleX *= 1.1; // Zoom in.
                _scaleTransform.ScaleY *= 1.1;
            }
            else
            {
                _scaleTransform.ScaleX /= 1.1; // Zoom out.
                _scaleTransform.ScaleY /= 1.1;
            }

            e.Handled = true; // Mark the event as handled to prevent further propagation.
        }
    }
}