namespace Splinter.CoverageRunner.OpenCover
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Xml.Linq;

    using log4net;

    using Splinter.Contracts;
    using Splinter.Contracts.DTOs;
    using Splinter.Utils.Cecil;

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

        private readonly ICodeCache codeCache;

        public SubjectTestMappingParser(ILog log, ICodeCache codeCache)
        {
            this.log = log;
            this.codeCache = codeCache;
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

            var testBinaryHash = HashFile(testBinary);

            var results = new List<TestSubjectMethodRef>();

            var testModule = session.Element("Modules").Elements("Module")
                .Single(m => testBinaryHash.SequenceEqual(HashFromString(m.Attribute("hash").Value)));

            var testMethodsDictionary = (from trackedMethodEl in testModule.Element("TrackedMethods").Elements("TrackedMethod")
                                         let testMethod = knownTestMethods.SingleOrDefault(tm => tm.Method.Equals(new MethodRef(testBinary, (uint)trackedMethodEl.Attribute("name"))))
                                         where testMethod != null
                                         select new { uid = (uint)trackedMethodEl.Attribute("uid"), testMethod })
                                         .ToDictionary(p => p.uid, p => p.testMethod);

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
                            var testsByOffset = new Dictionary<int, List<uint>>();
                            foreach (var sequencePointElement in metodEl.Element("SequencePoints").Elements("SequencePoint"))
                            {
                                var offset = (int)sequencePointElement.Attribute("offset");

                                //TODO: convert this (and everything around?) to LINQ. Might get less ugly.
                                List<uint> list;
                                if (!testsByOffset.TryGetValue(offset, out list))
                                {
                                    list = new List<uint>();
                                    testsByOffset.Add(offset, list);
                                }

                                foreach (var trackedMethodRefEl in sequencePointElement.Descendants("TrackedMethodRef"))
                                {
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
                                // we're getting fullname by fullname, or in other words, we're checking that we're able to find the method
                                var token = (uint)metodEl.Element("MetadataToken");
                                var method = this.codeCache.GetAssemblyDefinition(originalAssembly)
                                    .GetMethodByMetaDataToken(token);

                                if (!string.Equals(method.FullName, metodEl.Element("Name").Value))
                                {
                                    throw new Exception(string.Format("The method found under metadataToken {0} is not called '{1}' as expected.", token, method.FullName));
                                }

                                var subjectMethod = new MethodRef(originalAssembly, token);

                                var subject = new TestSubjectMethodRef(
                                    subjectMethod,
                                    testsByOffset.Select(kvp =>
                                        Tuple.Create(kvp.Key, (IReadOnlyCollection<TestMethodRef>)kvp.Value.Select(v => testMethodsDictionary[v]).ToArray())).ToArray(),
                                    allTests.Select(v => testMethodsDictionary[v]).ToArray());
                                results.Add(subject);
                            }
                        }
                    }
                }
            }

            return results;
        }

        private static byte[] HashFile(FileInfo file)
        {
            using (var sr = file.OpenRead())
            {
                using (var prov = new SHA1CryptoServiceProvider())
                {
                    return prov.ComputeHash(sr);
                }
            }
        }

        private static byte[] HashFromString(string dashDelimitedHexNumbers)
        {
            if (string.IsNullOrWhiteSpace(dashDelimitedHexNumbers))
            {
                return new byte[0];
            }

            return dashDelimitedHexNumbers.Split('-')
                .Select(ch => byte.Parse(ch, NumberStyles.HexNumber))
                .ToArray();
        }
    }
}

