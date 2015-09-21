using Mono.Cecil.Cil;
using Splinter.Utils.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Splinter.Contracts.DTOs
{
    public class CodeRegionInfo
    {
        public string SourceReference { get; private set; }
        public string Before { get; private set; }
        public string[] Affected { get; private set; }
        public string After { get; private set; }
        public CodeRegionInfo(SourceFile source, SequencePoint sp)
        {
            this.SourceReference = string.Format("{0}:{1}", new FileInfo(source.Document.Url).Name, sp.StartLine);
            this.Before = source.Lines[sp.StartLine].Substring(0, sp.StartColumn - 1);
            this.Affected = this.BuildAffectedCode(source, sp).ToArray();
            this.After = source.Lines[sp.EndLine].Substring(sp.EndColumn - 1);
        }

        private IEnumerable<string> BuildAffectedCode(SourceFile source, SequencePoint sp)
        {
            if (sp.EndLine > sp.StartLine)
            {
                yield return source.Lines[sp.StartLine].Substring(sp.StartColumn - 1);
                foreach (var line in source.Lines.Skip(sp.StartLine).Take(sp.EndLine - sp.StartLine - 1))
                {
                    yield return line.Value;
                }
                yield return source.Lines[sp.EndLine].Substring(0, sp.EndColumn);
            }
            else {
                yield return source.Lines[sp.StartLine].Substring(sp.StartColumn - 1, sp.EndColumn-sp.StartColumn);
            }
        }

    }

}
