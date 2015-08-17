using System;
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
    /// <summary>
    /// Parses the opencover results
    /// </summary>
    public interface ISubjectTestMappingParser
    {
        /// <summary>
        /// Parses the subject-test mapping from opencover results.xml
        /// </summary>
        IReadOnlyCollection<TestSubjectMethodRef> ParseMapping(
            ITestRunner testRunner,
            FileInfo testBinary,
            DirectoryInfo modelDir,
            XDocument resultsXml,
            string shadowDirFullName,
            IReadOnlyCollection<TestMethodRef> knownTestMethods);
    }

    /// <summary>
    /// Parses the opencover results
    /// </summary>
    public class SubjectTestMappingParser : ISubjectTestMappingParser
    {
        private readonly ILog log;

        public SubjectTestMappingParser(ILog log)
        {
            this.log = log;
        }

        /// <summary>
        /// Parses the subject-test mapping from opencover results.xml
        /// </summary>
        public IReadOnlyCollection<TestSubjectMethodRef> ParseMapping(
            ITestRunner testRunner,
            FileInfo testBinary,
            DirectoryInfo modelDir,
            XDocument resultsXml,
            string shadowDirFullName,
            IReadOnlyCollection<TestMethodRef> knownTestMethods)
        {
            var session = resultsXml.Root;

            var testBinaryHash = this.HashFile(testBinary);

            var results = new List<TestSubjectMethodRef>();

            var testMethodsDictionary = new Dictionary<uint, TestMethodRef>();

            var testModule = session.Element("Modules").Elements("Module")
                .Single(m => testBinaryHash.SequenceEqual(HashFromString(m.Attribute("hash").Value)));

            foreach (var trackedMethodEl in testModule.Element("TrackedMethods").Elements("TrackedMethod"))
            {
                var method = new MethodRef(testBinary, trackedMethodEl.Attribute("name").Value);

                var testMethod = knownTestMethods.SingleOrDefault(tm => tm.Method.Equals(method));
                if (testMethod == null)
                {
                    throw new Exception(
                        string.Format("Test method '{0}' from the coverage run result not found in the list recognized by the test runner. This is needed for test timing.", method.FullName));
                }

                testMethodsDictionary.Add((uint)trackedMethodEl.Attribute("uid"), testMethod);
            }

            foreach (var moduleEl in session.Element("Modules").Elements("Module"))
            {
                var shadowAssembly = new FileInfo(moduleEl.Element("FullName").Value);
                if (shadowAssembly.FullName.StartsWith(shadowDirFullName, StringComparison.OrdinalIgnoreCase))
                {
                    var relativePath = shadowAssembly.FullName.Substring(shadowDirFullName.Length);
                    //the file path from the original directory is the one we care about
                    var originalAssembly = new FileInfo(modelDir + relativePath);

                    foreach (var classEl in moduleEl.Element("Classes").Elements("Class"))
                    {
                        foreach (var metodEl in classEl.Element("Methods").Elements("Method"))
                        {
                            //first we read the test methods mapped to particular sequence points.
                            var testsByOffset = new Dictionary<int,List<uint>>();
                            foreach (var sequencePointElement in metodEl.Element("SequencePoints").Elements("SequencePoint"))
                            {
                                var offset = (int)sequencePointElement.Attribute("offset");

                                foreach (var trackedMethodRefEl in sequencePointElement.Descendants("TrackedMethodRef"))
                                {
                                    List<uint> list;
                                    if (!testsByOffset.TryGetValue(offset, out list))
                                    {
                                        list = new List<uint>();
                                        testsByOffset.Add(offset, list);
                                    }

                                    list.Add((uint)trackedMethodRefEl.Attribute("uid"));
                                }
                            }

                            //then we read all the tests, including the above
                            var allTests = new List<uint>();
                            foreach (var trackedMethodRefEl in metodEl.Descendants("TrackedMethodRef"))
                            {
                                allTests.Add((uint)trackedMethodRefEl.Attribute("uid"));
                            }

                            if (allTests.Count > 0)
                            {
                                var subjectMethod = new MethodRef(originalAssembly, metodEl.Element("Name").Value);
                                var subject = new TestSubjectMethodRef(
                                    subjectMethod,
                                    testsByOffset.Select(kvp => 
                                        Tuple.Create(kvp.Key, (IReadOnlyCollection<TestMethodRef>) kvp.Value.Select(v => testMethodsDictionary[v]).ToArray())).ToArray(),
                                    allTests.Select(v => testMethodsDictionary[v]).ToArray());
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
            if (string.IsNullOrWhiteSpace(dashDelimitedHexNumbers))
            {
                return new byte[0];
            }

            return dashDelimitedHexNumbers.Split('-')
                .Select(ch => byte.Parse(ch, System.Globalization.NumberStyles.HexNumber))
                .ToArray();
        }
    }
}

