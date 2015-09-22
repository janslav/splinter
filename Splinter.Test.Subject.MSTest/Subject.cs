using System;

namespace Splinter.Test.Subject.MSTest
{
    public class Subject
    {
        public void ThisDoesNothing()
        {
            //return;
        }

        public int Add(int left, int right)
        {
            return left + right;
        }

        public int Subtract(int left, int right)
        {
            return left - right;
        }

        public int Multiply(int left, int right)
        {
            return left * right;
        }

        /* To test output formating */
        public int MultiplyMultiLine(int left, int right)
        {
            /* Before */
            return
   left
   *
   right; /*After*/
        }

        /* To test output formating */
        public int MultiplySingleLine(int left, int right)
        {
            /* Before */
            return left * right; /*After*/
        }

        /* To test output formating */
        public int MultiplyTwoLines(int left, int right)
        {
            /* Before */
            return left *
                right; /*After*/
        }

        public int Divide(int left, int right)
        {
            if (right == 0)
            {
                throw new ArgumentException("Don't. Just don't.");
            }

            return left / right;
        }
    }
}
