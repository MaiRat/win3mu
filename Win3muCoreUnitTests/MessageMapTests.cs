using Microsoft.VisualStudio.TestTools.UnitTesting;
using Win3muCore;
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
        public void ShouldBypassUnknownMessage32_ClassifiesAppDefinedAndKnownMessages()
        {
            Assert.IsTrue(MessageMap.ShouldBypassUnknownMessage32(1084));
            Assert.IsFalse(MessageMap.ShouldBypassUnknownMessage32(0x0288));
            Assert.IsFalse(MessageMap.ShouldBypassUnknownMessage32(0xC000));
        }

        [TestMethod]
        public void FormatUnknownMessageError_IncludesMessage176Name()
        {
            var message = MessageMap.FormatUnknownMessageError(0x00B0, () => "'edit' (Edit)");

            Assert.AreEqual("Unknown windows message EM_GETSEL(32) (0x00B0) for window class 'edit' (Edit)", message);
        }

        [TestMethod]
        public void FormatUnknownMessageError_ReentrantLookupFallsBackWithoutRecursing()
        {
            var message = MessageMap.FormatUnknownMessageError(0x00B0, () =>
            {
                throw new VirtualException(MessageMap.FormatUnknownMessageError(0x00B0, () => "'edit' (Edit)"));
            });

            Assert.AreEqual("Unknown windows message EM_GETSEL(32) (0x00B0)", message);
        }
    }
}
