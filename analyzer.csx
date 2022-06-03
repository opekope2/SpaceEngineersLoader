#!/usr/bin/env dotnet-script
#r "nuget: System.CommandLine, 2.0.0-beta3.22114.1"
#r "nuget: System.CommandLine.NamingConventionBinder, 2.0.0-beta3.22114.1"
#r "nuget: Mono.Cecil, 0.11.4"

#load "data.csx"

using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Mono.Cecil;

RootCommand cmd = new RootCommand("Space Engineers Dependency Portability Analyzer");
Command analyze = new Command("analyze", "Analyze input assemblies")
{
    new Option<FileSystemInfo>(new[] { "-a", "--assembly" }, "Add an assembly from a file or all assemblies from a folder") { Arity = ArgumentArity.OneOrMore },
    new Option<FileSystemInfo>(new[] { "-f", "--find" }, "Find references in analyzed assemblies") { Arity = ArgumentArity.OneOrMore },
    new Option<FileInfo>(new[] { "-o", "--output" }, "Set output file"),
    new Option<bool>(new[] { "-p", "--pinvoke" }, "List all P/Invoke uses"),
    //new Option<bool>(new[] { "-d", "--dependencies" }, "Analyze dependencies as well"),
};
analyze.Handler = CommandHandler.Create(Analyze);

cmd.AddCommand(analyze);
cmd.Invoke(Args.ToArray());

void Analyze(List<FileSystemInfo> assembly, List<string> find, FileInfo output, bool pinvoke)
{
    Data_Analysis analysis = new Data_Analysis
    {
        Assemblies = new List<Data_AssemblyDefinition>()
    };

    foreach (var asm in assembly)
    {
        if (asm is FileInfo file && (file.Extension == ".exe" || file.Extension == ".dll"))
        {
            try
            {
                AnalyzeAssembly(file, analysis, find, pinvoke);
            }
            catch (BadImageFormatException)
            {
                Console.Error.WriteLine($"Error loading {file}!"); // Todo logger
            }
        }
        else if (asm is DirectoryInfo dir)
        {
            foreach (var file2 in dir.GetFiles())
            {
                try
                {
                    if ((file2.Extension == ".exe" || file2.Extension == ".dll"))
                    {
                        AnalyzeAssembly(file2, analysis, find, pinvoke);
                    }
                }
                catch (BadImageFormatException)
                {
                    Console.Error.WriteLine($"Error loading {file2}!"); // Todo logger
                }
            }
        }
    }

    if (output == null || output.Name == "-")
    {
        Console.WriteLine(JsonSerializer.Serialize(analysis, new JsonSerializerOptions { WriteIndented = true }));
    }
    else
    {
        File.WriteAllBytes(output.FullName, JsonSerializer.SerializeToUtf8Bytes(analysis, new JsonSerializerOptions { WriteIndented = true }));
    }
}

void AnalyzeAssembly(FileInfo file, Data_Analysis analysis, List<string> find, bool pinvoke)
{
    AssemblyDefinition def = AssemblyLoader.FindAndLoad(file);
    if (!analysis.Assemblies.Any(a => a.Name == def.FullName))
    {
        analysis.Assemblies.Add(AnalyzeAssembly(def, find, pinvoke));
    }
}

Data_AssemblyDefinition AnalyzeAssembly(AssemblyDefinition assembly, List<string> find, bool pinvoke)
{
    string framework = assembly.CustomAttributes.Where(attr => attr.AttributeType.FullName == "System.Runtime.Versioning.TargetFrameworkAttribute").FirstOrDefault()
        ?.ConstructorArguments[0].Value as string;
    Console.Error.WriteLine($"Analyzing {assembly.FullName} ({framework})"); // Todo logger

    Data_AssemblyDefinition def = new Data_AssemblyDefinition
    {
        File = null,
        Name = assembly.FullName,
        TargetFramework = framework,
        Types = new List<Data_TypeDefinition>()
    };

    foreach (var type in assembly.MainModule.Types)
    {
        Data_TypeDefinition typeAnalysis = AnalyzeType(type, find, pinvoke);
        if (typeAnalysis.Fields.Count > 0 || typeAnalysis.Methods.Count > 0)
        {
            def.Types.Add(typeAnalysis);
        }
    }

    return def;
}

Data_TypeDefinition AnalyzeType(TypeDefinition type, List<string> find, bool pinvoke)
{
    Data_TypeDefinition def = new Data_TypeDefinition
    {
        Name = type.FullName,
        Fields = new List<Data_MemberDefinition>(),
        Methods = new List<Data_MemberDefinition>()
    };

    foreach (var field in type.Fields)
    {
        Data_MemberDefinition fieldAnalysis = AnalyzeField(field, find);
        if (fieldAnalysis.Matches.Count > 0 || fieldAnalysis.Uses.Count > 0)
        {
            def.Methods.Add(fieldAnalysis);
        }
    }

    foreach (var method in type.Methods)
    {
        Data_MemberDefinition methodAnalysis = AnalyzeMethod(method, find, pinvoke);
        if (methodAnalysis.Matches.Count > 0 || methodAnalysis.Uses.Count > 0)
        {
            def.Methods.Add(methodAnalysis);
        }
    }

    return def;
}

Data_MemberDefinition AnalyzeField(FieldDefinition field, List<string> find)
{
    Data_MemberDefinition def = new Data_MemberDefinition
    {
        Name = field.Name,
        Uses = new List<Data_Uses>(),
        Matches = new List<string>()
    };

    foreach (var target in find)
    {
        if (field.FieldType.Name.Contains(target))
        {
            def.Uses.Add(new Data_Uses
            {
                AssemblyName = field.FieldType.Module.Assembly.FullName,
                Type = field.FieldType.FullName,
                Method = null
            });
            def.Matches.Add(target);
        }
    }

    return def;
}

