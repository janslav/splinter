using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Registration;
using System.Diagnostics;

using log4net;

using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Mdb;
using Mono.Cecil.Pdb;

using Splinter.Contracts;
using Splinter.Contracts.DTOs;
using Splinter.Utils.Cecil;
using Splinter;
using Splinter.Phase2_Mutation.NinjaTurtles;
using Splinter.Phase2_Mutation.NinjaTurtles.Turtles;
using Splinter.Phase2_Mutation.DTOs;

namespace Splinter.Phase2_Mutation
{
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

        public MutationTestSession(ILog log, ICodeCache codeCache)
        {
            this.log = log;
            this.codeCache = codeCache;

            this.ImportTurtles();
        }

        /// <summary>
        /// Turtles = the implementatinos of method code mutators
        /// </summary>
        private void ImportTurtles()
        {
            //this is how you MEF-resolve stuff that wasn't made for MEF
            var registration = new RegistrationBuilder();
            registration.ForTypesDerivedFrom<IMethodTurtle>().ExportInterfaces();

            var catalog = new ApplicationCatalog(registration);

            var compositionContainer = new CompositionContainer(catalog);
            compositionContainer.ComposeParts(this);
        }

        public IReadOnlyCollection<SingleMutationTestResult> Run(MutationTestSessionInput input)
        {
            var mutations = this.allTurtles.SelectMany(t => t.TryCreateMutants(input)).ToArray();

            try
            {
                var results = mutations.AsParallel()
                    .Select(mutation =>
                    {
                        var failingTests = new List<MethodRef>();
                        var passingTests = new List<MethodRef>();

                        using (mutation)
                        {
                            foreach (var test in mutation.Input.Subject.TestMethods)
                            {
                                var shadowedTestAssembly = mutation.TestDirectory.GetEquivalentShadowPath(test.Method.Assembly);
                                var processInfo = test.Runner.GetProcessInfoToRunTest(mutation.TestDirectory.Shadow, shadowedTestAssembly, test.Method.FullName);

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
                        }

                        return new SingleMutationTestResult(
                            mutation.Input.Subject.Method,
                            mutation.InstructionIndex,
                            mutation.Description,
                            passingTests,
                            failingTests);
                    });

                return results.ToArray();
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

        private MethodDefinition GetMethodDef(MethodRef method)
        {
            return this.codeCache.GetAssembly(method.Assembly).GetMethodByFullName(method.FullName);
        }

        //private void PopulateDefaultTurtles()
        //{
        //    IMethodTurtle

        //    foreach (var type in GetType().Assembly.GetTypes()
        //        .Where(t => t.GetInterface("IMethodTurtle") != null
        //        && !t.IsAbstract))
        //    {
        //        _mutationsToApply.Add(type);
        //    }
        //}
    }
}
