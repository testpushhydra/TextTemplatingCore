using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata;
using Utils;

namespace CloudIDEaaS.TemplateExecute
{
    public class TemplateMetadataReferenceResolver : MetadataReferenceResolver, IThunkResolver
    {
        private List<MetadataReference> _references;
        private SyntaxTree _syntaxTree;

        public TemplateMetadataReferenceResolver(List<MetadataReference> references, SyntaxTree syntaxTree)
        {
            _references = references;
            _syntaxTree = syntaxTree;
        }

        public override bool Equals(object? other)
        {
            throw new NotImplementedException();
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }

        public override ImmutableArray<PortableExecutableReference> ResolveReference(string reference, string? baseFilePath, MetadataReferenceProperties properties)
        {
            throw new NotImplementedException();
        }

        public void TransformType()
        {
            throw new NotImplementedException();
        }

        public Type TransformType(string assemblyName, string typeName)
        {
            string fullName = typeName + ", " + assemblyName;
            Type type = null;

            if (ConfigurationManager.AppSettings[fullName] != null)
            {
                var transformedName = ConfigurationManager.AppSettings[fullName];
                var nameParts = AssemblyExtensions.ParseAssemblyName(transformedName);
                System.Reflection.Assembly assembly;

                if (nameParts.AssemblyPath != null)
                {
                    assembly = System.Reflection.Assembly.LoadFrom(nameParts.AssemblyPath);
                }
                else
                {
                    assembly = AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(a => a.FullName == nameParts.FullAssemblyNameNoArchitecture);
                }

                if (assembly != null)
                {
                    type = assembly.GetTypes().SingleOrDefault(t => t.FullName == typeName);

                    if (type != null)
                    {
                        return type;
                    }
                    else
                    {
                        type = assembly.GetForwardedTypes().SingleOrDefault(t => t.FullName == typeName);
                    }
                }
            }

            return type;
        }
    }
}
