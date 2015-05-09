using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Registration;
using System.Diagnostics;

using log4net;

using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Mdb;
using Mono.Cecil.Pdb;

using Splinter;
using Splinter.Contracts;
using Splinter.Contracts.DTOs;
using Splinter.Phase2_Mutation.NinjaTurtles;
using Splinter.Phase2_Mutation.NinjaTurtles.Turtles;
using Splinter.Phase2_Mutation.DTOs;
using Splinter.Utils.Cecil;

namespace Splinter.Phase2_Mutation
{
    /// <summary>
    /// The creation of mutated assemblies and running tests against them is driven from here.
    /// </summary>
    public interface IMutationTestSession
    {
        IReadOnlyCollection<SingleMutationTestResult> Run(MutationTestSessionInput input);
    }

    public class MutationTestSession : IMutationTestSession
    {
        private readonly ILog log;

        [ImportMany]
        private IEnumerable<IMethodTurtle> allTurtles = null; //assigning null to avoid compiler warning

        private readonly ICodeCache codeCache;

        private readonly IWindowsErrorReporting errorReportingSwitch;

        public MutationTestSession(ILog log, ICodeCache codeCache, IWindowsErrorReporting errorReportingSwitch)
        {
            this.log = log;
            this.codeCache = codeCache;
            this.errorReportingSwitch = errorReportingSwitch;

            this.ImportTurtles();
        }

        /// <summary>
        /// Turtles = the implementatinos of method code mutators
        /// </summary>
        private void ImportTurtles()
        {
            var catalog = new ApplicationCatalog();
            var compositionContainer = new CompositionContainer(catalog);
            compositionContainer.ComposeParts(this);
        }

        public IReadOnlyCollection<SingleMutationTestResult> Run(MutationTestSessionInput input)
        {
            using (this.errorReportingSwitch.TurnOffErrorReporting())
            {
                var unmutableMethods = new List<SingleMutationTestResult>();

                var mutations = this.allTurtles.SelectMany(t =>
                {
                    var mutants = t.TryCreateMutants(input);
                    if (mutants.Count == 0)
                    {
                        unmutableMethods.Add(new SingleMutationTestResult(
                                input.Subject.Method,
                                0,
                                "",
                                input.Subject.TestMethods.Select(tm => tm.Method).ToArray(),
                                new MethodRef[0]));
                    }
                    return mutants;
                });

                try
                {
                    var results = mutations.AsParallel().Select(mutation =>
                    {
                        var failingTests = new List<MethodRef>();
                        var passingTests = new List<MethodRef>();

                        using (mutation)
                        {
                            foreach (var test in mutation.Input.Subject.TestMethods)
                            {
                                var shadowedTestAssembly = mutation.TestDirectory.GetEquivalentShadowPath(test.Method.Assembly);
                                var processInfo = test.Runner.GetProcessInfoToRunTest(mutation.TestDirectory.Shadow, shadowedTestAssembly, test.Method.FullName);

                                this.log.DebugFormat(
                                    "Running test '{0}' for mutation '{1}' in method '{2}'.",
                                    test.Method.FullName,
                                    mutation.Description,
                                    mutation.Input.Subject.Method.FullName);

                                using (var p = Process.Start(processInfo))
                                {
                                    var runnerName = test.Runner.Name;

                                    p.OutputDataReceived += (_, e) => this.log.Debug(runnerName + ": " + e.Data);
                                    p.ErrorDataReceived += (_, e) => this.log.Warn(runnerName + ": " + e.Data);

                                    p.WaitForExit();

                                    if (p.ExitCode == 0)
                                    {
                                        passingTests.Add(test.Method);
                                    }
                                    else
                                    {
                                        failingTests.Add(test.Method);
                                    }
                                }
                            }

                            return new SingleMutationTestResult(
                                mutation.Input.Subject.Method,
                                mutation.InstructionIndex,
                                mutation.Description,
                                passingTests,
                                failingTests);
                        }
                    });

                    unmutableMethods.AddRange(results);
                    return unmutableMethods;
                }
                finally
                {
                    //a second failsafe. We really don't want to leave the copied directories around.
                    foreach (var mutation in mutations)
                    {
                        mutation.Dispose();
                    }
                }
            }
        }

        private MethodDefinition GetMethodDef(MethodRef method)
        {
            return this.codeCache.GetAssembly(method.Assembly).GetMethodByFullName(method.FullName);
        }
    }
}
