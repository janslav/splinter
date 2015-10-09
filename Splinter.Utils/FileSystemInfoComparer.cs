namespace Splinter.Utils
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// IEqualityComparer implementation for FileSystemInfo
    /// </summary>
    public class FileSystemInfoComparer : IEqualityComparer<FileSystemInfo>
    {
        /// <summary>
        /// Determines whether the specified objects are equal.
        /// </summary>
        public bool Equals(FileSystemInfo x, FileSystemInfo y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if ((x == null) || (y == null))
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
        /// Returns a hash code for the specified object.
        /// </summary>
        public int GetHashCode(FileSystemInfo obj)
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(obj.FullName);
        }
    }
}
