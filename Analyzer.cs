using PlantUml.Net;
using System.IO;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DiagramGenerator;

public static class Analyzer
{
    private static string? _filePath;
    public static readonly List<ClassInfo> Classes = new List<ClassInfo>();
    private static readonly List<NamespaceFile> Namespaces = new List<NamespaceFile>();
    private static readonly List<CsFile> CsFiles = new List<CsFile>();
    private static readonly List<string> Interfaces = new List<string>();
    public static StringBuilder UmlDiagram = new StringBuilder();
    public static ImageSource DiagramImage;

    public static void GetProject()
    {
        var openDialog = new OpenFolderDialog() { Title = "Виберіть папку з вашим проектом" };
        if (openDialog.ShowDialog() == true)
        {
            _filePath = openDialog.FolderName;
            Console.WriteLine($"Opened from {_filePath}");
        }
        else
        {
            Console.WriteLine("No folder selected.");
        }
    }

    public static void CreateComponentsDiagram(ref ImageSource diagramSource)
    {
        AnalyzeProjectComponents();
        WriteDiagramFile();
        SetDiagram(UmlDiagram);
        diagramSource = DiagramImage;
    }


    static public void AnalyzeProjectComponents()
    {
        ClearData();
        var files = GetCsFiles();

        const string usingPattern = @"^using\s+([\w.]+);";
        const string namespacePattern = @"^namespace\s+([\w.]+)";
        foreach (var file in files)
        {
            var csFile = new CsFile();
            csFile.SetName(Path.GetFileNameWithoutExtension(file));

            string content = File.ReadAllText(file);
            foreach (Match match in Regex.Matches(content, usingPattern, RegexOptions.Multiline))
            {
                csFile.AddUsing(match.Groups[1].Value.Trim());
            }

            foreach (Match match in Regex.Matches(content, namespacePattern, RegexOptions.Multiline))
            {
                string nameSpaceName = match.Groups[1].Value.Trim();

                if (!Namespaces.Any(n => n.Name == nameSpaceName))
                {
                    NamespaceFile ns = new NamespaceFile(nameSpaceName, csFile);
                    Namespaces.Add(ns);
                }

                Namespaces
                    .FirstOrDefault(namespaceFile => namespaceFile.Name == nameSpaceName)?
                    .CsFiles.Add(csFile);
            }

            CsFiles.Add(csFile);
        }
    }

    public static void ClearData()
    {
        Namespaces.Clear();
        CsFiles.Clear();
        Classes.Clear();
    }

    public static List<string> GetCsFiles()
    {
        return Directory.GetFiles(_filePath, "*.cs", SearchOption.AllDirectories)
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
        Console.WriteLine("Diagram has been made:\n" + UmlDiagram);
    }

    public static ImageSource ConvertByteToImage(byte[] imageBytes)
    {
        var bitmap = new BitmapImage();
        using var ms = new MemoryStream(imageBytes);
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.StreamSource = ms;
        bitmap.EndInit();
        bitmap.Freeze();

        return bitmap;
    }

    private static void AddNamespacesToDiagram()
    {
        foreach (var namespaceFile in Namespaces)
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

        if (IsInterface(name))
        {
            UmlDiagram.AppendLine($"\tinterface \"{name}\"{{}}");
        }
        else
        {
            UmlDiagram.AppendLine($"\tclass \"{name}\" {{}}");
        }
    }

    private static bool IsInterface(string line)
    {
        return line.StartsWith("I") && line.Length > 1 && char.IsUpper(line[1]);
    }

    private static void AddDependenciesToDiagram()
    {
        var namespaceNames = new HashSet<string>(Namespaces.Select(n => n.Name));
        foreach (var namespaceFile in Namespaces)
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

    public static byte[] GenerateByteDiagramImage(string plantUmlText)
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

    public static byte[] GenerateByteDiagramImage(StringBuilder plantUmlText)
    {
        return GenerateByteDiagramImage(plantUmlText.ToString());
    }

    public static void SetDiagram(StringBuilder newDiagram)
    {
        UmlDiagram = new StringBuilder(newDiagram.ToString());
        var imageBytes = GenerateByteDiagramImage(newDiagram.ToString());
        DiagramImage = ConvertByteToImage(imageBytes);
    }

    public static void SetDiagram(string newDiagram)
    {
        SetDiagram(new StringBuilder(newDiagram));
    }

    public static string GetDiagram()
    {
        return UmlDiagram.ToString();
    }

    public static List<ClassInfo> GetClasses()
    {
        return Classes;
    }
    public static void AddClass(ClassInfo classes)
    {
        Classes.Add(classes);
    }

    public static List<NamespaceFile> GetNamespaces()
    {
        return Namespaces;
    }
}