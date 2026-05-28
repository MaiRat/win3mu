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
            Assert.IsInstanceOfType(semantics, typeof(bypass));
        }

        [TestMethod]
        public void ShouldBypassUnknownMessage32_ReturnsTrueForAppDefinedMessage()
        {
            Assert.IsTrue(MessageMap.ShouldBypassUnknownMessage32(1084));
            Assert.IsFalse(MessageMap.ShouldBypassUnknownMessage32(0x0288));
            Assert.IsFalse(MessageMap.ShouldBypassUnknownMessage32(0xC000));
        }
    }
}
