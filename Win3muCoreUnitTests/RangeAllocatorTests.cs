using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Win3muCore;

namespace Win3muCoreUnitTests
{
    [TestClass]
    public class RangeAllocatorTests
    {
        [TestMethod]
        public void Shrink_FreeTrailingSpace_Succeeds()
        {
            var allocator = new RangeAllocator<string>(1000);
            Assert.AreEqual(1000, allocator.AddressSpaceSize);
            Assert.AreEqual(1000, allocator.FreeSpace);

            // Shrink to 500
            allocator.AddressSpaceSize = 500;
            Assert.AreEqual(500, allocator.AddressSpaceSize);
            Assert.AreEqual(500, allocator.FreeSpace);
        }

        [TestMethod]
        public void Shrink_AfterAllocation_WithFreeTrail_Succeeds()
        {
            var allocator = new RangeAllocator<string>(1000);
            var range = allocator.Alloc(200, false, false);
            Assert.IsNotNull(range);
            Assert.AreEqual(800, allocator.FreeSpace);

            // Shrink to 600 — 400 bytes of free tail remain after the 200-byte allocation
            allocator.AddressSpaceSize = 600;
            Assert.AreEqual(600, allocator.AddressSpaceSize);
            Assert.AreEqual(400, allocator.FreeSpace);
        }

        [TestMethod]
        public void Shrink_ToExactAllocationBoundary_Succeeds()
        {
            var allocator = new RangeAllocator<string>(1000);
            var range = allocator.Alloc(500, false, false);
            Assert.IsNotNull(range);

            // Shrink exactly to the end of the allocation
            allocator.AddressSpaceSize = 500;
            Assert.AreEqual(500, allocator.AddressSpaceSize);
            Assert.AreEqual(0, allocator.FreeSpace);
        }

        [TestMethod]
        public void Shrink_IntoAllocatedRegion_Throws()
        {
            var allocator = new RangeAllocator<string>(1000);
            var range = allocator.Alloc(800, false, false);
            Assert.IsNotNull(range);

            // Try to shrink below the allocated region — should fail
            bool threw = false;
            try
            {
                allocator.AddressSpaceSize = 500;
            }
            catch (InvalidOperationException)
            {
                threw = true;
            }
            Assert.IsTrue(threw, "Expected InvalidOperationException when shrinking into allocated region");
        }

        [TestMethod]
        public void Shrink_ThenGrow_RoundTrips()
        {
            var allocator = new RangeAllocator<string>(1000);
            allocator.AddressSpaceSize = 500;
            Assert.AreEqual(500, allocator.AddressSpaceSize);
            Assert.AreEqual(500, allocator.FreeSpace);

            allocator.AddressSpaceSize = 800;
            Assert.AreEqual(800, allocator.AddressSpaceSize);
            Assert.AreEqual(800, allocator.FreeSpace);
        }

        [TestMethod]
        public void Shrink_SameSize_NoOp()
        {
            var allocator = new RangeAllocator<string>(1000);
            allocator.AddressSpaceSize = 1000;
            Assert.AreEqual(1000, allocator.AddressSpaceSize);
            Assert.AreEqual(1000, allocator.FreeSpace);
        }
    }
}
