using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DiagramGenerator;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly ScaleTransform _scaleTransform = new ScaleTransform();


    public MainWindow()
    {
        InitializeComponent();
        ImageViewer.LayoutTransform = _scaleTransform;
    }

    private void Click(object sender, RoutedEventArgs e)
    {

    }

    private void DiagramViewbox_OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
        {
            ScaleImage(e.Delta > 0 ? 1.1 : 0.9);

            e.Handled = true;
        }
    }

    private void ScaleImage(double scale)
    {
        _scaleTransform.ScaleX *= scale;
        _scaleTransform.ScaleY *= scale;
    }

    private void ReloadDiagram(object sender, RoutedEventArgs e)
    {
        Analyzer.SetDiagram(DiagramTextBox.Text);
        ImageViewer.Source = Analyzer.DiagramImage;
    }

    private void OpenProject(object sender, RoutedEventArgs routedEventArgs)
    {
        Analyzer.GetProject();
    }

    private void AnalyzeProjectComponents(object sender, RoutedEventArgs routedEventArgs)
    {
        var image = ImageViewer.Source;
        Analyzer.CreateComponentsDiagram(ref image);
        ImageViewer.Source = image;
        DiagramTextBox.Text = Analyzer.UmlDiagram.ToString();
    }

    private void BuildHierarchyDiagram(object sender, RoutedEventArgs routedEventArgs)
    {
        var image = ImageViewer.Source;
        ClassHierarchy.CreateHierarchyDiagram(ref image);
        ImageViewer.Source = image;
        DiagramTextBox.Text = Analyzer.UmlDiagram.ToString();
    }

    private void Clear()
    {
        ImageViewer.Source = null;
        DiagramTextBox.Text = "";
    }

    private void NewFile(object sender, RoutedEventArgs e)
    {
        throw new NotImplementedException();
    }

    private void SaveDiagramAsText(object sender, RoutedEventArgs e)
    {
        FileEditor.SaveDiagramFile();
    }

    private void OpenSettings(object sender, RoutedEventArgs e)
    {
        throw new NotImplementedException();
    }

    private void SaveDiagramAsImage(object sender, RoutedEventArgs e)
    {
        FileEditor.SaveImage();
    }
}