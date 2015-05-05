using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Splinter.Utils
{
    public static class CmdLine
    {
        public static string EncodeArgument(string original)
        {
            var result = original;

            result = TryEncodeSlashesFollowedByQuotes(result);

            result = TryEncodeQuotes(result);

            result = TryEncodeLastSlash(result);

            return result;
        }

        private static string TryEncodeSlashesFollowedByQuotes(string original)
        {
            var regexPattern = @"\\+""";

            string result = Regex.Replace(original, regexPattern,
                delegate(Match match)
                {
                    string matchText = match.ToString();
                    string justSlashes = matchText.Remove(matchText.Length - 1);
                    return justSlashes + justSlashes + "\"";  //double up the slashes
                });

            return result;
        }

        private static string TryEncodeQuotes(string original)
        {
            var result = original.Replace("\"", "\"\"");
            return result;
        }

        private static string TryEncodeLastSlash(string original)
        {
            var regexPattern = @"\\+$";

            string result = Regex.Replace(original, regexPattern,
                delegate(Match match)
                {
                    string matchText = match.ToString();
                    return matchText + matchText;  //double up the slashes
                });

            return result;
        }

        public static string EscapeBackSlashes(string text)
        {
            var regexPattern = "\\\\";

            var result = text;

            var regex = new Regex(regexPattern);

            var matches = regex.Matches(text);

            for (int i = matches.Count - 1; i >= 0; i--)
            {
                var match = matches[i];

                var index = match.Index + match.Length;

                if (index >= text.Length || text[index] == '\\')
                    result = result.Insert(match.Index, "\\");
            }

            return result;
        }
    }
}