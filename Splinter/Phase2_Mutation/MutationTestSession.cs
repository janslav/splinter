using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Registration;

using log4net;

using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Mdb;
using Mono.Cecil.Pdb;

using Splinter.Contracts;
using Splinter.Contracts.DTOs;

using Splinter;
using Splinter.Phase2_Mutation.NinjaTurtles;
using Splinter.Phase2_Mutation.NinjaTurtles.Turtles;

namespace Splinter.Phase2_Mutation
{
    public interface IMutationTestSession
    {
        MutationSessionResult Run(ITestRunner testRunner, TestSubjectMethodRef subject);
    }

    public class SingleMutationResult
    {
        IMethodTurtle mutation;

        //mutation place

        IReadOnlyCollection<MethodRef> passingTests;

        IReadOnlyCollection<MethodRef> failingTests;
    }

    public class MutationSessionResult
    {
        TestSubjectMethodRef subjectRef;

        IReadOnlyCollection<SingleMutationResult> mutationResults;
    }

    public class MutationTestSession : IMutationTestSession
    {
        private readonly ILog log;

        [ImportMany]
        private IEnumerable<IMethodTurtle> allTurtles = null; //assigning null to avoid compiler warning

        private readonly IModuleCache codeCache;

        public MutationTestSession(ILog log, IModuleCache codeCache)
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

        public MutationSessionResult Run(ITestRunner testRunner, TestSubjectMethodRef subjectRef)
        {
            var subjectMethod = this.GetMethodDef(subjectRef.Method);

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
