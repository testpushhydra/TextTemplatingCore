//---------------------------------------------//
// Copyright 2022 CloudIDEaaS                        //
// https://github.com/CloudIDEaaS/TextTemplatingCore //
//---------------------------------------------//
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;


namespace CloudIDEaaS.TemplateExecute
{
    internal sealed class LibraryLoadContext : ISessionLoadContext
    {
        public Assembly LoadFromAssemblyName(AssemblyName assemblyName)
        {
            try
            {
                return Assembly.Load(assemblyName);
            }
            catch (Exception ex)
            {
                Debugger.Break();
                throw;
            }
        }

        public Assembly LoadFromAssemblyPath(string assemblyPath)
        {
            try
            {
                return Assembly.LoadFrom(assemblyPath);
            }
            catch (Exception ex)
            {
                Debugger.Break();
                throw;
            }
        }

        public Assembly LoadFromStream(Stream assembly)
        {
            try
            {
                var bytes = new byte[assembly.Length];

                assembly.Read(bytes, 0, bytes.Length);

                return Assembly.Load(bytes);
            }
            catch (Exception ex)
            {
                Debugger.Break();
                throw;
            }
        }

        public Assembly LoadFromStream(Stream assembly, Stream assemblySymbols)
        {
            var assemblyBytes = new byte[assembly.Length];
            var symbolsBytes = new byte[assemblySymbols.Length];

            assembly.Read(assemblyBytes, 0, assemblyBytes.Length);

            return Assembly.Load(assemblyBytes, symbolsBytes);
        }

        public Assembly LoadLibrary(string library)
        {
            Assembly assembly;

            if (library.EndsWith(".dll"))
            {
                assembly = LoadFromAssemblyPath(library);
            }
            else if (library == "Microsoft.VisualStudio.Interop")
            {
                library = Environment.ExpandEnvironmentVariables(@"%userprofile%\.nuget\packages\microsoft.visualstudio.interop\17.8.37221\lib\net6.0\Microsoft.VisualStudio.Interop.dll");

                if (!File.Exists(library))
                {
                    MessageBox.Show($"File not found at { library }. Please install package Microsoft.VisualStudio.Interop 17.8.37221");

                    throw new Exception($"Please install package Microsoft.VisualStudio.Interop 17.8.37221");
                }

                assembly = LoadFromAssemblyPath(library);
            }
            else
            {
                assembly = LoadFromAssemblyName(new AssemblyName(library));
            }

            return assembly;
        }
    }
}
