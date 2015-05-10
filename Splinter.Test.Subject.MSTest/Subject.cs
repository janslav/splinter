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
