namespace OopRgr;

public class CsFile
{
    private string Name;
    private string Namespace;
    private List<string> UsingsDirectories = new List<string>();

    public CsFile()
    {
    }

    public void AddUsing(string @using)
    {
        @using = @using.Replace("using ", "");
        UsingsDirectories.Add(@using);
    }

    public void SetName(string name)
    {
        Name = name;
    }

    public void SetNamespace(string @namespace)
    {
        Namespace = @namespace.Replace("namespace", "").Trim();
    }

    public void Print()
    {
        Console.WriteLine($"Name {Name}, Namespace {Namespace}");
        Console.WriteLine("Using directives: {");
        foreach (var usingDirective in UsingsDirectories)
        {
            Console.WriteLine(usingDirective);
        }
        Console.WriteLine("}");
    }
}
