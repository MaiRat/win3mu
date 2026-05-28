using Microsoft.VisualStudio.TestTools.UnitTesting;
using Win3muCore;

namespace Win3muCoreUnitTests
{
    [TestClass]
    public class UserTests
    {
        [TestMethod]
        public void UserModule_ExportsRecentStubOrdinals()
        {
            var user = new User();

            Assert.AreEqual((ushort)0x00E2, user.GetOrdinalFromName("LockInput"));
            Assert.AreEqual((ushort)0x00E6, user.GetOrdinalFromName("GetNextWindow"));
            Assert.AreEqual((ushort)0x00F7, user.GetOrdinalFromName("GetCursor"));
            Assert.AreEqual((ushort)0x0108, user.GetOrdinalFromName("GetMenuItemID"));
            Assert.AreEqual((ushort)0x0116, user.GetOrdinalFromName("GetDesktopHwnd"));
            Assert.AreEqual((ushort)0x014C, user.GetOrdinalFromName("UserYield"));
            Assert.AreEqual((ushort)0x0166, user.GetOrdinalFromName("IsMenu"));
            Assert.AreEqual((ushort)0x01B1, user.GetOrdinalFromName("IsCharAlpha"));
            Assert.AreEqual((ushort)0x01E2, user.GetOrdinalFromName("EnableScrollBar"));
        }

        [TestMethod]
        public void User_CharHelpers_ClassifyAnsiCharacters()
        {
            var user = new User();

            Assert.IsTrue(user.IsCharAlpha((ushort)'A'));
            Assert.IsTrue(user.IsCharAlphaNumeric((ushort)'7'));
            Assert.IsTrue(user.IsCharUpper((ushort)'Z'));
            Assert.IsTrue(user.IsCharLower((ushort)'z'));

            Assert.IsFalse(user.IsCharAlpha((ushort)'7'));
            Assert.IsFalse(user.IsCharAlphaNumeric((ushort)'?'));
            Assert.IsFalse(user.IsCharUpper((ushort)'a'));
            Assert.IsFalse(user.IsCharLower((ushort)'A'));
        }

        [TestMethod]
        public void User_SimpleCompatibilityStubs_ReturnExpectedDefaults()
        {
            var user = new User();

            Assert.IsTrue(user.LockInput(true));
            Assert.AreEqual((ushort)0, user.GetSystemDebugState());
        }
    }
}
