using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Splinter.Contracts.DTOs
{
    /// <summary>
    /// Represents a method
    /// </summary>
    [DebuggerDisplay("Method {FullName}")]
    public class MethodRef
    {
        public MethodRef(FileInfo assembly, string fullName)
        {
            this.Assembly = assembly;
            this.FullName = fullName;
        }

        /// <summary>
        /// Gets the assembly location.
        /// </summary>
        public FileInfo Assembly { get; private set; }

        /// <summary>
        /// Gets the full name of the method, as produced by Mono.Cecil.MethodReference.FullName
        /// </summary>
        public string FullName { get; private set; }

        #region Equals & GetHashCode
        /// <summary>
        /// Determines whether the specified <see cref="System.Object" />, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (obj.GetType() != typeof(MethodRef))
            {
                return false;
            }

            var o = (MethodRef)obj;

            return string.Equals(this.Assembly.FullName, o.Assembly.FullName, StringComparison.OrdinalIgnoreCase) &&
                 string.Equals(this.FullName, o.FullName);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            var fileHash = StringComparer.OrdinalIgnoreCase.GetHashCode(this.Assembly.FullName);

            return fileHash * 17 + this.FullName.GetHashCode();
        }
        #endregion
    }
}
