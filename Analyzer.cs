using PlantUml.Net;
using System.IO;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using System.Text.RegularExpressions;
using System.Windows;

namespace OopRgr;

public static class Analyzer
{
    private static string? _filePath;
    private static readonly List<NamespaceFile> Namespaces = new List<NamespaceFile>();
    private static readonly List<CsFile> _csFiles = new List<CsFile>();
    private static readonly List<ClassInfo> Classes = new List<ClassInfo>();
    public static StringBuilder UmlDiagram = new StringBuilder();
    public static ImageSource DiagramImage;

    public static void GetProject()
    {
        var openDialog = new OpenFolderDialog() { Title = "Виберіть папку з вашим проектом" };
        if (openDialog.ShowDialog() == true)
        {
            _filePath = openDialog.FolderName;
            Console.WriteLine($"Opened from {_filePath}");
            MessageBox.Show($"Відкрито папку {_filePath}");
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

    public static void CreateHierarchyDiagram(ref ImageSource diagramSource)
    {
        ClearData();
        var files = GetCsFiles(_filePath);

        const string classPattern =
            @"(?<modifiers>(?:(?:public|private|internal|protected|abstract|sealed|static|partial|new|unsafe)\s+)*)?class\s+(?<className>[a-zA-Z_][a-zA-Z0-9_]*)\s*(?<generic><[^>]+>)?\s*(?::\s*(?<inheritance>(?:[a-zA-Z_][a-zA-Z0-9_]*(?:<[^>]+>)?(?:\s*,\s*[a-zA-Z_][a-zA-Z0-9_]*(?:<[^>]+>)?)*))?)?\s*{";
        foreach (var file in files)
        {
            var matches = Regex.Matches(File.ReadAllText(file), classPattern, RegexOptions.Multiline);
            foreach (Match match in matches)
            {
                var classInfo = new ClassInfo
                {
                    ClassName = match.Groups["className"].Value,
                    FullDeclaration = match.Value.Trim(),
                    GenericParameters = match.Groups["generic"].Value
                };

                var modifiersStr = match.Groups["modifiers"].Value.ToLower();
                classInfo.Modifiers.IsPublic = modifiersStr.Contains("public");
                classInfo.Modifiers.IsPrivate = modifiersStr.Contains("private");
                classInfo.Modifiers.IsInternal = modifiersStr.Contains("internal");
                classInfo.Modifiers.IsProtected = modifiersStr.Contains("protected");
                classInfo.Modifiers.IsAbstract = modifiersStr.Contains("abstract");
                classInfo.Modifiers.IsSealed = modifiersStr.Contains("sealed");
                classInfo.Modifiers.IsStatic = modifiersStr.Contains("static");
                classInfo.Modifiers.IsPartial = modifiersStr.Contains("partial");
                classInfo.Modifiers.IsNew = modifiersStr.Contains("new");
                classInfo.Modifiers.IsUnsafe = modifiersStr.Contains("unsafe");

                var inheritance = match.Groups["inheritance"].Value;
                if (!string.IsNullOrEmpty(inheritance))
                {
                    var inheritedTypes = inheritance.Split(',')
                        .Select(t => t.Trim())
                        .Where(t => !string.IsNullOrEmpty(t))
                        .ToList();

                    if (inheritedTypes.Any())
                    {
                        if (!inheritedTypes[0].StartsWith("I"))
                        {
                            classInfo.BaseClass = inheritedTypes[0];
                            inheritedTypes.RemoveAt(0);
                        }

                        classInfo.Interfaces.AddRange(inheritedTypes);
                    }
                }
                Console.WriteLine($"Class: {classInfo}");
                Classes.Add(classInfo);
            }
        }
    }

    static public void AnalyzeProjectComponents()
    {
        ClearData();
        var files = GetCsFiles(_filePath);

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

            _csFiles.Add(csFile);
        }
    }

    private static void ClearData()
    {
        Namespaces.Clear();
        _csFiles.Clear();
    }

    private static List<string> GetCsFiles(string? path)
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
        Console.WriteLine("Diagram has been made:\n" + UmlDiagram);
    }

    private static ImageSource ConvertByteToImage(byte[] imageBytes)
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

    static public byte[] GenerateByteDiagramImage(string plantUmlText)
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
}