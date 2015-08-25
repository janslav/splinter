namespace Splinter.Utils
{
    using System.Text.RegularExpressions;

    /// <summary>
    /// Taken from https://github.com/ericpopivker/Command-Line-Encoder
    /// It looks well tested except for the case when shell (cmd.exe) is used, then I think it requires escaping stuff with ^ 
    /// </summary>
    public static class CmdLine
    {
        /// <summary>
        /// Using this you can safely encode/escape text arguments on command line.
        /// </summary>
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
            const string RegexPattern = @"\\+""";

            var result = Regex.Replace(original, RegexPattern,
                delegate(Match match)
                {
                    var matchText = match.ToString();
                    var justSlashes = matchText.Remove(matchText.Length - 1);
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
            const string RegexPattern = @"\\+$";

            var result = Regex.Replace(original, RegexPattern,
                delegate(Match match)
                {
                    var matchText = match.ToString();
                    return matchText + matchText;  //double up the slashes
                });

            return result;
        }
    }
}