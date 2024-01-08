using Microsoft.VisualStudio.TextTemplating;
using Microsoft.VisualStudio;
using System.Text;
using System.IO;

namespace TemplateExecute
{
    internal class TemplateContext
    {
        private bool encodingSetFromOutputDirective;
        private string extension = ".txt";
        private readonly FileInfo inputFile;
        private ITextTemplatingSession session;

        public string TemplateFile => this.inputFile.FullName;

        public string TemplateContents { get; }

        public string Extension => this.extension;

        public Encoding OutputEncoding { get; private set; }

        public ITextTemplatingSession UserSession
        {
            get
            {
                if( this.session == null )
                    this.session = (ITextTemplatingSession)new TextTemplatingSession();
                return this.session;
            }
        }

        internal TemplateContext(FileInfo inputFile)
        {
            this.inputFile = inputFile;
            this.TemplateContents = File.ReadAllText(this.TemplateFile);
            this.OutputEncoding = EncodingHelper.GetEncoding(this.TemplateFile);
        }

        public void SetFileExtension(string extension) => this.extension = extension;

        public void SetOutputEncoding(Encoding encoding, bool fromOutputDirective)
        {
            if( this.encodingSetFromOutputDirective )
                return;
            if( fromOutputDirective )
            {
                this.encodingSetFromOutputDirective = true;
                this.OutputEncoding = encoding;
            }
            else
                this.OutputEncoding = encoding;
        }

        internal string GetOutputFileName(string outputFileOption) => string.IsNullOrEmpty(outputFileOption) ? Path.Combine(this.inputFile.Directory.FullName, Path.GetFileNameWithoutExtension(this.inputFile.Name)) + this.Extension : outputFileOption;
    }
}

