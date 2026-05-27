using Microsoft.VisualStudio.TestTools.UnitTesting;
using Win3muCore;

namespace Win3muCoreUnitTests
{
    [TestClass]
    public class MMSystemTests
    {
        [TestMethod]
        public void UsesMciDgvOpenParams_DetectsDeviceSpecificOpenFlags()
        {
            Assert.IsFalse(MMSystem.UsesMciDgvOpenParams(Win16.MCI_OPEN_ELEMENT | Win16.MCI_OPEN_TYPE));
            Assert.IsTrue(MMSystem.UsesMciDgvOpenParams(Win16.MCI_OPEN_TYPE | WinCommon.MCI_DGV_OPEN_PARENT));
            Assert.IsTrue(MMSystem.UsesMciDgvOpenParams(WinCommon.MCI_DGV_OPEN_WS));
        }

        [TestMethod]
        public void ConvertMciDgvRectParams_RoundTripsRectCoordinates()
        {
            var st16 = new Win16.MCI_DGV_RECT_PARMS()
            {
                dwCallback = 0x12345678,
                rc = new Win16.RECT() { Left = 1, Top = 2, Right = 30, Bottom = 40 },
            };

            var st32 = MMSystem.ConvertMciDgvRectParams(st16);
            var roundTrip = MMSystem.ConvertMciDgvRectParams(st32);

            Assert.AreEqual(1, st32.rc.Left);
            Assert.AreEqual(2, st32.rc.Top);
            Assert.AreEqual(30, st32.rc.Right);
            Assert.AreEqual(40, st32.rc.Bottom);
            Assert.AreEqual(st16.rc.Left, roundTrip.rc.Left);
            Assert.AreEqual(st16.rc.Top, roundTrip.rc.Top);
            Assert.AreEqual(st16.rc.Right, roundTrip.rc.Right);
            Assert.AreEqual(st16.rc.Bottom, roundTrip.rc.Bottom);
        }

        [TestMethod]
        public void ConvertMciDgvWindowParams_RoundTripsCommandShowState()
        {
            var st16 = new Win16.MCI_DGV_WINDOW_PARMS()
            {
                nCmdShow = 7,
            };

            var st32 = MMSystem.ConvertMciDgvWindowParams(st16);
            var roundTrip = MMSystem.ConvertMciDgvWindowParams(st32);

            Assert.AreEqual((uint)7, st32.nCmdShow);
            Assert.AreEqual((ushort)7, roundTrip.nCmdShow);
        }

        [TestMethod]
        public void ConvertMciDgvUpdateParams_RoundTripsRectCoordinates()
        {
            var st16 = new Win16.MCI_DGV_UPDATE_PARMS()
            {
                rc = new Win16.RECT() { Left = -10, Top = 5, Right = 60, Bottom = 70 },
            };

            var st32 = MMSystem.ConvertMciDgvUpdateParams(st16);
            var roundTrip = MMSystem.ConvertMciDgvUpdateParams(st32);

            Assert.AreEqual(-10, st32.rc.Left);
            Assert.AreEqual(5, st32.rc.Top);
            Assert.AreEqual(60, st32.rc.Right);
            Assert.AreEqual(70, st32.rc.Bottom);
            Assert.AreEqual(st16.rc.Left, roundTrip.rc.Left);
            Assert.AreEqual(st16.rc.Top, roundTrip.rc.Top);
            Assert.AreEqual(st16.rc.Right, roundTrip.rc.Right);
            Assert.AreEqual(st16.rc.Bottom, roundTrip.rc.Bottom);
        }
    }
}
