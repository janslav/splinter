using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Splinter.Contracts.DTOs
{
    /// <summary>
    /// Represents a test method
    /// </summary>
    [DebuggerDisplay("TestMethodRef {Method.FullName} Runner: {Runner.Name}")]
    public class TestMethodRef
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TestMethodRef"/> class.
        /// </summary>
        public TestMethodRef(MethodRef method, ITestRunner testRunner)
        {
            this.Method = method;
            this.Runner = testRunner;
        }

        /// <summary>
        /// Gets the method name and location.
        /// </summary>
        public MethodRef Method { get; private set; }

        /// <summary>
        /// Gets the runner that was used to run this test method.
        /// </summary>
        public ITestRunner Runner { get; private set; }

        #region Equals & GetHashCode
        /// <summary>
        /// Determines whether the specified <see cref="System.Object" />, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (obj.GetType() != typeof(TestMethodRef))
            {
                return false;
            }

            var o = (TestMethodRef)obj;

            return this.Method.Equals(o.Method) && this.Runner.Equals(o.Runner);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return this.Method.GetHashCode() * 17 + this.Runner.GetHashCode();
        }
        #endregion
    }
}
