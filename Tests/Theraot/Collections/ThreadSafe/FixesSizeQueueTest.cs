﻿using NUnit.Framework;
using System.Linq;
using Theraot.Collections.ThreadSafe;

namespace Tests.Theraot.Collections.ThreadSafe
{
    [TestFixture]
    internal class FixesSizeQueueTest
    {
        [Test]
        public void Fill()
        {
            var queue = new FixedSizeQueue<int>(64);
            foreach (var item in Enumerable.Range(0, queue.Capacity))
            {
                queue.TryAdd(item);
            }
            foreach (var item in Enumerable.Range(0, queue.Capacity))
            {
                int found;
                queue.TryTake(out found);
                Assert.AreEqual(found, item);
            }
        }

        [Test]
        public void FillPass()
        {
            var queue = new FixedSizeQueue<int>(64);
            foreach (var item in Enumerable.Range(0, queue.Capacity))
            {
                queue.TryAdd(item);
            }
            Assert.IsFalse(queue.TryAdd(999));
            foreach (var item in Enumerable.Range(0, queue.Capacity))
            {
                int found;
                queue.TryTake(out found);
                Assert.AreEqual(found, item);
            }
        }

        [Test]
        public void FillEnumerable()
        {
            var queue = new FixedSizeQueue<int>(Enumerable.Range(0, 64));
            Assert.IsFalse(queue.TryAdd(999));
            foreach (var item in Enumerable.Range(0, queue.Capacity))
            {
                int found;
                queue.TryTake(out found);
                Assert.AreEqual(found, item);
            }
        }
    }
}