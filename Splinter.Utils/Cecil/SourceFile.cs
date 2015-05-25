#region Copyright & licence

// This file is part of NinjaTurtles.
// 
// NinjaTurtles is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as
// published by the Free Software Foundation, either version 3 of the
// License, or (at your option) any later version.
// 
// NinjaTurtles is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public
// License along with NinjaTurtles.  If not, see <http://www.gnu.org/licenses/>.
// 
// Copyright (C) 2012-14 David Musgrove and others.

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Splinter.Utils.Cecil
{
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
            for (int i = 0; i < lines.Length; i++)
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
