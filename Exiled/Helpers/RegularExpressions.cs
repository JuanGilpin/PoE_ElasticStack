using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Exiled.Helpers
{
    public class RegularExpressions
    {
        public static Match RegexRun(string pattern, string line, long lineNumber)
        {
            Regex logRegex = new Regex(pattern);
            return logRegex.Match(line);            
        }
    }
}
