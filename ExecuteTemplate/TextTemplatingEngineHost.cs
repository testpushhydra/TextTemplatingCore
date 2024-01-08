using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;
using CloudIDEaaS.TemplateExecute;
using CloudIDEaaS.TextTemplatingCore.TextTemplatingCoreLib;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TextTemplating;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;
using Application = System.Windows.Forms.Application;

namespace TemplateExecute
{
    public class TextTemplatingEngineHost : ITextTemplatingEngineHost
    {
        private string templateFile;
        private string inputFile;
        private string outputFile;
        private string[] libraries;

        public ISessionLoadContext LoadContext { get; } = new LibraryLoadContext();

        public object GetHostOption(string optionName)
        {
            return null;
        }

        public IList<string> StandardAssemblyReferences
        {
            get
            {
                List<string> assemblyReferences = new List<string>()
                {
                    typeof (Uri).Assembly.Location,
                    "System",
                    "WindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35",
                    "System.Runtime, Version=4.2.2.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
                    "System.Collections, Version=4.1.2.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
                    "System.CodeDom",
                    "System.ComponentModel.TypeConverter",
                    "System.Runtime"
                };

                assemblyReferences.AddRange((IEnumerable<string>) libraries);
                return (IList<string>) assemblyReferences;
            }
        }

        public IList<string> StandardImports
        {
            get
            {
                List<string> standardImports = new List<string>() { "System" };

                return (IList<string>) standardImports;
            }
        }

        internal List<string> IncludeDirectories
        {
            get
            {
                var directory = new DirectoryInfo(Path.GetDirectoryName(templateFile));

                return new List<string> { directory.FullName };
            }
        }

        public string HostPath
        {
            get
            {
                return Path.GetFullPath(Assembly.GetExecutingAssembly().Location);
            }
        }

        public string TemplateFile { get; }

        public void Process(string[] args)
        {
            var textTemplatingInterfacesPath = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), @"Binaries\Microsoft.VisualStudio.TextTemplating.12.0\Microsoft.VisualStudio.TextTemplating.Interfaces.10.0.dll");

            Debugger.Launch();

            if (args.Length < 3)
            {
                throw new ArgumentException($"Need at least 3 arguments, found only {args.Length}.", nameof(args));
            }

            templateFile = UnescapeArg(args[0]);
            inputFile = UnescapeArg(args[1]);
            outputFile = UnescapeArg(args[2]);
            libraries = args.Length > 3 ? UnescapeArgs(args[3..]) : new string[0];

