namespace DiagramGenerator;

public class ClassInfo
{
    public string Namespace { get; set; }
    public string Name { get; set; }
    public List<string> Modifiers { get; set; } = new List<string>();
    public List<string> Properties { get; set; } = new List<string>();
    public List<string> Methods { get; set; } = new List<string>();
    public string BaseClass { get; set; }
    public List<string> Interfaces { get; set; } = new List<string>();
    public List<string> Constructors { get; set; } = new List<string>();

    public void Print()
    {
        Console.WriteLine($"Class Name: {Name}");
        Console.WriteLine($"Namespace: {Namespace}");
        Console.WriteLine($"Modifiers: {string.Join(", ", Modifiers)}");
        Console.WriteLine($"Base Class: {BaseClass}");
        Console.WriteLine($"Interfaces: {string.Join(", ", Interfaces)}");
        Console.WriteLine("Properties: " + string.Join(", ", Properties));
        Console.WriteLine("Methods: " + string.Join(", ", Methods));
        Console.WriteLine("Constructors: " + string.Join(", ", Constructors));
    }
}