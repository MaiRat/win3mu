using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
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
        public void WindowClassKind_Get_RecognizesEditClassAlias()
        {
            Assert.AreEqual(WndClassKind.Edit, WindowClassKind.Get("EditClass"));
        }

        [TestMethod]
        public void EditClass_EmCharFromPos_IsExplicitlyBypassed()
        {
            var map = new MessageMap();
            var messageInfosField = typeof(MessageMap).GetField("_messageInfos", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(messageInfosField);

            object mapping = null;
            foreach (var info in (System.Collections.IEnumerable)messageInfosField.GetValue(map))
            {
                var infoType = info.GetType();
                var wndClassKindField = infoType.GetField("WndClassKind");
                var message32Field = infoType.GetField("message32");
                Assert.IsNotNull(wndClassKindField);
                Assert.IsNotNull(message32Field);

                if ((WndClassKind)wndClassKindField.GetValue(info) == WndClassKind.Edit &&
                    (ushort)message32Field.GetValue(info) == Win32.EM_CHARFROMPOS)
                {
                    mapping = info;
                    break;
                }
            }

            Assert.IsNotNull(mapping);
            var mappingType = mapping.GetType();
            var message16Field = mappingType.GetField("message16");
            var semanticsField = mappingType.GetField("semantics");
            Assert.IsNotNull(message16Field);
            Assert.IsNotNull(semanticsField);
            Assert.AreEqual(Win32.EM_CHARFROMPOS, (ushort)message16Field.GetValue(mapping));
            Assert.IsInstanceOfType(semanticsField.GetValue(mapping), typeof(bypass));
        }

        [TestMethod]
        public void FormatUnknownMessageError_IncludesMessage176Name()
        {
            var message = MessageMap.FormatUnknownMessageError(0x00B0, () => "'edit' (Edit)");

            StringAssert.Contains(message, "EM_GETSEL(32) (0x00B0)");
            StringAssert.EndsWith(message, "for window class 'edit' (Edit)");
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
