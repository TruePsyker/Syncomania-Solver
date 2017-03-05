using System;
using System.Collections.Generic;
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
            var pq = new PriorityQueue<int>(32000);
            var check = new List<int>(32000);
            var rnd = new Random();

            int recordsCount = 10000;

            for ( int i = 0; i < recordsCount; i++ )
            {
                int rec = rnd.Next();
                pq.Add( rec );
                check.Add(rec);
            }

            check.Sort();

            for ( int i = 0; i < recordsCount; i++ )
            {
                Assert.AreEqual( check[i], pq.RemoveMin() );
            }
        }

        [TestMethod]
        public void ExtendCapacity_IsValid()
        {
            var pq = new PriorityQueue<int>(2);

            Assert.AreEqual( 2, pq.Capacity );

            pq.Add(1);
            pq.Add(2);

            Assert.AreEqual( 4, pq.Capacity );
        }
    }
}
