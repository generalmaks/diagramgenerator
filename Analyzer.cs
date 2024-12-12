using PlantUml.Net;
using System.IO;
using Microsoft.Win32;

namespace OopRgr;

static public class Analyzer
{
    private static string _filePath;
    private static List<NamespaceFile> _namespaces = new List<NamespaceFile>();
    private static List<CsFile> _csFiles = new List<CsFile>();
    private static string _umlDiagram;
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
                        NamespaceFile ns = new NamespaceFile(nameSpaceName, csFile);
                    }
                }
            }

            _csFiles.Add(csFile);
        }

        foreach (var csfile in _csFiles)
        {
            csfile.Print();
        }
    }

    static private void WriteDiagramFile()
    {
        _umlDiagram.Insert(_umlDiagram.Length - 1, "@startuml/n");
    }
    static public byte[] GenerateDiagramImage(string plantUmlText)
    {
        var rendererFactory = new RendererFactory();
        var renderer = rendererFactory.CreateRenderer();

        byte[] imageBytes = renderer.Render(plantUmlText, OutputFormat.Png);
        return imageBytes;
    }
}