            // Directory.SetCurrentDirectory(Path.GetDirectoryName(templateFile));

            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            using (Engine engine = new Engine(this))
            {
                var file = new FileInfo(templateFile);

                TemplateContext templateContext = new TemplateContext(file);
                string contents = engine.ProcessTemplate(templateContext);
                File.WriteAllText(templateContext.GetOutputFileName(outputFile), contents, templateContext.OutputEncoding);
            }

        }

        private Assembly? CurrentDomain_AssemblyResolve(object? sender, ResolveEventArgs args)
        {
            var files = new DirectoryInfo("C:\\Program Files\\Microsoft Visual Studio\\2022\\Community\\MSBuild\\Microsoft\\VisualStudio\\v17.0\\TextTemplating").GetFiles();
            var name = args.Name.Substring(0, args.Name.IndexOf(","));
            string fullName;

            if (name.EndsWith(".resources"))
            {
                return null;
            }

            if (files.Any(f => Path.GetFileNameWithoutExtension(f.Name) == name))
            {
                var file = files.Single(f => Path.GetFileNameWithoutExtension(f.Name) == name);

                Debug.WriteLine("CurrentDomain_AssemblyResolve: Resolved '{0}' with '{1}'", name, file.FullName);

                return Assembly.LoadFrom(file.FullName);
            }

            fullName = ResolveAssemblyReference(name);

            if (fullName != null)
            {
                return Assembly.LoadFrom(fullName);
            }

            return null;
        }

        public bool LoadIncludeText(string requestFileName, out string content, out string location)
        {
            if (Path.IsPathRooted(requestFileName))
            {
                content = File.ReadAllText(requestFileName);
                location = requestFileName;

                return true;
            }

            foreach (var includeDirectory in this.IncludeDirectories)
            {
                string path = Path.Combine(includeDirectory, requestFileName);

                if (File.Exists(path))
                {
                    content = File.ReadAllText(path);
                    location = path;
                    return true;
                }
            }

            content = string.Empty;
            location = string.Empty;

            return false;
        }

        public void LogErrors(CompilerErrorCollection errors)
        {
            throw new NotImplementedException();
        }

        public string ResolveAssemblyReference(string assemblyReference)
        {
            if (assemblyReference.EndsWith(".resources"))
            {
                return null;
            }

            if (Path.IsPathRooted(assemblyReference))
            {
                return assemblyReference;
            }
            else
            {
                int index;
                string name;
                List<FileInfo> files;

                if (assemblyReference.Contains("$(SolutionDir"))
                {
                    assemblyReference = assemblyReference.Replace("$(SolutionDir)", @"%SolutionDir%\");
                    assemblyReference = Environment.ExpandEnvironmentVariables(assemblyReference);

                    if (Path.IsPathRooted(assemblyReference))
                    {
                        Debug.WriteLine("Resolved '{0}' with '{1}'", assemblyReference, assemblyReference);

                        return assemblyReference;
                    }
                }

                index = assemblyReference.IndexOf(",");
                name = assemblyReference.Substring(0, index == -1 ? assemblyReference.Length : index);

                files = new DirectoryInfo("C:\\Program Files\\Microsoft Visual Studio\\2022\\Community\\MSBuild\\Microsoft\\VisualStudio\\v17.0\\TextTemplating").GetFiles("*.dll", SearchOption.AllDirectories).ToList();

                files.AddRange(new DirectoryInfo(@"C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\PublicAssemblies").GetFiles("*.dll", SearchOption.AllDirectories));
                files.AddRange(new DirectoryInfo(@"C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\3.1.0\ref\netcoreapp3.1").GetFiles("*.dll", SearchOption.AllDirectories));

                if (files.Any(f => Path.GetFileNameWithoutExtension(f.Name) == name))
                {
                    var file = files.Single(f => Path.GetFileNameWithoutExtension(f.Name) == name);

                    Debug.WriteLine("ResolveAssemblyReference: Resolved '{0}' with '{1}'", name, file.FullName);

                    return file.FullName;
                }

                var nugetFolder = Environment.ExpandEnvironmentVariables($"%userprofile%\\.nuget\\packages");
                var packageFiles = new DirectoryInfo(nugetFolder).GetFiles($"{name}.dll", SearchOption.AllDirectories);

                if (packageFiles.Length == 1)
                {
                    files.Add(packageFiles.Single());
                }
                else if (packageFiles.Length > 0)
                {
                    var packageDirectories = packageFiles.Select(f => f.DirectoryName.Remove(0, nugetFolder.Length + 1)).Select(f => f.Substring(0, f.IndexOf(@"\"))).Distinct();
                    var rootName = name;
                    string packageRootDirectory = null;
                    var rename = ConfigurationManager.AppSettings[name];

                    if (rename != null)
                    {
                        var libDirectory = new DirectoryInfo(Path.Combine(nugetFolder, rename));

                        if (libDirectory.Exists)
                        {
                            files = libDirectory.GetFiles($"{name}.dll").ToList();

                            if (files.Count == 1)
                            {
                                var file = files.Single();

                                Debug.WriteLine("ResolveAssemblyReference: Resolved '{0}' with '{1}'", name, file.FullName);

                                return file.FullName;
                            }
                            else
                            {
                                return null;
                            }
                        }
                        else
                        {
                            return null;
                        }
                    }

                    if (packageDirectories.Any(d => string.Compare(d, rootName, true) == 0))
                    {
                        packageRootDirectory = packageDirectories.Single(d => string.Compare(d, rootName, true) == 0);
                    }
                    else if (packageDirectories.Any(d => d.StartsWith(rootName, StringComparison.CurrentCultureIgnoreCase)))
                    {
                        packageRootDirectory = packageDirectories.Single(d => d.StartsWith(rootName, StringComparison.CurrentCultureIgnoreCase));
                    }

                    if (packageRootDirectory != null)
                    {
                        var packageVersionDirectory = new DirectoryInfo(Path.Combine(nugetFolder, packageRootDirectory)).GetDirectories().OrderBy(d => d.Name).Last();
                        var libDirectory = new DirectoryInfo(Path.Combine(packageVersionDirectory.FullName, @"lib\net8.0"));

                        if (!libDirectory.Exists)
                        {
                            libDirectory = new DirectoryInfo(Path.Combine(packageVersionDirectory.FullName, @"lib\netcoreapp3.1"));
                        }

                        if (!libDirectory.Exists)
                        {
                            libDirectory = new DirectoryInfo(Path.Combine(packageVersionDirectory.FullName, @"lib\netstandard2.0"));
                        }

                        if (!libDirectory.Exists)
                        {
                            libDirectory = new DirectoryInfo(Path.Combine(packageVersionDirectory.FullName, @"lib\netstandard2.0"));
                        }

                        if (!libDirectory.Exists)
                        {
                            libDirectory = new DirectoryInfo(Path.Combine(packageVersionDirectory.FullName, @"lib\netstandard2.1"));
                        }

                        if (libDirectory.Exists)
                        {
                            files = libDirectory.GetFiles($"{name}.dll").ToList();

                            if (files.Count == 1)
                            {
                                var file = files.Single();

                                Debug.WriteLine("ResolveAssemblyReference: Resolved '{0}' with '{1}'", name, file.FullName);

                                return file.FullName;
                            }
                            else
                            {
                                return null;
                            }
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
                else
                {
                    return null;
                }
            }

            return null;
        }

        public Type ResolveDirectiveProcessor(string processorName)
        {
            throw new NotImplementedException();
        }

        public string ResolveParameterValue(string directiveId, string processorName, string parameterName)
        {
            throw new NotImplementedException();
        }

        public string ResolvePath(string path)
        {
            throw new NotImplementedException();
        }

        private static string UnescapeArg(string arg)
        {
            return arg.Replace("\\\\", "\\");
        }

        private static string[] UnescapeArgs(string[] args)
        {
            return args.Select(UnescapeArg).ToArray();
        }

        //private static IEnumerable<TemplateError> ProcessErrors(IEnumerable<Diagnostic> diagnostics)
        //{
        //    return diagnostics
        //        .Where(d => d.Severity == DiagnosticSeverity.Error || d.Severity == DiagnosticSeverity.Warning)
        //        .Select(d => new TemplateError(
        //            d.Severity == DiagnosticSeverity.Warning,
        //            d.ToString(),
        //            d.Location.GetMappedLineSpan().Span.Start.Line + 1,
        //            d.Location.GetMappedLineSpan().Span.Start.Character + 1));
        //}

        //public static void WriteTemplateErrors(IEnumerable<TemplateError> errors)
        //{
        //    foreach (TemplateError error in errors)
        //    {
        //        Console.Error.WriteLine(error.Warning ? 1 : 0);
        //        Console.Error.WriteLine(error.Line);
        //        Console.Error.WriteLine(error.Column);
        //        Console.Error.WriteLine(error.Message.Length);
        //        Console.Error.WriteLine(error.Message);
        //    }
        //}

        public AppDomain ProvideTemplatingAppDomain(string content)
        {
            throw new NotImplementedException();
        }

        public void SetFileExtension(string extension)
        {
            throw new NotImplementedException();
        }

        public void SetOutputEncoding(Encoding encoding, bool fromOutputDirective)
        {
            throw new NotImplementedException();
        }
    }
}
