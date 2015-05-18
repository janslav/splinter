using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace Splinter.Utils
{
    /// <summary>
    /// IEqualityComparer implementation for FileSystemInfo
    /// </summary>
    public class FileSystemInfoComparer : IEqualityComparer<FileSystemInfo>
    {
        /// <summary>
        /// Determines whether the specified objects are equal.
        /// </summary>
        /// <param name="x">The first object of type <paramref name="T" /> to compare.</param>
        /// <param name="y">The second object of type <paramref name="T" /> to compare.</param>
        /// <returns>
        /// true if the specified objects are equal; otherwise, false.
        /// </returns>
        public bool Equals(FileSystemInfo x, FileSystemInfo y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if ((x == null) ||  (y == null))
            {
                return false;
            }

            if (x.GetType() != y.GetType())
            {
                return false;
            }

            return string.Equals(x.FullName, y.FullName, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public int GetHashCode(FileSystemInfo obj)
        {
            return obj.FullName.GetHashCode();
        }
    }
}
