using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.TextTemplating;
using Utils;

namespace CloudIDEaaS.TemplateExecute
{
    internal class TextTemplatingEngineHost : ITextTemplatingEngineHost, IServiceProvider, IDisposable
    {
        private string extension;
        private DTE? dte;

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        public IList<string> StandardAssemblyReferences { get; }
        public IList<string> StandardImports { get; }
        public string TemplateFile { get; }
        public string SolutionFileName { get; }
        private string templatePath;

        public TextTemplatingEngineHost(string templateFile, string solutionFileName)
        {
            this.TemplateFile = templateFile;
            this.SolutionFileName = solutionFileName;

            templatePath = Path.GetDirectoryName(templateFile);
        }

        public object GetHostOption(string optionName)
        {
            return null;
        }

        public bool LoadIncludeText(string requestFileName, out string content, out string location)
        {
            throw new NotImplementedException();
        }

        public void LogErrors(CompilerErrorCollection errors)
        {
            throw new NotImplementedException();
        }

        public AppDomain ProvideTemplatingAppDomain(string content)
        {
            return AppDomain.CurrentDomain;
        }

        public string ResolveAssemblyReference(string assemblyReference)
        {
            throw new NotImplementedException();
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
            if (path == null || path == string.Empty)
            {
                return templatePath;
            }
            else
            {
                if (Path.IsPathRooted(path))
                {
                    return path;
                }

                return Environment.ExpandEnvironmentVariables(Path.GetFullPath(Path.Combine(templatePath, path)));
            }
        }

        public void SetFileExtension(string extension)
        {
            this.extension = extension;
        }

        public void SetOutputEncoding(Encoding encoding, bool fromOutputDirective)
        {
            
        }
        public object? GetService(Type serviceType)
        {
            if (serviceType == typeof(DTE))
            {
                var solutionName = Path.GetFileNameWithoutExtension(this.SolutionFileName);
                var vsProcess = System.Diagnostics.Process.GetProcessesByName("devenv").Single(p => p.MainWindowTitle == $"{solutionName} - Microsoft Visual Studio");
                var visualStudioType = Type.GetTypeFromProgID("VisualStudio.DTE.17.0");
                IntPtr hwnd;
                uint processId;

                dte = Activator.CreateInstance(visualStudioType) as DTE;

                dte.Solution.Open(this.SolutionFileName);

                hwnd = dte.MainWindow.HWnd;

                if (GetWindowThreadProcessId(hwnd, out processId) != 0)
                {
                    var output = Console.OpenStandardOutput();
                    var message = string.Format("TempVsProcessId: {0}", processId);
                    var bytes = ASCIIEncoding.ASCII.GetBytes(message);

                    output.Write(bytes, 0, bytes.Length);

                    output.Flush();
                }

                return dte;
            }

            return null;
        }

        public void Dispose()
        {
            if (dte != null)
            {
                IntPtr pUnk;

                dte.Solution.Close();
                dte.Quit();

                pUnk = Marshal.GetIUnknownForObject(dte);

                Marshal.Release(pUnk);
                dte = null;
            }
        }
    }
}
