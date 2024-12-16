using PlantUml.Net;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace OopRgr;

public static class Analyzer
{
    private static string _filePath;
    private static List<NamespaceFile> _namespaces = new List<NamespaceFile>();
    private static List<CsFile> _csFiles = new List<CsFile>();
    public static StringBuilder UmlDiagram = new StringBuilder();
    public static System.Windows.Media.ImageSource DiagramImage;

    public static void GetProject()
    {
        var openDialog = new OpenFolderDialog() { Title = "Виберіть папку з вашим проектом" };
        if (openDialog.ShowDialog() == true)
        {
            _filePath = openDialog.FolderName;
            Console.WriteLine($"Opened from {_filePath}");
            AnalyzeProject();
        }
        else
        {
            Console.WriteLine("No folder selected.");
        }
    }

    static private void AnalyzeProject()
    {
        ClearData();
        List<string> files = GetCsFiles(_filePath);
        foreach (var file in files)
        {
            var csFile = new CsFile();
            csFile.SetName(Path.GetFileNameWithoutExtension(file));

            using (StreamReader sr = new StreamReader(file))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    line = line.Trim();

                    if (line.StartsWith("using "))
                    {
                        csFile.AddUsing(line);
                    }
                    else if (line.StartsWith("namespace "))
                    {
                        string nameSpaceName = ExtractNamespace(line);
                        if (!_namespaces.Any(n => n.Name == nameSpaceName))
                        {
                            NamespaceFile ns = new NamespaceFile(nameSpaceName, csFile);
                            _namespaces.Add(ns);
                        }

                        _namespaces
                            .FirstOrDefault(namespaceFile => namespaceFile.Name == nameSpaceName)?
                            .CsFiles.Add(csFile);
                    }
                }
            }

            _csFiles.Add(csFile);
        }

        WriteDiagramFile();
    }

    private static string ExtractNamespace(string nameSpaceName)
    {
        return nameSpaceName
            .Replace("namespace ", "")
            .Replace(";", "")
            .Replace("{", "")
            .Trim();
    }

    private static void ClearData()
    {
        _namespaces.Clear();
        _csFiles.Clear();
    }

    private static List<string> GetCsFiles(string path)
    {
        return Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains("Debug") && !f.Contains("Release"))
            .ToList();
    }

    static private void WriteDiagramFile()
    {
        UmlDiagram.Clear();
        UmlDiagram.AppendLine("@startuml");
        UmlDiagram.AppendLine("skinparam linetype ortho");

        AddNamespacesToDiagram();
        AddDependenciesToDiagram();

        UmlDiagram.AppendLine("@enduml");
        Console.WriteLine(UmlDiagram);
        SetDiagram(UmlDiagram);
    }

    private static System.Windows.Media.ImageSource ConvertByteToImage(byte[] imageBytes)
    {
        BitmapImage bitmap = new BitmapImage();
        using (MemoryStream ms = new MemoryStream(imageBytes))
        {
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = ms;
            bitmap.EndInit();
            bitmap.Freeze();
        }

        return bitmap;
    }

    private static void AddNamespacesToDiagram()
    {
        foreach (var namespaceFile in _namespaces)
        {
            UmlDiagram.AppendLine($"namespace {namespaceFile.Name} {{");
            foreach (var csFile in namespaceFile.CsFiles)
            {
                AddCsFileToDiagram(csFile);
            }

            UmlDiagram.AppendLine("}");
        }
    }

    private static void AddCsFileToDiagram(CsFile csFile)
    {
        string name = csFile.Name;
        var indexOfDot = name.IndexOf('.');
        if (indexOfDot >= 0)
        {
            name = name.Substring(0, indexOfDot);
        }

        if (IsIntefrace(name))
        {
            UmlDiagram.AppendLine($"\tinterface \"{name}\"{{}}");
        }
        else
        {
            UmlDiagram.AppendLine($"\tclass \"{name}\" {{}}");
        }
    }

    private static bool IsIntefrace(string line)
    {
        return line.StartsWith("I") && line.Length > 1 && char.IsUpper(line[1]);
    }

    private static void AddDependenciesToDiagram()
    {
        var namespaceNames = new HashSet<string>(_namespaces.Select(n => n.Name));
        foreach (var namespaceFile in _namespaces)
        {
            foreach (var csFile in namespaceFile.CsFiles)
            {
                foreach (var usingFile in csFile.UsingsDirectories)
                {
                    if (namespaceNames.Contains(usingFile))
                    {
                        UmlDiagram.AppendLine($"\"{csFile.Name}\" --> {usingFile}");
                    }
                }
            }
        }
    }

    static public byte[] GenerateDiagramImage(string plantUmlText)
    {
        var rendererFactory = new RendererFactory();
        var renderer = rendererFactory.CreateRenderer();
        try
        {
            byte[] imageBytes = renderer.Render(plantUmlText, OutputFormat.Png);
            return imageBytes;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        return null;
    }

    public static void SetDiagram(StringBuilder newDiagram)
    {
        UmlDiagram = new StringBuilder(newDiagram.ToString());
        var imageBytes = GenerateDiagramImage(newDiagram.ToString());
        DiagramImage = ConvertByteToImage(imageBytes);
    }

    public static void SetDiagram(string newDiagram)
    {
        SetDiagram(new StringBuilder(newDiagram));
    }
}