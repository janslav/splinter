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
            new Subj.Subject().ThisDoesNothing();
        }

        [TestMethod]
        public void ThisTestDoesntAssert()
        {
            new Subj.Subject().Multiply(3, 7);
        }

       /* [TestMethod]
        public void ThisTestDoesntAssertOnMultiLineMethod()
        {
            new Subj.Subject().MultiplyMultiLine(3, 7);
        }

        [TestMethod]
        public void ThisTestDoesntAssertOnTwoLineMethod()
        {
            new Subj.Subject().MultiplyTwoLines(3, 7);
        }

        [TestMethod]
        public void ThisTestDoesntAssertOnSingleLineMethod()
        {
            new Subj.Subject().MultiplySingleLine(3, 7);
        }
        
                    ///* To test output formating */
        //public int MultiplyMultiLine(int left, int right)
        //{
        //    /* Before */ return 
        //        left 
        //        * 
        //        right; /*After*/
        //}

        ///* To test output formating */
        //public int MultiplySingleLine(int left, int right)
        //{
        //    /* Before */ return left * right; /*After*/
        //}

        ///* To test output formating */
        //public int MultiplyTwoLines(int left, int right)
        //{
        //    /* Before */
        //    return left * 
        //        right; /*After*/
        //}
            

        [TestMethod]
        public void ThisTestUsesImproperTestingData()
        {
            Assert.AreEqual(3, new Subj.Subject().Subtract(3, 0));
        }

        [TestMethod]
        public void ThisTestsTheHappyPath()
        {
            Assert.AreEqual(7, new Subj.Subject().Divide(21, 3));
        }

        //[TestMethod, ExpectedException(typeof(System.ArgumentException))]
        //public void ThisTestsTheExceptionalCase()
        //{
        //    new Subj.Subject().Divide(21, 0);
        //}
    }
}
