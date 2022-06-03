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
Command listPinvoke = new Command("list-pinvokes", "Lists P/Invoke calls to a library => function array map.")
{
    new Argument("analysis", "Output of analyzer.csx"),
    new Option<FileInfo>(new[] { "-o", "--output" }, "Set output file"),
};
listPinvoke.Handler = CommandHandler.Create(ProcessArgs);
cmd.AddCommand(listPinvoke);

cmd.Invoke(Args.ToArray());

void ProcessArgs(string analysis, FileInfo output, bool pinvoke)
{
    FileInfo analysis2 = new FileInfo(analysis);

    if (!analysis2.Exists)
    {
        return;
        // Todo
    }

    using FileStream stream = File.OpenRead(analysis2.FullName);
    Data_Analysis a = JsonSerializer.Deserialize<Data_Analysis>(stream);

    Dictionary<string, List<string>> map = new Dictionary<string, List<string>>();

    foreach (var asm in a.Assemblies)
    {
        foreach (var type in asm.Types)
        {
            foreach (var field in type.Fields)
            {
                foreach (var use in field.Uses)
                {
                    if (use.Method?.IsPInvoke == true)
                    {
                        if (!map.ContainsKey(use.AssemblyName))
                        {
                            map[use.AssemblyName] = new List<string>();
                        }
                        if (!map[use.AssemblyName].Contains(use.Method.Name))
                        {
                            map[use.AssemblyName].Add(use.Method.Name);
                        }
                    }
                }
            }
            foreach (var method in type.Methods)
            {
                foreach (var use in method.Uses)
                {
                    if (use.Method?.IsPInvoke == true)
                    {
                        if (!map.ContainsKey(use.AssemblyName))
                        {
                            map[use.AssemblyName] = new List<string>();
                        }
                        if (!map[use.AssemblyName].Contains(use.Method.Name))
                        {
                            map[use.AssemblyName].Add(use.Method.Name);
                        }
                    }
                }
            }
        }
    }

    if (output == null || output.Name == "-")
    {
        Console.WriteLine(JsonSerializer.Serialize(map, new JsonSerializerOptions { WriteIndented = true }));
    }
    else
    {
        File.WriteAllBytes(output.FullName, JsonSerializer.SerializeToUtf8Bytes(map, new JsonSerializerOptions { WriteIndented = true }));
    }
}
