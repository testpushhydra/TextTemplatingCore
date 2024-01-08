using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utils
{
    public static class StringExtensions
    {
        public static string EmptyToNull(this string str)
        {
            return string.IsNullOrWhiteSpace(str) ? null : str;
        }

        public static string AsDisplayText(this string str)
        {
            if (str == null)
            {
                return "null";
            }
            else
            {
                return str;
            }
        }
    }
}
