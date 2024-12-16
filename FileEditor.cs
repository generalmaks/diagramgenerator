using System.IO;
using Microsoft.Win32;

namespace OopRgr;

public static class FileEditor
{
    private static string _filePath { get; set; }
    public static void SaveDiagramFile()
    {
        var saveDialog = new SaveFileDialog()
        {
            Title = "Виберіть місце, в яке ви хочете зберегти файл",
            Filter = "PlantUml(*.plantuml)|*.plantuml"
        };
        if (saveDialog.ShowDialog() == true)
        {
            _filePath = saveDialog.FileName;
            try
            {
                File.WriteAllText(_filePath, Analyzer.GetDiagram());
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}