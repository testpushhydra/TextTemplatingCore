using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace CloudIDEaaS.TemplateExecute
{
    public class TemplateSourceReferenceResolver : SourceReferenceResolver
    {
        private List<MetadataReference> _references;
        private SyntaxTree _syntaxTree;

        public TemplateSourceReferenceResolver(List<MetadataReference> references, SyntaxTree syntaxTree)
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

        public override string? NormalizePath(string path, string? baseFilePath)
        {
            throw new NotImplementedException();
        }

        public override Stream OpenRead(string resolvedPath)
        {
            throw new NotImplementedException();
        }

        public override string? ResolveReference(string path, string? baseFilePath)
        {
            throw new NotImplementedException();
        }
    }
}
