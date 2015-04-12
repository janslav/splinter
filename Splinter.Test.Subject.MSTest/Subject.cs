namespace Splinter.Test.Subject.MSTest
{
    public class Subject
    {
        public int Dummy()
        {
            return 0;
        }

        public int Add(int left, int right)
        {
            return left + right;
        }

        public int WorkingAdd(int left, int right)
        {
            return left + right;
        }

        public int UncoveredAdd(int left, int right)
        {
            return left + right;
        }
    }
}
