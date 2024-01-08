//---------------------------------------------//
// Copyright 2022 CloudIDEaaS                        //
// https://github.com/CloudIDEaaS/TextTemplatingCore //
//---------------------------------------------//
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.Loader;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.DependencyModel.Resolution;


namespace CloudIDEaaS.TemplateExecute
{
    internal sealed class LibraryLoadContext : AssemblyLoadContext
    {
        public Assembly LoadLibrary(string library)
        {
            Assembly assembly;

            if (library.EndsWith(".dll"))
            {
                assembly = LoadFromAssemblyPath(library);
            }
            else
            {
                assembly = LoadFromAssemblyName(new AssemblyName(library));
            }

            DependencyContext? dependencyContext = DependencyContext.Load(assembly);

            var resolver = new CompositeCompilationAssemblyResolver(new ICompilationAssemblyResolver[]
            {
                new AppBaseCompilationAssemblyResolver(Path.GetDirectoryName(library)),
                new ReferenceAssemblyPathResolver(),
                new PackageCompilationAssemblyResolver(),
            });

            Assembly? _OnResolving(AssemblyLoadContext context, AssemblyName assemblyName)
            {
                if (dependencyContext != null)
                {
                    RuntimeLibrary library = dependencyContext.RuntimeLibraries.FirstOrDefault(rl => rl.Name.Equals(assemblyName.Name, StringComparison.OrdinalIgnoreCase));

                    if (library != null)
                    {
                        var wrapper = new CompilationLibrary(library.Type, library.Name, library.Version, library.Hash,
                            library.RuntimeAssemblyGroups.SelectMany(rag => rag.AssetPaths), library.Dependencies, library.Serviceable);

                        var assemblies = new List<string>();
                        resolver.TryResolveAssemblyPaths(wrapper, assemblies);
                        if (assemblies.Count > 0)
                        {
                            return LoadFromAssemblyPath(assemblies[0]);
                        }
                    }
                }

                var assemblyReference = assemblyName.FullName;

                if (assemblyReference.EndsWith(".resources"))
                {
                    return null;
                }

                if (Path.IsPathRooted(assemblyReference))
                {
                    return LoadFromAssemblyPath(assemblyReference);
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

                            return LoadFromAssemblyPath(assemblyReference);
                        }
                    }
                    index = assemblyReference.IndexOf(",");
                    name = assemblyReference.Substring(0, index == -1 ? assemblyReference.Length : index);

                    files = new DirectoryInfo("C:\\Program Files\\Microsoft Visual Studio\\2022\\Community\\MSBuild\\Microsoft\\VisualStudio\\v17.0\\TextTemplating").GetFiles("*.dll", SearchOption.AllDirectories).ToList();

                    if (files.Any(f => Path.GetFileNameWithoutExtension(f.Name) == name))
                    {
                        var file = files.Single(f => Path.GetFileNameWithoutExtension(f.Name) == name);

                        Debug.WriteLine("ResolveAssemblyReference: Resolved '{0}' with '{1}'", name, file.FullName);

                        return LoadFromAssemblyPath(file.FullName);
                    }

                    files.AddRange(new DirectoryInfo(@"C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\PublicAssemblies").GetFiles("*.dll", SearchOption.AllDirectories));
                    files.AddRange(new DirectoryInfo(@"C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\3.1.0\ref\netcoreapp3.1").GetFiles("*.dll", SearchOption.AllDirectories));

                    if (files.Any(f => Path.GetFileNameWithoutExtension(f.Name) == name))
                    {
                        var file = files.Single(f => Path.GetFileNameWithoutExtension(f.Name) == name);

                        Debug.WriteLine("ResolveAssemblyReference: Resolved '{0}' with '{1}'", name, file.FullName);

                        return LoadFromAssemblyPath(file.FullName);
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

                                    return LoadFromAssemblyPath(file.FullName);
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

                                    return LoadFromAssemblyPath(file.FullName);
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

            Resolving += _OnResolving;

            return assembly;
        }
    }
}
