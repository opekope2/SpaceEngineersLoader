public class Data_AssemblyDefinition
{
    public string File { get; set; }
    public string Name { get; set; }
    public string TargetFramework { get; set; }
    public List<Data_TypeDefinition> Types { get; set; }
}

public class Data_MemberDefinition
{
    public string Name { get; set; }
    public List<Data_Uses> Uses { get; set; }
    public List<string> Matches { get; set; }
}

public class Data_MethodReference
{
    public string Name { get; set; }
    public List<Data_TypeReference> Parameters { get; set; }
    public Data_TypeReference ReturnType { get; set; }
    public bool IsPInvoke { get; set; }
}

public class Data_TypeReference
{
    public string AssemblyName { get; set; }
    public string Type { get; set; }
    public Data_MemberDefinition Method { get; set; }
}

public class Data_Analysis
{
    public List<Data_AssemblyDefinition> Assemblies { get; set; }
}

public class Data_TypeDefinition
{
    public string Name { get; set; }
    public List<Data_MemberDefinition> Fields { get; set; }
    public List<Data_MemberDefinition> Methods { get; set; }
}

public class Data_Uses
{
    public string AssemblyName { get; set; }
    public string Type { get; set; }
    public Data_MethodReference Method { get; set; }
}
