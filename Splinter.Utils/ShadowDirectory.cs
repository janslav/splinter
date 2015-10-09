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

namespace Splinter.Utils
{
    using System;
    using System.Diagnostics;
    using System.IO;

    using log4net;

    /// <summary>
    /// The point of ShadowDirectory is to create a temporary ("shadow") directory by copying the contents of a specified model.
    /// It then cleans up this directory on calling Dispose()
    /// </summary>
    [DebuggerDisplay("ShadowDirectory {Model} -> {Shadow}")]
    public class ShadowDirectory : IDisposable
    {
        private readonly ILog log;

        private readonly string operationId;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShadowDirectory"/> class.
        /// </summary>
        public ShadowDirectory(ILog log, DirectoryInfo model, string operationId)
        {
            this.log = log;
            this.Model = model;
            this.operationId = operationId;

            this.Shadow = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "Splinter", Path.GetRandomFileName()));

            this.log.DebugFormat("{0}: Copying root directory to '{1}'.", this.operationId, this.Shadow.FullName);
            DirectoryCopy(model, this.Shadow);
        }

        /// <summary>
        /// Gets the model directory.
        /// </summary>
        public DirectoryInfo Model { get; private set; }

        /// <summary>
        /// Gets the shadow directory.
        /// </summary>
        public DirectoryInfo Shadow { get; private set; }

        /// <summary>
        /// Gets the equivalent shadow path of a specified "source" path
        /// </summary>
        /// <remarks>
        /// If the model is "C:\src" and the specified fileInSourceDir is "C:\src\A\B\C\file.txt" and the shadow is "C:\temp\xyz"
        /// then this method returns "C:\temp\xyz\A\B\C\file.txt"
        /// </remarks>
        public FileInfo GetEquivalentShadowPath(FileInfo fileInSourceDir)
        {
            if (!fileInSourceDir.FullName.StartsWith(this.Model.FullName, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Provided file is not part of the model path.", "fileInSourceDir");
            }

            var relativePath = fileInSourceDir.FullName.Substring(this.Model.FullName.Length);
            //the file path from the original directory is the one we care about
            var shadowed = new FileInfo(this.Shadow.FullName + relativePath);

            return shadowed;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="ShadowDirectory"/> class.
        /// </summary>
        ~ShadowDirectory()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        private void Dispose(bool disposing)
        {
            try
            {
                var s = this.Shadow;
                if (s != null)
                {
                    var l = this.log;
                    if (l != null)
                    {
                        l.DebugFormat("{0}: Deleting directory '{1}'.", this.operationId, s.FullName);
                    }

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
                throw new DirectoryNotFoundException("model directory does not exist or could not be found: " + source);
            }

            // If the destination directory doesn't exist, create it. 
            if (!destination.Exists)
            {
                destination.Create();
            }

            // Get the files in the directory and copy them to the new location.
            foreach (var file in source.GetFiles())
            {
                var newFilePath = Path.Combine(destination.FullName, file.Name);
                file.CopyTo(newFilePath, false);
            }

            // recursively copy subdirectories and their contents to new location. 
            foreach (var subdir in source.GetDirectories())
            {
                var temppath = Path.Combine(destination.FullName, subdir.Name);
                DirectoryCopy(subdir, new DirectoryInfo(temppath));
            }
        }
    }
}