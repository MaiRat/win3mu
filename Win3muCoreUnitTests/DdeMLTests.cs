using Microsoft.VisualStudio.TestTools.UnitTesting;
using Win3muCore;

namespace Win3muCoreUnitTests
{
    [TestClass]
    public class DdeMLTests
    {
        [TestMethod]
        public void CreateQueryAndFreeStringHandle_RoundTripsStringLength()
        {
            var dde = new DdeML();

            var handle = dde.DdeCreateStringHandle(1, "System", 0);

            Assert.AreNotEqual((ushort)0, handle);
            Assert.AreEqual((uint)6, dde.DdeQueryString(1, handle, 0, 0, 0));
            Assert.AreEqual(0, dde.DdeCmpStringHandles(handle, handle));
            Assert.IsTrue(dde.DdeFreeStringHandle(1, handle));
            Assert.AreEqual((uint)0, dde.DdeQueryString(1, handle, 0, 0, 0));
        }

        [TestMethod]
        public void DuplicateAndKeptHandles_ReferenceCountUntilFinalFree()
        {
            var dde = new DdeML();

            var first = dde.DdeCreateStringHandle(1, "Topic", 0);
            var second = dde.DdeCreateStringHandle(1, "Topic", 0);

            Assert.AreEqual(first, second);
            Assert.IsTrue(dde.DdeKeepStringHandle(1, first));
            Assert.IsTrue(dde.DdeFreeStringHandle(1, first));
            Assert.AreEqual((uint)5, dde.DdeQueryString(1, first, 0, 0, 0));
            Assert.IsTrue(dde.DdeFreeStringHandle(1, second));
            Assert.AreEqual((uint)5, dde.DdeQueryString(1, first, 0, 0, 0));
            Assert.IsTrue(dde.DdeFreeStringHandle(1, first));
            Assert.AreEqual((uint)0, dde.DdeQueryString(1, first, 0, 0, 0));
        }

        [TestMethod]
        public void CompareStringHandles_UsesStringValues()
        {
            var dde = new DdeML();

            var alpha = dde.DdeCreateStringHandle(1, "Alpha", 0);
            var beta = dde.DdeCreateStringHandle(1, "Beta", 0);

            Assert.IsTrue(dde.DdeCmpStringHandles(alpha, beta) < 0);
            Assert.IsTrue(dde.DdeCmpStringHandles(beta, alpha) > 0);
        }
    }
}
