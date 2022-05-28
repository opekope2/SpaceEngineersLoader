#!/usr/bin/env dotnet-script
#r "nuget: System.CommandLine, 2.0.0-beta3.22114.1"
#r "nuget: System.CommandLine.NamingConventionBinder, 2.0.0-beta3.22114.1"

using System.CommandLine;
using System.CommandLine.NamingConventionBinder;

RootCommand cmd = new RootCommand("Space Engineers Dependency Portability Analyzer");
cmd.AddOption(new Option<FileSystemInfo>(new[] { "-a", "--assembly" }, "Add an assembly from a file or all assemblies from a folder") { Arity = ArgumentArity.OneOrMore });
cmd.AddOption(new Option<FileSystemInfo>(new[] { "-f", "--framework" }, "Add a framework assembly from a file or all framework assemblies from a folder") { Arity = ArgumentArity.OneOrMore });
cmd.AddOption(new Option<FileInfo>(new[] { "-o", "--output" }, "Set output file"));
cmd.Handler = CommandHandler.Create(ProcessArgs);
cmd.Invoke(Args.ToArray());

void ProcessArgs(List<FileSystemInfo> assembly, List<FileSystemInfo> framework, FileInfo output)
{
}
