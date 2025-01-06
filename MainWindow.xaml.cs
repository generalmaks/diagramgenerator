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
    private ScaleTransform _scaleTransform = new ScaleTransform();

    private Dictionary<string, Action> _buttonActions;

    public MainWindow()
    {
        _buttonActions = new Dictionary<string, Action>()
        {
            { "NewFile", Clear },
            { "SaveDiagramText", FileEditor.SaveDiagramFile },
            { "OpenProject", OpenProject },
            { "SaveImage", FileEditor.SaveImage },
            { "BuildComponentsDiagram", AnalyzeProjectComponents },
            {"BuildHierarchyDiagram", BuildHierarchyDiagram}
        };
        InitializeComponent();
        ImageViewer.LayoutTransform = _scaleTransform;
    }

    private void Click(object sender, RoutedEventArgs e)
    {
        string? tag;
        if (sender is Button button2)
            tag = button2.Tag.ToString();
        else if (sender is MenuItem menuItem2)
            tag = menuItem2.Tag.ToString();
        else
            return;

        var action = _buttonActions[tag];
        action.Invoke();
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

    private void OpenProject()
    {
        Analyzer.GetProject();
    }

    private void AnalyzeProjectComponents()
    {
        var image = ImageViewer.Source;
        Analyzer.CreateComponentsDiagram(ref image);
        ImageViewer.Source = image;
        DiagramTextBox.Text = Analyzer.UmlDiagram.ToString();
    }

    private void BuildHierarchyDiagram()
    {
        var image = ImageViewer.Source;
        Analyzer.CreateHierarchyDiagram(ref image);
        ImageViewer.Source = image;
        DiagramTextBox.Text = Analyzer.UmlDiagram.ToString();
    }

    private void Clear()
    {
        ImageViewer.Source = null;
        DiagramTextBox.Text = "";
    }
}