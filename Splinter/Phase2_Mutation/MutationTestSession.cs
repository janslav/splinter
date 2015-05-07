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

using NinjaTurtles;
using NinjaTurtles.Turtles;

namespace Splinter.Phase2_Mutation
{
    public interface IMutationTestSession
    {
        void Start(ITestRunner testRunner, TestSubjectMethodRef subject);
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

        public void Start(ITestRunner testRunner, TestSubjectMethodRef subjectRef)
        {
            var subjectMethod = this.GetMethodDefinition(subjectRef.Method);
        }

        private MethodReference GetMethodDefinition(MethodRef method)
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
