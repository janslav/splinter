﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Xml.Linq;
using System.Diagnostics;
using System.Security.Cryptography;

using log4net;

using Splinter.Contracts;
using Splinter.Contracts.DTOs;
using Splinter.Utils;

namespace Splinter.CoverageRunner.OpenCover
{
    public interface ISubjectTestMappingParser
    {
        /// <summary>
        /// Parses the subject-test mapping from opencover results.xml
        /// </summary>
        IReadOnlyCollection<TestSubjectMethodRef> ExtractMapping(FileInfo testBinary, DirectoryInfo shadowDir);
    }

    public class SubjectTestMappingParser : ISubjectTestMappingParser
    {
        private readonly ILog log;

        public SubjectTestMappingParser(ILog log)
        {
            this.log = log;
        }

        public IReadOnlyCollection<TestSubjectMethodRef> ExtractMapping(FileInfo testBinary, DirectoryInfo shadowDir)
        {
            var resultsXmlFile = new FileInfo(Path.Combine(shadowDir.FullName, ProcessInvoker.outputFileName));

            var resultsXml = XDocument.Load(resultsXmlFile.FullName);
            var session = resultsXml.Root;

            var testBinaryHash = this.HashFile(testBinary);

            var originalDir = testBinary.DirectoryName;

            var results = new List<TestSubjectMethodRef>();


            var testMethods = new Dictionary<uint, MethodRef>();

            var testModule = session.Element("Modules").Elements("Module")
                .Single(m => testBinaryHash.SequenceEqual(HashFromString(m.Attribute("hash").Value)));

            foreach (var trackedMethodEl in testModule.Element("TrackedMethods").Elements("TrackedMethod"))
            {
                var method = new MethodRef(testBinary, trackedMethodEl.Attribute("name").Value);

                testMethods.Add((uint) trackedMethodEl.Attribute("uid"), method);
            }

            foreach (var moduleEl in session.Element("Modules").Elements("Module"))
            {
                var shadowAssembly = new FileInfo(moduleEl.Element("FullName").Value);
                if (shadowAssembly.FullName.StartsWith(shadowDir.FullName, StringComparison.OrdinalIgnoreCase))
                {
                    var relativePath = shadowAssembly.FullName.Substring(shadowDir.FullName.Length);
                    //the file path from the original directory is the one we care about
                    var originalAssembly = new FileInfo(Path.Combine(originalDir, relativePath));

                    foreach (var classEl in moduleEl.Element("Classes").Elements("Class"))
                    {
                        foreach (var metodEl in classEl.Element("Methods").Elements("Method"))
                        {
                            var list = new HashSet<MethodRef>();

                            foreach (var trackedMethodRefEl in metodEl.Descendants("TrackedMethodRef"))
                            {
                                MethodRef testMethod;
                                if (testMethods.TryGetValue((uint) trackedMethodRefEl.Attribute("uid"), out testMethod))
                                {
                                    list.Add(testMethod);
                                }
                            }

                            if (list.Count > 0)
                            {
                                var subjectMethod = new MethodRef(originalAssembly, metodEl.Element("Name").Value);
                                var subject = new TestSubjectMethodRef(subjectMethod, list);
                                results.Add(subject);
                            }
                        }
                    }
                }
            }

            return results;
        }

        private byte[] HashFile(FileInfo file)
        {
            using (var sr = file.OpenRead())
            {
                using (var prov = new SHA1CryptoServiceProvider())
                {
                    return prov.ComputeHash(sr);
                }
            }
        }

        private byte[] HashFromString(string dashDelimitedHexNumbers)
        {
            return dashDelimitedHexNumbers.Split('-')
                .Select(ch => byte.Parse(ch, System.Globalization.NumberStyles.HexNumber))
                .ToArray();
        }
    }
}