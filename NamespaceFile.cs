namespace DiagramGenerator;

public class NamespaceFile
{
    public string Name { get; set; }
    public List<CsFile> CsFiles = new List<CsFile>();
    public List<ClassInfo> ClassInfos = new List<ClassInfo>();
    public NamespaceFile(string name, CsFile csfile)
    {
        Name = name;
        CsFiles.Add(csfile);
    }

    public NamespaceFile(string name)
    {
        Name = name;
    }
}