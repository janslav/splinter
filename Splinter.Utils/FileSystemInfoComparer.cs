using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace Splinter.Utils
{
    public class FileSystemInfoComparer : IEqualityComparer<FileSystemInfo>
    {
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

        public int GetHashCode(FileSystemInfo obj)
        {
            return obj.FullName.GetHashCode();
        }
    }
}
