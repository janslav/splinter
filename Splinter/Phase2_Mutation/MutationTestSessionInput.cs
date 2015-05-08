﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Splinter.Contracts;
using Splinter.Contracts.DTOs;

namespace Splinter.Phase2_Mutation
{
    public class MutationTestSessionInput
    {
        public MutationTestSessionInput(DirectoryInfo modelDirectory, TestSubjectMethodRef subject)
        {
            this.ModelDirectory = modelDirectory;
            this.Subject = subject;
        }

        public DirectoryInfo ModelDirectory { get; private set; }

        public TestSubjectMethodRef Subject { get; private set; }
    }
}
