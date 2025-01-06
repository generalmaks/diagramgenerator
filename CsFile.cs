namespace DiagramGenerator;

public class CsFile
{
    public string Name;
    public List<string> UsingsDirectories = new List<string>();

    public CsFile()
    {
    }

    public void AddUsing(string @using)
    {
        @using = @using.Replace("using ", "");
        @using = @using.Replace(";", "");
        UsingsDirectories.Add(@using);
    }

    public void SetName(string name)
    {
        Name = name;
    }


    public void Print()
    {
        Console.WriteLine($"Name {Name}");
        Console.WriteLine("Using directives: {");
        foreach (var usingDirective in UsingsDirectories)
        {
            Console.WriteLine(usingDirective);
        }
        Console.WriteLine("}");
    }
}
