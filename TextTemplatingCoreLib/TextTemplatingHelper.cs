//---------------------------------------------//
// Copyright 2022 CloudIDEaaS                        //
// https://github.com/CloudIDEaaS/TextTemplatingCore //
//---------------------------------------------//
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace CloudIDEaaS.TextTemplatingCore.TextTemplatingCoreLib
{
    public static class TextTemplatingHelper
    {
        public static IEnumerable<string> ProcessReferences(IEnumerable<string> references, string inputFileName, IDictionary<string, string> variables = null)
        {
            variables = variables != null
                ? new Dictionary<string, string>(variables, StringComparer.InvariantCultureIgnoreCase)
                : new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            AddEnvironmentVariables(variables);

            return references
                .Select(r =>
                {
                    foreach (var v in variables)
                    {
                        r = Regex.Replace(r, Regex.Escape($"$({v.Key})"), v.Value.Replace("$", "$$"), RegexOptions.IgnoreCase);
                    }

                    return r;
                })
                .Select(r =>
                {
                    if (r.EndsWith(".dll"))
                    {
                        r = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(inputFileName), r));
                    }

                    return r;
                })
                .Distinct();
        }

        private static void AddEnvironmentVariables(IDictionary<string, string> variables)
        {
            // Handle variables like $(UserProfile) for pulling NuGet packages from the local storage folder
            foreach (DictionaryEntry ev in Environment.GetEnvironmentVariables())
            {
                if (!(ev.Key is string key) || variables.ContainsKey(key))
                {
                    continue;
                }

                string value;
                switch (ev.Value)
                {
                    case null:
                        value = "";
                        break;
                    case string s:
                        value = s;
                        break;
                    default:
                        value = ev.Value.ToString();
                        break;
                }

                variables.Add(key, value);
            }
        }

        public static string TemplateExecute(string inputFileName, string templateCode, string[] references, Action callback, out TemplateError[] errors)
        {
            string coreInputFile = null;
            string coreOutputFile = null;

            try
            {
                coreInputFile = Path.GetTempFileName();
                File.WriteAllText(coreInputFile, templateCode, Encoding.UTF8);
                coreOutputFile = Path.GetTempFileName();

                bool executeSuccess = RunExecute(inputFileName, coreInputFile, coreOutputFile, references, callback, out errors);

                if (!executeSuccess)
                {
                    return null;
                }
                else
                {
                    return File.ReadAllText(coreOutputFile, Encoding.UTF8);
                }
            }
            finally
            {
                try
                {
                    if (coreInputFile != null && File.Exists(coreInputFile))
                    {
                        File.Delete(coreInputFile);
                    }
                }
                catch
                {
                }

                try
                {
                    if (coreOutputFile != null && File.Exists(coreOutputFile))
                    {
                        File.Delete(coreOutputFile);
                    }
                }
                catch
                {
                }
            }
        }

        private static bool RunExecute(string inputFileName, string coreInputFile, string coreOutputFile, string[] references, Action processEndCallback, out TemplateError[] errors)
        {
            string executePath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"TemplateExecute\TemplateExecute.exe"));

            ProcessStartInfo info = new ProcessStartInfo
            {
                FileName = executePath,
                Arguments = EscapeArguments(new[] { inputFileName, coreInputFile, coreOutputFile }.Concat(references)),
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                WindowStyle = ProcessWindowStyle.Hidden,
            };

            var process = Process.Start(info);
            string output = string.Empty;

            process.EnableRaisingEvents = true;

            process.OutputDataReceived += (s, e) =>
            {
                if (e.Data != null && e.Data.Length > 0)
                {
                    output += e.Data;
                }
            };

            process.Exited += (s, e) =>
            {
                var regex = new Regex(@"^TempVsProcessId: (?<tempVsProcessId>\d*?)$");

                if (output != null)
                {
                    if (regex.IsMatch(output))
                    {
                        var match = regex.Match(output);

                        var processIdText = match.Groups["tempVsProcessId"].Value;

                        if (processIdText != null)
                        {
                            var processId = (int) uint.Parse(processIdText);
                            Process vsProcess;

                            try
                            {
                                vsProcess = Process.GetProcessById(processId);

                                if (vsProcess != null)
                                {
                                    vsProcess.Kill();
                                }
                            }
                            catch
                            {
                            }
                        }
                    }
                }
            };

            process.BeginOutputReadLine();

            if (Debugger.IsAttached)
            {
                process.WaitForExit();
            }
            else
            {
                process.WaitForExit(90);
            }

            if (!process.HasExited)
            {
                string error = process.StandardError.ReadToEnd();

                try
                {
                    if (error.Length > 0)
                    {
                        process.Kill();
                        throw new TimeoutException($"The TemplateExecute process did not respond within 90 seconds. Aborting operation. Error: { error }");
                    }
                    else
                    {
                        processEndCallback();
                    }
                }
                catch (Exception ex)
                {
                    throw new TimeoutException($"The TemplateExecute process did not respond within 90 seconds. Aborting operation. Error: { ex }");
                }
            }
            else
            {
                processEndCallback();
            }

            if (process.ExitCode == 0)
            {
                errors = ProcessTemplateErrors(process).ToArray();
                return true;
            }
            else if (process.ExitCode == 1)
            {
                errors = ProcessTemplateErrors(process).ToArray();
                return false;
            }
            else
            {
                string error = process.StandardError.ReadToEnd();
                errors = new[] { new TemplateError(false, $"Something went wrong executing the template in .NET Core: {error}") };
                return false;
            }
        }

        private static IEnumerable<TemplateError> ProcessTemplateErrors(Process process)
        {
            var stdError = process.StandardError;

            while (!stdError.EndOfStream)
            {
                bool warning = stdError.ReadLine() == "1";
                int line = int.Parse(stdError.ReadLine());
                int column = int.Parse(stdError.ReadLine());
                int messageLength = int.Parse(stdError.ReadLine());
                char[] messageBuffer = new char[messageLength];
                int readLength = stdError.ReadBlock(messageBuffer, 0, messageLength);
                string message = new string(messageBuffer, 0, readLength);
                stdError.ReadLine();

                yield return new TemplateError(warning, message, line, column);
            }
        }

        public static string EscapeArguments(IEnumerable<string> args)
        {
            StringBuilder arguments = new StringBuilder();

            foreach (string arg in args)
            {
                if (arguments.Length > 0)
                {
                    arguments.Append(" ");
                }

                arguments.Append($"\"{arg.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"");
            }

            return arguments.ToString();
        }

        public static string UnescapeArg(string arg)
        {
            return arg.Replace("\\\\", "\\");
        }

        public static string[] UnescapeArgs(string[] args)
        {
            return args.Select(UnescapeArg).ToArray();
        }
    }
}
