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

        foreach (var file in files)
        {
            var fileText = File.ReadAllText(file);

            var syntaxTree = CSharpSyntaxTree.ParseText(fileText);
            var root = syntaxTree.GetRoot();
            var classDeclarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
            foreach (var classDeclaration in classDeclarations)
            {
                var Class = new ClassInfo();
                Console.WriteLine("=== Class Analysis ===");

                Class.Name = classDeclaration.Identifier.Text;
                Console.WriteLine($"Class Name: {classDeclaration.Identifier.Text}");


                var compilatedRoot = syntaxTree.GetCompilationUnitRoot();
                var namespaceDeclarations = root.DescendantNodes()
                    .OfType<FileScopedNamespaceDeclarationSyntax>();
                Class.Namespace = namespaceDeclarations.FirstOrDefault()?.Name.ToString() ?? string.Empty;
                Console.WriteLine($"Namespace: {Class.Namespace}");

                if (classDeclaration.BaseList != null)
                {
                    var baseTypes = classDeclaration.BaseList.Types
                        .Select(t => t.ToString());
                    foreach (var baseType in baseTypes)
                    {
                        if (baseType.StartsWith("I"))
                            Class.Interfaces.Add(baseType);
                        else
                            Class.BaseClass = baseType;
                    }

                    Console.WriteLine("Base Classes/Interfaces: " + string.Join(", ", baseTypes));
                }
                else
                {
                    Console.WriteLine("Base Classes/Interfaces: None");
                }

                Console.WriteLine("Fields:");
                foreach (var field in classDeclaration.Members.OfType<FieldDeclarationSyntax>())
                {
                    var fieldType = field.Declaration.Type;
                    foreach (var variable in field.Declaration.Variables)
                    {
                        Class.Properties.Add(variable.Identifier.Text);
                        Console.WriteLine($"  Name: {variable.Identifier.Text}, Type: {fieldType}");
                    }
                }

                Console.WriteLine("Properties:");
                foreach (var property in classDeclaration.Members.OfType<PropertyDeclarationSyntax>())
                {
                    Class.Properties.Add(property.Identifier.Text);
                    Console.WriteLine($"  Name: {property.Identifier.Text}, Type: {property.Type}");
                }

                Console.WriteLine("Methods:");
                foreach (var method in classDeclaration.Members.OfType<MethodDeclarationSyntax>())
                {
                    var parameters = string.Join(", ", method.ParameterList.Parameters
                        .Select(p => $"{p.Type} {p.Identifier.Text}"));
                    Class.Methods.Add($"{method.ReturnType} {method.Identifier.Text}");
                    Console.WriteLine(
                        $"  Name: {method.Identifier.Text}, Return Type: {method.ReturnType}, Parameters: ({parameters})");
                }

                Console.WriteLine("Constructors:");
                foreach (var constructor in classDeclaration.Members.OfType<ConstructorDeclarationSyntax>())
                {
                    var parameters = string.Join(", ", constructor.ParameterList.Parameters
                        .Select(p => $"{p.Type} {p.Identifier.Text}"));
                    Class.Constructors.Add($"{constructor.Modifiers} {constructor.Identifier.Text} {parameters}");
                    Console.WriteLine($"  Name: {constructor.Identifier.Text}, Parameters: ({parameters})");
                }

                Classes.Add(Class);
            }
        }

        foreach (var @class in Classes)
        {
            @class.Print();
        }

        ExtractNamespaces();
        foreach (var name in Namespaces)
        {
            Console.WriteLine(name.Name);
        }

        BuildHierarchyDiagram(ref diagramSource);
    }

    static private void ExtractNamespaces()
    {
        foreach (var @class in Classes)
        {
            var classNamespace = @class.Namespace;
            if (Namespaces.All(n => n.Name != classNamespace))
            {
                Namespaces.Add(new NamespaceFile(classNamespace));
            }

            Namespaces
                .First(n => n.Name == classNamespace)
                .ClassInfos.Add(@class);
        }
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
                    UmlDiagram.AppendLine($"    abstract class \"{classInfo.Name}\" {{");
                }
                else if (modifiers.Contains("static"))
                {
                    UmlDiagram.AppendLine($"    class \"{classInfo.Name}\" <<static>> {{");
                }
                else
                {
                    UmlDiagram.AppendLine($"    class \"{classInfo.Name}\" {{");
                }

                foreach (var property in classInfo.Properties)
                {
                    UmlDiagram.AppendLine("     " + property);
                }

                foreach (var method in classInfo.Methods)
                {
                    UmlDiagram.AppendLine("    " + method);
                }
                UmlDiagram.AppendLine("}");
            }
            

            // Close namespace container
            if (!string.IsNullOrEmpty(@namespace.Name))
            {
                UmlDiagram.AppendLine("}");
            }
        }

        foreach (var @namespace in Namespaces)
        {
            foreach (var classInfo in @namespace.ClassInfos)
            {
                // Add inheritance relationship
                if (!string.IsNullOrEmpty(classInfo.BaseClass))
                {
                    UmlDiagram.AppendLine($"\"{classInfo.Name}\" --|> \"{classInfo.BaseClass}\"");
                }
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