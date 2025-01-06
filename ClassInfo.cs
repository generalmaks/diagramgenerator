namespace DiagramGenerator;

public class ClassInfo
{
    public string Namespace { get; set; }
    public string Name { get; set; }
    public List<string> Modifiers { get; set; }
    public string BaseClass { get; set; }
    public List<string> Interfaces { get; set; }

    public void Print()
    {
        Console.WriteLine($"Class name: {Name} | " +
                          $"Namespace: {Namespace} | " +
                          $"Modifiers: {string.Join(" ", Modifiers)} | " +
                          $"Inheritance: {BaseClass} | " +
                          $"Interfaces: {string.Join(" ", Interfaces)} | ");
    }
}