using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;

using FluentAssertions;
using Moq;

using Splinter;
using Splinter.Contracts;
using Splinter.Contracts.DTOs;
using Splinter.Phase2_Mutation.NinjaTurtles;
using Splinter.Phase2_Mutation.NinjaTurtles.Turtles;
using Splinter.Utils.Cecil;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Splinter.Phase2_Mutation.TestsOrderingStrategies
{
    /// <summary>
    /// Tests the test ordering strategies
    /// </summary>
    [TestClass]
    public class TestOrderingTest
    {
        [TestMethod]
        public void ByFragsOrderingPicksTopScoreFirst()
        {
            var fakeRunner = new Mock<ITestRunner>();
            fakeRunner.Setup(r => r.Equals(fakeRunner.Object)).Returns(true);

            var passingTest = new TestMethodRef(new MethodRef(new FileInfo("file"), 1), fakeRunner.Object, TimeSpan.Zero);
            var failingTest = new TestMethodRef(new MethodRef(new FileInfo("file"), 2), fakeRunner.Object, TimeSpan.Zero);
            var timeoutingTest = new TestMethodRef(new MethodRef(new FileInfo("file"), 3), fakeRunner.Object, TimeSpan.Zero);

            var sut = new TestOrderingByFrags();

            sut.NotifyTestPassed(null, passingTest, TimeSpan.Zero);
            sut.NotifyTestFailed(null, failingTest, TimeSpan.Zero);
            sut.NotifyTestTimedOut(null, timeoutingTest, TimeSpan.Zero);

            var ordered = sut.OrderTestsForRunning(new[] { timeoutingTest, passingTest, failingTest }).ToList();

            //the failing test (i.e. one succesful at killing mutant should go first, timeouting last)
            ordered.Should().ContainInOrder(failingTest, passingTest, timeoutingTest);
        }

        [TestMethod]
        public void ByRunTimeOrderingPicksFastestFirst()
        {
            var fakeRunner = new Mock<ITestRunner>();
            fakeRunner.Setup(r => r.Equals(fakeRunner.Object)).Returns(true);

            var slowTest = new TestMethodRef(new MethodRef(new FileInfo("file"), 1), fakeRunner.Object, TimeSpan.Zero);
            var fastTest = new TestMethodRef(new MethodRef(new FileInfo("file"), 2), fakeRunner.Object, TimeSpan.Zero);

            var sut = new TestOrderingByRunTime();

            sut.NotifyTestPassed(null, slowTest, TimeSpan.FromSeconds(1));
            sut.NotifyTestPassed(null, slowTest, TimeSpan.FromSeconds(10));
            sut.NotifyTestFailed(null, fastTest, TimeSpan.FromSeconds(1));
            sut.NotifyTestFailed(null, fastTest, TimeSpan.FromSeconds(1));


            var ordered = sut.OrderTestsForRunning(new[] { slowTest, fastTest }).ToList();

            //the failing test (i.e. one succesful at killing mutant should go first, timeouting last)
            ordered.Should().ContainInOrder(fastTest, slowTest);
        }
    }
}