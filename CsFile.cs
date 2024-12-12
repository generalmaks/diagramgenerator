namespace OopRgr;

public class CsFile
{
    private string Name;
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
