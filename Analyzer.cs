using PlantUml.Net;
using System.IO;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using System.Text.RegularExpressions;
using System.Windows;

namespace DiagramGenerator;

public static class Analyzer
{
    private static string? _filePath;
    private static readonly List<NamespaceFile> Namespaces = new List<NamespaceFile>();
    private static readonly List<CsFile> CsFiles = new List<CsFile>();
    private static readonly List<ClassInfo> Classes = new List<ClassInfo>();
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

    public static void CreateHierarchyDiagram(ref ImageSource diagramSource)
    {
        ClearData();
        var files = GetCsFiles(_filePath);

        var classRegex = new Regex(
            @"(?<modifiers>(?:(?:public|private|internal|protected|abstract|sealed|static|partial|new|unsafe)\s+)*)?class\s+(?<className>[a-zA-Z_][a-zA-Z0-9_]*)\s*(?<generic><[^>]+>)?\s*(?::\s*(?<inheritance>(?:[a-zA-Z_][a-zA-Z0-9_]*(?:<[^>]+>)?(?:\s*,\s*[a-zA-Z_][a-zA-Z0-9_]*(?:<[^>]+>)?)*))?)?\s*{");
        var interfaceRegex = new Regex(
            @"(?<modifiers>(?:(?:public|private|internal|protected|partial|new|unsafe)\s+)*)" +
            @"interface\s+" +
            @"(?<interfaceName>[a-zA-Z_][a-zA-Z0-9_]*)" +
            @"(?<generic><[^>]+>)?\s*" +
            @"(?::\s*(?<inheritance>(?:[a-zA-Z_][a-zA-Z0-9_]*(?:<[^>]+>)?(?:\s*,\s*[a-zA-Z_][a-zA-Z0-9_]*(?:<[^>]+>)?)*))?)?" +
            @"\s*{");
        var namespacePattern = new Regex(@"^namespace\s+([\w.]+)");
        foreach (var file in files)
        {
            var fileText = File.ReadAllText(file);

            Match namespaceMatch = namespacePattern.Match(fileText);
            MatchCollection matches = classRegex.Matches(fileText);
            MatchCollection interfaces = interfaceRegex.Matches(fileText);
            foreach (Match match in interfaces)
            {
                Console.WriteLine(match.Value);
                Interfaces.Add(match.Value);
            }

            foreach (Match match in matches)
            {
                Console.WriteLine("================");
                var classNamespace = namespaceMatch.Value.Replace("namespace", "").Trim();

                var className = match.Groups["className"].Value;
                var classModifiers = match.Groups["modifiers"].ToString().Split(" ").ToList();
                var classBaseClass = match.Groups["inheritance"].Value;
                var classInterfaces = match.Groups["interface"].Value.Split(" ").ToList();

                classInterfaces = classInterfaces.Select(inter => inter == "" ? "Unknown" : inter).ToList();
                var classInfo = new ClassInfo
                {
                    Namespace = classNamespace,
                    Name = className,
                    Modifiers = classModifiers,
                    BaseClass = classBaseClass,
                    Interfaces = classInterfaces,
                };
                if (Namespaces.All(nameSpace => nameSpace.Name != classNamespace))
                {
                    var nameSpace = new NamespaceFile(classNamespace);
                    Namespaces.Add(nameSpace);
                }

                Namespaces
                    .FirstOrDefault(namespaceFile => namespaceFile.Name == classNamespace)?
                    .ClassInfos.Add(classInfo);

                classInfo.Print();
            }
        }

        foreach (var nameSpace in Namespaces)
        {
            Console.WriteLine(nameSpace.Name);
            foreach (var classInfo in nameSpace.ClassInfos)
            {
                Console.WriteLine($"\t{classInfo.Name}");
            }
        }

        BuildHierarchyDiagram(ref diagramSource);
    }

    static private void BuildHierarchyDiagram(ref ImageSource diagramSource)
    {
        UmlDiagram.Clear();
        UmlDiagram.AppendLine("@startuml");
        UmlDiagram.AppendLine("skinparam linetype ortho");

        foreach (var @namespace in Namespaces)
        {
            if (!string.IsNullOrEmpty(@namespace.Name))
            {
                UmlDiagram.AppendLine($"namespace {@namespace.Name} {{");
            }

            foreach (var classInfo in @namespace.ClassInfos)
            {
                // Build modifier string
                string modifiers = string.Join(" ", classInfo.Modifiers);

                // Add class definition
                if (modifiers.Contains("abstract"))
                {
                    UmlDiagram.AppendLine($"    abstract class \"{classInfo.Name}\" {{}}");
                }
                else if (modifiers.Contains("static"))
                {
                    UmlDiagram.AppendLine($"    class \"{classInfo.Name}\" <<static>> {{}}");
                }
                else
                {
                    UmlDiagram.AppendLine($"    class \"{classInfo.Name}\" {{}}");
                }

                // Add inheritance relationship
                if (!string.IsNullOrEmpty(classInfo.BaseClass))
                {
                    UmlDiagram.AppendLine($"\"{classInfo.Name}\" --|> \"{classInfo.BaseClass}\"");
                }
            }

            // Close namespace container
            if (!string.IsNullOrEmpty(@namespace.Name))
            {
                UmlDiagram.AppendLine("}");
            }
        }

        UmlDiagram.AppendLine("@enduml");
        Console.WriteLine(UmlDiagram.ToString());

        var imageBytes = GenerateByteDiagramImage(UmlDiagram.ToString());
        diagramSource = ConvertByteToImage(imageBytes);
        DiagramImage = diagramSource;
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

            CsFiles.Add(csFile);
        }
    }

    private static void ClearData()
    {
        Namespaces.Clear();
        CsFiles.Clear();
        Classes.Clear();
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