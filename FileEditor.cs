using System.IO;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace DiagramGenerator;

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

    public static void SaveImage()
    {
        var saveDialog = new SaveFileDialog()
        {
            Title = "Виберіть місце, куди ви хочете зберегти зображення",
            Filter = "Png(*.png)|*.png"
        };
        if (saveDialog.ShowDialog() == true)
        {
            _filePath = saveDialog.FileName;
            var bitmapSource = Analyzer.DiagramImage as BitmapSource;
            if (bitmapSource != null)
            {
                // Save the image as a PNG file
                using (var fileStream = new FileStream(_filePath, FileMode.Create))
                {
                    BitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                    encoder.Save(fileStream);
                }

                Console.WriteLine("Image saved successfully");
            }
        }
    }
}