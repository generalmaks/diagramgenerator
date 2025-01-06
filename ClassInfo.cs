namespace OopRgr;

  public class ModifierInfo
    {
        public bool IsPublic { get; set; }
        public bool IsPrivate { get; set; }
        public bool IsInternal { get; set; }
        public bool IsProtected { get; set; }
        public bool IsAbstract { get; set; }
        public bool IsSealed { get; set; }
        public bool IsStatic { get; set; }
        public bool IsPartial { get; set; }
        public bool IsNew { get; set; }
        public bool IsUnsafe { get; set; }

        public List<string> GetActiveModifiers()
        {
            var modifiers = new List<string>();
            if (IsPublic) modifiers.Add("public");
            if (IsPrivate) modifiers.Add("private");
            if (IsInternal) modifiers.Add("internal");
            if (IsProtected) modifiers.Add("protected");
            if (IsAbstract) modifiers.Add("abstract");
            if (IsSealed) modifiers.Add("sealed");
            if (IsStatic) modifiers.Add("static");
            if (IsPartial) modifiers.Add("partial");
            if (IsNew) modifiers.Add("new");
            if (IsUnsafe) modifiers.Add("unsafe");
            return modifiers;
        }
    }


public class ClassInfo
{
    public string ClassName { get; set; }
    public string BaseClass { get; set; }
    public List<string> Interfaces { get; set; } = new List<string>();
    public string FullDeclaration { get; set; }
    public ModifierInfo Modifiers { get; set; } = new ModifierInfo();
    public string GenericParameters { get; set; }

    public override string ToString()
    {
        var parts = new List<string>();

        // Add modifiers
        parts.AddRange(Modifiers.GetActiveModifiers());

        // Add class name and generic parameters
        parts.Add("class");
        parts.Add(ClassName + GenericParameters);

        // Add inheritance
        var inheritance = new List<string>();
        if (!string.IsNullOrEmpty(BaseClass)) inheritance.Add(BaseClass);
        inheritance.AddRange(Interfaces);

        if (inheritance.Any())
        {
            parts.Add(":");
            parts.Add(string.Join(", ", inheritance));
        }

        return string.Join(" ", parts);
    }
}