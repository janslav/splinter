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
    [DebuggerDisplay("ShadowDirectory {Source} -> {Shadow}")]
    public class ShadowDirectory : IDisposable
    {
        /// <summary>
        /// Creates a ShadowProcess instance.
        /// </summary>
        public ShadowDirectory(DirectoryInfo source)
        {
            this.Source = source;

            this.Shadow = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "Splinter", Path.GetRandomFileName()));

            DirectoryCopy(source, this.Shadow);
        }

        public DirectoryInfo Source { get; private set; }

        public DirectoryInfo Shadow { get; private set; }

        public FileInfo GetEquivalentShadowPath(FileInfo fileInSourceDir)
        {
            if (!fileInSourceDir.FullName.StartsWith(this.Source.FullName, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Provided file is not part of the source path.", "fileInSourceDir");
            }

            var relativePath = fileInSourceDir.FullName.Substring(this.Source.FullName.Length);
            //the file path from the original directory is the one we care about
            var shadowed = new FileInfo(this.Shadow.FullName + relativePath);

            return shadowed;
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ShadowDirectory()
        {
            this.Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            try
            {
                var s = this.Shadow;
                if (s != null)
                {
                    s.Delete(true);
                    this.Shadow = null;
                }
            }
            catch
            {
                //do we care really?
            }
        }

        //stolen from https://msdn.microsoft.com/en-us/library/bb762914%28v=vs.110%29.aspx
        private static void DirectoryCopy(DirectoryInfo source, DirectoryInfo destination)
        {
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
            foreach (FileInfo file in source.GetFiles())
            {
                string newFilePath = Path.Combine(destination.FullName, file.Name);
                file.CopyTo(newFilePath, false);
            }

            // recursively copy subdirectories and their contents to new location. 
            foreach (DirectoryInfo subdir in source.GetDirectories())
            {
                string temppath = Path.Combine(destination.FullName, subdir.Name);
                DirectoryCopy(subdir, new DirectoryInfo(temppath));
            }
        }
    }
}