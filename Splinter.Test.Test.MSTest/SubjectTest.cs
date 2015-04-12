using Microsoft.VisualStudio.TestTools.UnitTesting;
using Subj = Splinter.Test.Subject.MSTest;

namespace Splinter.Test.Test.MSTest
{
    [TestClass]
    public class SubjectTest
    {
        [TestMethod]
        public void Dummy_Dummies()
        {
            Assert.AreEqual(0, new Subj.Subject().Dummy());
        }

        [TestMethod]
        public void Add_Works()
        {
            Assert.AreEqual(3, new Subj.Subject().Add(3, 0));
        }

        [TestMethod]
        public void WorkingAdd_Works()
        {
            Assert.AreEqual(3, new Subj.Subject().WorkingAdd(3, 0));
            Assert.AreEqual(7, new Subj.Subject().WorkingAdd(3, 4));
        }
    }
}