Data_MemberDefinition AnalyzeMethod(MethodDefinition method, List<string> find, bool pinvoke)
{
    Data_MemberDefinition def = new Data_MemberDefinition
    {
        Name = method.Name,
        Uses = new List<Data_Uses>(),
        Matches = new List<string>()
    };

    if (pinvoke && method.IsPInvokeImpl && method.HasPInvokeInfo)
    {
        PInvokeInfo pinvInfo = method.PInvokeInfo;
        if (pinvInfo == null)
        {
            // Todo
            return def;
        }
        def.Uses.Add(new Data_Uses
        {
            AssemblyName = pinvInfo.Module.Name,
            Type = null,
            Method = new Data_MethodReference
            {
                Name = pinvInfo.EntryPoint,
                Parameters = method.Parameters.Select(p => new Data_TypeReference
                {
                    AssemblyName = p.ParameterType.Module.Assembly.FullName,
                    Type = p.ParameterType.FullName,
                    Method = null
                }).ToList(),
                ReturnType = new Data_TypeReference
                {
                    AssemblyName = method.ReturnType.Module.Assembly.FullName,
                    Type = method.ReturnType.FullName,
                    Method = null
                },
                IsPInvoke = true
            }
        });
    }
    if (!method.HasBody)
    {
        return def;
    }

    def = AnalyzeMethod(method, find);

    foreach (var target in find)
    {
        foreach (var variable in method.Body.Variables)
        {
            TypeReference varType = variable.VariableType;
            if (varType.FullName.Contains(target))
            {
                AddIfNew(new Data_Uses
                {
                    AssemblyName = varType.Module.Assembly.FullName,
                    Type = varType.FullName,
                    Method = null
                }, target);
            }
        }
    }

    IEnumerable<Data_Uses> uses = def.Uses;
    IEnumerable<string> matches = def.Matches;

    foreach (var instr in method.Body.Instructions)
    {
        if (instr.Operand is MethodReference m)
        {
            Data_MemberDefinition methodAnalysis = AnalyzeMethod(m, find);
            uses = uses.Union(methodAnalysis.Uses, Data_MemberDefinition_Comparer.GlobalInstance);
            matches = matches.Union(methodAnalysis.Matches);
        }
    }

    def.Uses = uses.ToList();
    def.Matches = matches.ToList();

    return def;

    void AddIfNew(Data_Uses usage, string match)
    {
        if (!def.Uses.Any(u => u.AssemblyName == usage.AssemblyName && u.Type == usage.Type && u.Method?.Name == usage.Method?.Name))
        {
            def.Uses.Add(usage);
        }
        if (!def.Matches.Contains(match))
        {
            def.Matches.Add(match);
        }
    }
}

class Data_MemberDefinition_Comparer : IEqualityComparer<Data_Uses>
{
    public static readonly Data_MemberDefinition_Comparer GlobalInstance = new Data_MemberDefinition_Comparer();

    public bool Equals(Data_Uses x, Data_Uses y)
    {
        return x.AssemblyName == y.AssemblyName && x.Type == y.Type && x.Method?.Name == y.Method?.Name;
    }

    public int GetHashCode([DisallowNull] Data_Uses obj)
    {
        return obj.AssemblyName.GetHashCode() * 17 + obj.Type.GetHashCode();
    }
}

Data_MemberDefinition AnalyzeMethod(MethodReference method, List<string> find)
{
    Data_MemberDefinition def = new Data_MemberDefinition
    {
        Name = method.Name,
        Uses = new List<Data_Uses>(),
        Matches = new List<string>()
    };

    foreach (var target in find)
    {
        if (method.Name.Contains(target) || method.ReturnType.Name.Contains(target))
        {
            def.Matches.Add(target);
            def.Uses.Add(new Data_Uses
            {
                AssemblyName = method.Module.Assembly.FullName,
                Type = method.DeclaringType.FullName,
                Method = new Data_MethodReference
                {
                    Name = method.Name,
                    Parameters = method.Parameters.Select(p => new Data_TypeReference
                    {
                        AssemblyName = p.ParameterType.Module.Assembly.FullName,
                        Type = p.ParameterType.FullName,
                        Method = null
                    }).ToList(),
                    ReturnType = new Data_TypeReference
                    {
                        AssemblyName = method.ReturnType.Module.Assembly.FullName,
                        Type = method.ReturnType.FullName,
                        Method = null
                    },
                    IsPInvoke = false
                }
            });
        }
        foreach (var parameter in method.Parameters)
        {
            TypeReference paramType = parameter.ParameterType;
            if (paramType.FullName.Contains(target))
            {
                def.Matches.Add(target);
                def.Uses.Add(new Data_Uses
                {
                    AssemblyName = paramType.Module.Assembly.FullName,
                    Type = paramType.FullName,
                    Method = null
                });
            }
        }
    }

    return def;
}

static class AssemblyLoader
{
    public static AssemblyDefinition FindAndLoad(FileInfo assembly)
    {
        FileInfo asmPath = Find(assembly);
        return asmPath == null ? null : AssemblyDefinition.ReadAssembly(asmPath.FullName);
    }

    public static FileInfo Find(FileInfo assembly)
    {
        if (assembly.Exists)
        {
            return assembly;
        }
        foreach (var path in Environment.GetEnvironmentVariable("PATH").Split(Path.PathSeparator))
        {
            string found = Path.Combine(path, assembly.Name);
            if (File.Exists(found))
            {
                return new FileInfo(found);
            }
        }
        return null;
    }
}
