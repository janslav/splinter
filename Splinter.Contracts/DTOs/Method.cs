using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Splinter.Contracts.DTOs
{
    [DebuggerDisplay("Method {FullName}")]
    public class Method
    {
        public FileInfo Assembly { get; set; }

        public string FullName { get; set; }

        #region Equals & GetHashCode
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (obj.GetType() != typeof(Method))
            {
                return false;
            }

            var o = (Method)obj;

            return string.Equals(this.Assembly.FullName, o.Assembly.FullName, StringComparison.OrdinalIgnoreCase) &&
                 string.Equals(this.FullName, o.FullName);
        }

        public override int GetHashCode()
        {
            var fileHash = StringComparer.OrdinalIgnoreCase.GetHashCode(this.Assembly.FullName);

            return fileHash * 17 + this.FullName.GetHashCode();
        }
        #endregion
    }
}
