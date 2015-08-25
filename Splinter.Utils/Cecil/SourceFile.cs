namespace Splinter.Utils.Cecil
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using Mono.Cecil.Cil;

    /// <summary>
    /// Represents a source code file that is part of a mutation testing
    /// report.
    /// </summary>
    public class SourceFile
    {
        /// <summary>
        /// Initializes a new instance of <see cref="SourceFile" />.
        /// </summary>
        public SourceFile(Document document)
        {
            this.Lines = new SortedList<int, string>();

            this.Document = document;

            if (!File.Exists(document.Url))
            {
                throw new Exception(string.Format("Couldn't find source file '{0}'.", document.Url));
            }

            var lines = File.ReadAllLines(document.Url);
            for (var i = 0; i < lines.Length; i++)
            {
                this.Lines.Add(i + 1, lines[i]);
            }
        }

        /// <summary>
        /// Gets or sets the URL of the file.
        /// </summary>
        public Document Document { get; set; }

        /// <summary>
        /// Gets or sets a list of lines of code in the file. 
        /// </summary>
        public IDictionary<int, string> Lines { get; set; }
    }
}
