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

public static class ClassHierarchy
{
    public static void CreateHierarchyDiagram(ref ImageSource diagramSource)
    {
        Analyzer.ClearData();
        var files = Analyzer.GetCsFiles();

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

                Analyzer.AddClass(Class);
            }
        }

        ExtractNamespaces();
        BuildHierarchyDiagram(ref diagramSource);
    }

    private static void ExtractNamespaces()
    {
        var classes = Analyzer.GetClasses();
        var namespaces = Analyzer.GetNamespaces();
        foreach (var @class in classes)
        {
            var classNamespace = @class.Namespace;
            if (namespaces.All(n => n.Name != classNamespace))
            {
                namespaces.Add(new NamespaceFile(classNamespace));
            }

            namespaces
                .First(n => n.Name == classNamespace)
                .ClassInfos.Add(@class);
        }
    }

    private static void BuildHierarchyDiagram(ref ImageSource diagramSource)
    {
        Analyzer.UmlDiagram.Clear();
        Analyzer.UmlDiagram.AppendLine("@startuml");
        Analyzer.UmlDiagram.AppendLine("skinparam linetype ortho");
        var namespaces = Analyzer.GetNamespaces();

        foreach (var @namespace in namespaces)
        {
            if (!string.IsNullOrEmpty(@namespace.Name))
            {
                Analyzer.UmlDiagram.AppendLine($"namespace {@namespace.Name} {{");
            }

            foreach (var classInfo in @namespace.ClassInfos)
            {
                // Build modifier string
                string modifiers = string.Join(" ", classInfo.Modifiers);

                // Add class definition
                if (modifiers.Contains("abstract"))
                {
                    Analyzer.UmlDiagram.AppendLine($"    abstract class \"{classInfo.Name}\" {{");
                }
                else if (modifiers.Contains("static"))
                {
                    Analyzer.UmlDiagram.AppendLine($"    class \"{classInfo.Name}\" <<static>> {{");
                }
                else
                {
                    Analyzer.UmlDiagram.AppendLine($"    class \"{classInfo.Name}\" {{");
                }

                foreach (var property in classInfo.Properties)
                {
                    Analyzer.UmlDiagram.AppendLine("     " + property);
                }

                foreach (var method in classInfo.Methods)
                {
                    Analyzer.UmlDiagram.AppendLine("    " + method);
                }

                Analyzer.UmlDiagram.AppendLine("}");
            }


            // Close namespace container
            if (!string.IsNullOrEmpty(@namespace.Name))
            {
                Analyzer.UmlDiagram.AppendLine("}");
            }
        }

        foreach (var @namespace in namespaces)
        {
            foreach (var classInfo in @namespace.ClassInfos)
            {
                // Add inheritance relationship
                if (!string.IsNullOrEmpty(classInfo.BaseClass))
                {
                    Analyzer.UmlDiagram.AppendLine($"\"{classInfo.Name}\" --|> \"{classInfo.BaseClass}\"");
                }
            }
        }

        Analyzer.UmlDiagram.AppendLine("@enduml");
        Console.WriteLine(Analyzer.UmlDiagram.ToString());

        var imageBytes = Analyzer.GenerateByteDiagramImage(Analyzer.UmlDiagram);
        diagramSource = Analyzer.ConvertByteToImage(imageBytes);
    }
}