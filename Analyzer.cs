using PlantUml.Net;
using System.IO;
using System.Text;
using Microsoft.Win32;

namespace OopRgr;

static public class Analyzer
{
    private static string _filePath;
    private static List<NamespaceFile> _namespaces = new List<NamespaceFile>();
    private static List<CsFile> _csFiles = new List<CsFile>();
    private static StringBuilder _umlDiagram = new StringBuilder();

    static public void GetProject()
    {
        var openDialog = new OpenFolderDialog() { Title = "Виберіть папку з вашим проектом" };
        if (openDialog.ShowDialog() == true)
        {
            _filePath = openDialog.FolderName;
            Console.WriteLine($"Opened from {_filePath}");
            Analyze();
        }
        else
        {
            Console.WriteLine("No folder selected.");
        }
    }

    static private void Analyze()
    {
        _csFiles.Clear();
        List<string> files = Directory.GetFiles(_filePath, "*.cs", SearchOption.AllDirectories).ToList();
        files = files.Except(files.Where(f => f.Contains("Debug"))).ToList();
        files = files.Except(files.Where(f => f.Contains("Release"))).ToList();
        Console.WriteLine("######################################");
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
                        string nameSpaceName = line.Replace("namespace ", "").Trim();
                        nameSpaceName = nameSpaceName.Replace(";", "").Trim();
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

    static private void WriteDiagramFile()
    {
        _umlDiagram.Clear();
        _umlDiagram.AppendLine("@startuml");
        var namespaceNames = new HashSet<string>(_namespaces.Select(n => n.Name));
        foreach (var namespaceFile in _namespaces)
        {
            Console.WriteLine(namespaceFile.Name + ":");
            _umlDiagram.AppendLine($"namespace {namespaceFile.Name} {{\n");
            foreach (var csFile in namespaceFile.CsFiles)
            {
                Console.WriteLine("\t" + csFile.Name);
                _umlDiagram.AppendLine($"\tclass {csFile.Name} {{}}\n");
            }

            _umlDiagram.AppendLine("}");
        }

        foreach (var namespaceFile in _namespaces)
        {
            foreach (var csFile in namespaceFile.CsFiles)
            {
                foreach (var usingFile in csFile.UsingsDirectories)
                {
                    if (namespaceNames.Contains(usingFile))
                    {
                        _umlDiagram.AppendLine($"{csFile.Name} --> {usingFile}\n");
                    }
                }
            }
        }

        _umlDiagram.AppendLine("@enduml");
        Console.WriteLine(_umlDiagram);
    }

    static public byte[] GenerateDiagramImage(string plantUmlText)
    {
        var rendererFactory = new RendererFactory();
        var renderer = rendererFactory.CreateRenderer();

        byte[] imageBytes = renderer.Render(plantUmlText, OutputFormat.Png);
        return imageBytes;
    }
}