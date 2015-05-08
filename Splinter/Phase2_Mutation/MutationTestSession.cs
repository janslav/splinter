﻿using System;
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

namespace Splinter.Phase2_Mutation
{
    public interface IMutationTestSession
    {
        MutationTestSessionResult Run(MutationTestSessionInput input);
    }

    public class SingleMutationResult
    {
        IMethodTurtle mutation;

        //mutation place

        IReadOnlyCollection<MethodRef> passingTests;

        IReadOnlyCollection<MethodRef> failingTests;
    }

    public class MutationTestSessionResult
    {
        TestSubjectMethodRef subjectRef;

        IReadOnlyCollection<SingleMutationResult> mutationResults;
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

        public MutationTestSessionResult Run(MutationTestSessionInput input)
        {
            var mutations = this.allTurtles.SelectMany(t => t.TryCreateMutants(input)).ToArray();

            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount * 8,
            };

            try
            {
                var state = Parallel.ForEach(mutations,
                    mutation =>
                    {
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

                                    }
                                    else
                                    {

                                    }
                                }

                            }
                        }
                    });
            }
            finally
            {
                //a second failsafe. We really don't want to leave the copied directories around.
                foreach (var mutation in mutations)
                {
                    mutation.Dispose();
                }
            }

            return null;
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
