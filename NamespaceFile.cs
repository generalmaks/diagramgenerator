namespace OopRgr;

public class NamespaceFile
{
    public string Name { get; set; }
    public List<CsFile> CsFiles = new List<CsFile>();
    public NamespaceFile(string name, CsFile csfile)
    {
        Name = name;
        CsFiles.Add(csfile);
    }
}