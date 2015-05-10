using Microsoft.VisualStudio.TestTools.UnitTesting;
using Subj = Splinter.Test.Subject.MSTest;

namespace Splinter.Test.Test.MSTest
{
    [TestClass]
    public class SubjectTest
    {
        [TestMethod]
        public void ThisTestCoversOnlyUselessCode()
        {
            new Subj.Subject().DummyAdd(0, 0);
        }


        [TestMethod]
        public void ThisTestDoesntAssert()
        {
            new Subj.Subject().Add1(3, 0);
        }

        [TestMethod]
        public void ThisTestUsesImproperTestingData()
        {
            Assert.AreEqual(3, new Subj.Subject().Add2(3, 0));
        }

        [TestMethod]
        public void ThisTestShouldBeOk()
        {
            Assert.AreEqual(7, new Subj.Subject().Add3(3, 4));
        }
    }
}
