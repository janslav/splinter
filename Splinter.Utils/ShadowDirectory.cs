// Copyright 2005-2010 Gallio Project - http://www.gallio.org/
// Portions Copyright 2000-2004 Jonathan de Halleux
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace Splinter.Utils
{
    /// <summary>
    /// The point of ShadowDirectory is to create a temporary ("shadow") directory by copying the contents of a specified source.
    /// It then cleans up this directory on calling Dispose()
    /// </summary>
    /// </remarks>
    [DebuggerDisplay("ShadowDirectory {Shadow}")]
    public class ShadowDirectory : IDisposable
    {
        private bool disposed;

        /// <summary>
        /// Creates a ShadowProcess instance.
        /// </summary>
        public ShadowDirectory(DirectoryInfo copyFrom)
        {
            this.Shadow = new DirectoryInfo(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));

            DirectoryCopy(copyFrom, this.Shadow, true);
        }

        public DirectoryInfo Shadow { get; private set; }

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;

            this.Shadow.Delete(true);
        }

        //stolen from https://msdn.microsoft.com/en-us/library/bb762914%28v=vs.110%29.aspx
        private static void DirectoryCopy(DirectoryInfo source, DirectoryInfo destination, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo[] dirs = source.GetDirectories();

            if (!source.Exists)
            {
                throw new DirectoryNotFoundException("Source directory does not exist or could not be found: " + source);
            }

            // If the destination directory doesn't exist, create it. 
            if (!destination.Exists)
            {
                destination.Create();
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = source.GetFiles();
            foreach (FileInfo file in files)
            {
                string newFilePath = Path.Combine(destination.FullName, file.Name);
                file.CopyTo(newFilePath, false);
            }

            // If copying subdirectories, copy them and their contents to new location. 
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destination.FullName, subdir.Name);
                    DirectoryCopy(subdir, new DirectoryInfo(temppath), true);
                }
            }
        }
    }
}