using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SyncomaniaSolver;

namespace SyncomaniaSolverTests
{
    [TestClass]
    public class PriorityQueueTests
    {
        [TestMethod]
        public void Add_RemoveMin_IsValid()
        {
            var pq = new PriorityQueue<int>();
            pq.Add( 5 );
            pq.Add( 4 );
            pq.Add( 3 );
            pq.Add( 2 );
            pq.Add( 1 );

            var value = pq.RemoveMin();
            Assert.AreEqual( 1, value );

            value = pq.RemoveMin();
            Assert.AreEqual( 2, value );

            value = pq.RemoveMin();
            Assert.AreEqual( 3, value );

            value = pq.RemoveMin();
            Assert.AreEqual( 4, value );

            value = pq.RemoveMin();
            Assert.AreEqual( 5, value );
        }
    }
}
