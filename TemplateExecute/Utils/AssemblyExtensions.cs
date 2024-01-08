using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Utils
{
    public static class AssemblyExtensions
    {
        public static AssemblyNameParts ParseAssemblyName(string name)
        {
            var regex = new Regex(@"^\[?(?<assembly>[\w\.\-]+)(,\s?Version=(?<version>\d+\.\d+\.\d+\.\d+))?(,\s?Culture=(?<culture>[\w\-]+))?(,\s?PublicKeyToken=(?<token>\w+))?(,\s?processorArchitecture=(?<processorarchitecture>\w+))?\]?$");

            if (regex.IsMatch(name))
            {
                var match = regex.Match(name);
                var assemblyName = match.Groups["assembly"].Value.EmptyToNull();
                var version = match.Groups["version"].Value.EmptyToNull();
                var culture = match.Groups["culture"].Value.EmptyToNull();
                var token = match.Groups["token"].Value.EmptyToNull();
                var processorArchitecture = match.Groups["processorarchitecture"].Value.EmptyToNull();

                return new AssemblyNameParts(assemblyName, version, culture, token, processorArchitecture);
            }
            else
            {
                regex = new Regex(@"^\[?(?<type>[\w\.\-]+),\s(?<assembly>[\w\.]+)(,\s?Version=(?<version>\d+\.\d+\.\d+\.\d+))?(,\s?Culture=(?<culture>[\w\-]+))?(,\s?PublicKeyToken=(?<token>\w+))?(,\s?processorArchitecture=(?<processorarchitecture>\w+))?\]?$");

                if (regex.IsMatch(name))
                {
                    var match = regex.Match(name);
                    var type = match.Groups["type"].Value;
                    var assemblyName = match.Groups["assembly"].Value.EmptyToNull();
                    var version = match.Groups["version"].Value.EmptyToNull();
                    var culture = match.Groups["culture"].Value.EmptyToNull();
                    var token = match.Groups["token"].Value.EmptyToNull();
                    var processorArchitecture = match.Groups["processorarchitecture"].Value.EmptyToNull();

                    return new AssemblyNameParts(assemblyName, version, culture, token, processorArchitecture, type);
                }
                else
                {
                    regex = new Regex(@"^\[?(?<type>[\w\.\-]+),\s(?<assemblyPath>.+)\]?$");

                    if (regex.IsMatch(name))
                    {
                        var match = regex.Match(name);
                        var type = match.Groups["type"].Value;
                        var assemblyPath = match.Groups["assemblyPath"].Value.EmptyToNull();

                        return new AssemblyNameParts(assemblyPath, type);
                    }
                }
            }

            return null;
        }

    }
}
