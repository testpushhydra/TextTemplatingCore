//---------------------------------------------//
// Copyright 2022 CloudIDEaaS                        //
// https://github.com/CloudIDEaaS/TextTemplatingCore //
//---------------------------------------------//
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using CloudIDEaaS.TextTemplatingCore.TextTemplatingCoreLib;
using System.Diagnostics;
using System.Reflection.Metadata;

namespace CloudIDEaaS.TemplateExecute
{
    internal static class Program
    {
        private const string TEMPLATE_NAMESPACE = "CloudIDEaaS.TextTemplatingCore.GeneratedTemplate";
        private const string TEMPLATE_CLASS = "Template";

        private static int Main(string[] args)
        {
            try
            {
                if (args.Length < 3)
                {
                    throw new ArgumentException($"Need at least 3 arguments, found only {args.Length}.", nameof(args));
                }

                string templateFile = UnescapeArg(args[0]);
                string inputFile = UnescapeArg(args[1]);
                string outputFile = UnescapeArg(args[2]);
                string[] libraries = args.Length > 3 ? UnescapeArgs(args[3..]) : new string[0];
                var directory = new DirectoryInfo(Path.GetDirectoryName(templateFile));

                Directory.SetCurrentDirectory(directory.FullName);

                string inputCode = File.ReadAllText(inputFile, Encoding.UTF8);
                SourceText sourceText = SourceText.From(inputCode);
                CSharpParseOptions options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp12);
                SyntaxTree syntaxTree = SyntaxFactory.ParseSyntaxTree(sourceText, options);

                string partialCode = $"namespace {TEMPLATE_NAMESPACE} {{ public partial class {TEMPLATE_CLASS} {{ " +
                    $"public string TemplateFile {{ get; }} = @\"{templateFile.Replace("\"", "\"\"")}\";" +
                    " } }";
                SourceText partialText = SourceText.From(partialCode);
                SyntaxTree partialTree = SyntaxFactory.ParseSyntaxTree(partialText, options);

                // This array is here so the assemblies containing these types are automatically loaded into the AppDomain
                _ = new[] {
                    typeof(System.CodeDom.Compiler.GeneratedCodeAttribute),
                    typeof(System.CodeDom.Compiler.CompilerError),
                    typeof(System.Collections.CollectionBase),
                };

                List<MetadataReference> references = new List<MetadataReference>();

                foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
                {
                    references.Add(MetadataReference.CreateFromFile(a.Location));
                }

                LibraryLoadContext context = new LibraryLoadContext();

                foreach (string library in libraries)
                {
                    Assembly assembly = context.LoadLibrary(library);
                    references.Add(MetadataReference.CreateFromFile(assembly.Location));
                }

                var textTemplatingInterfacesPath = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), @"Binaries\Microsoft.VisualStudio.TextTemplating.12.0\Microsoft.VisualStudio.TextTemplating.Interfaces.10.0.dll");

                references.Add(MetadataReference.CreateFromFile(textTemplatingInterfacesPath));

                var templateSourceReferenceResolver = new TemplateSourceReferenceResolver(references, syntaxTree);
                var templateMetadataReferenceResolver = new TemplateMetadataReferenceResolver(references, syntaxTree);

                CSharpCompilation compilation = CSharpCompilation.Create("GeneratedTemplate.dll", new[] { syntaxTree, partialTree }, references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, sourceReferenceResolver: templateSourceReferenceResolver, metadataReferenceResolver: templateMetadataReferenceResolver));

                using (var stream = new MemoryStream())
                {
                    var result = compilation.Emit(stream);
                    var debugResult = false;

                    IEnumerable<TemplateError> compileErrors = ProcessErrors(result.Diagnostics);

                    if (!result.Success)
                    {
                        throw new TemplateException(compileErrors);
                    }

                    if (debugResult)
                    {
                        var fileInfo = new FileInfo(Path.Combine(directory.FullName, @"Generated_Code\GeneratedTemplate.dll"));

                        if (!fileInfo.Directory.Exists)
                        {
                            fileInfo.Directory.Create();
                        }
                        else if (fileInfo.Exists)
                        {
                            fileInfo.Delete();
                        }

                        BinaryReader reader = null;

                        stream.Seek(0, SeekOrigin.Begin);

                        using (var fileStream = fileInfo.OpenWrite())
                        using (var writer = new BinaryWriter(fileStream))
                        {
                            var buffer = new byte[stream.Length];
                            string code;

                            reader = new BinaryReader(stream);

                            reader.Read(buffer, 0, buffer.Length);
                            writer.Write(buffer, 0, buffer.Length);

                            writer.Flush();
                        }
                    }

                    stream.Seek(0, SeekOrigin.Begin);

                    Assembly assembly = context.LoadFromStream(stream);
                    var solutionFileName = Environment.ExpandEnvironmentVariables("%SolutionFileName%");

                    using (var host = new TextTemplatingEngineHost(templateFile, solutionFileName))
                    {
                        Type? templateType = assembly.GetType($"{TEMPLATE_NAMESPACE}.{TEMPLATE_CLASS}");

                        if (templateType == null)
                        {
                            throw new Exception("Failed to find template class.");
                        }

                        dynamic? template = Activator.CreateInstance(templateType);

                        if (template == null)
                        {
                            throw new Exception("Failed to create instance of template class.");
                        }

                        template.Host = host;
                        string output = template.TransformText();

                        File.WriteAllText(outputFile, output, Encoding.UTF8);

                        WriteTemplateErrors(compileErrors);
                        return 0;
                    }
                }
            }
            catch (TemplateException ex)
            {
                WriteTemplateErrors(ex.Errors);
                return 1;
            }
            catch (Exception ex)
            {
                Console.Error.Write(ex.ToString());
                return 2;
            }
        }

        private static string UnescapeArg(string arg)
        {
            return arg.Replace("\\\\", "\\");
        }

        private static string[] UnescapeArgs(string[] args)
        {
            return args.Select(UnescapeArg).ToArray();
        }

        private static IEnumerable<TemplateError> ProcessErrors(IEnumerable<Diagnostic> diagnostics)
        {
            return diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error || d.Severity == DiagnosticSeverity.Warning)
                .Select(d => new TemplateError(
                    d.Severity == DiagnosticSeverity.Warning,
                    d.ToString(),
                    d.Location.GetMappedLineSpan().Span.Start.Line + 1,
                    d.Location.GetMappedLineSpan().Span.Start.Character + 1));
        }

        private static void WriteTemplateErrors(IEnumerable<TemplateError> errors)
        {
            foreach (TemplateError error in errors)
            {
                if (!error.Warning)
                {
                    Console.Error.WriteLine(error.Warning ? 1 : 0);
                    Console.Error.WriteLine(error.Line);
                    Console.Error.WriteLine(error.Column);
                    Console.Error.WriteLine(error.Message.Length);
                    Console.Error.WriteLine(error.Message);
                }
            }
        }
    }
}
