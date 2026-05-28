using Microsoft.VisualStudio.TestTools.UnitTesting;
using Win3muCore.MessageSemantics;

namespace Win3muCoreUnitTests
{
    [TestClass]
    public class MessageMapTests
    {
        [TestMethod]
        public void LookupMessage32_BypassesImeRequest()
        {
            var map = new MessageMap();

            var semantics = map.LookupMessage32(System.IntPtr.Zero, 0x0288, out var message16);

            Assert.AreEqual((ushort)0x0288, message16);
            Assert.IsInstanceOfType<bypass>(semantics);
        }
    }
}
