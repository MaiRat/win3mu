using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sharp86;
using Win3muCore;

namespace Win3muCoreUnitTests
{
    [TestClass]
    public class DosApiTests
    {
        class TestCpu : CPU
        {
        }

        class TestSite : DosApi.ISite
        {
            public void ExitProcess(short exitCode) { }
            public bool DoesGuestDirectoryExist(string path) => true;
            public string TryMapGuestPathToHost(string path, bool forWrite) => path;
            public string TryMapHostPathToGuest(string path, bool forWrite) => path;
            public IEnumerable<string> GetVirtualSubFolders(string guestPath) => Array.Empty<string>();
            public uint Alloc(ushort size) => 0;
            public void Free(uint ptr) { }
        }

        static int FromBcd(byte value)
        {
            return ((value >> 4) * 10) + (value & 0x0F);
        }

        [TestMethod]
        public void Int1A_GetRealTimeClockTime_ReturnsCurrentLocalTimeInBcd()
        {
            var cpu = new TestCpu();
            var dos = new DosApi(cpu, new TestSite());
            cpu.ah = 2;
            cpu.FlagC = true;

            var before = DateTime.Now;
            dos.DispatchInt1A();
            var after = DateTime.Now;

            var decodedSeconds = (FromBcd(cpu.ch) * 3600) + (FromBcd(cpu.cl) * 60) + FromBcd(cpu.dh);
            var beforeSeconds = (before.Hour * 3600) + (before.Minute * 60) + before.Second;
            var afterSeconds = (after.Hour * 3600) + (after.Minute * 60) + after.Second;

            Assert.IsFalse(cpu.FlagC);
            Assert.IsTrue(Math.Abs(decodedSeconds - beforeSeconds) <= 1 || Math.Abs(decodedSeconds - afterSeconds) <= 1);
            Assert.IsTrue(cpu.dl == 0 || cpu.dl == 1);
        }

        [TestMethod]
        public void Int1A_GetRealTimeClockDate_ReturnsCurrentLocalDateInBcd()
        {
            var cpu = new TestCpu();
            var dos = new DosApi(cpu, new TestSite());
            cpu.ah = 4;
            cpu.FlagC = true;

            var before = DateTime.Now.Date;
            dos.DispatchInt1A();
            var after = DateTime.Now.Date;

            var decoded = new DateTime(
                (FromBcd(cpu.ch) * 100) + FromBcd(cpu.cl),
                FromBcd(cpu.dh),
                FromBcd(cpu.dl));

            Assert.IsFalse(cpu.FlagC);
            Assert.IsTrue(decoded == before || decoded == after);
        }

        [TestMethod]
        public void Int2F_WindowsDetectionServices_ReturnNotPresent()
        {
            foreach (ushort ax in new ushort[] { 0x1600, 0x160A, 0x4680 })
            {
                var cpu = new TestCpu();
                var dos = new DosApi(cpu, new TestSite());
                cpu.ax = ax;

                dos.DispatchInt2f();

                Assert.AreEqual((ushort)0, cpu.ax);
            }
        }

        [TestMethod]
        public void Int2F_MscdexInstallationCheck_StillReportsNotInstalled()
        {
            var cpu = new TestCpu();
            var dos = new DosApi(cpu, new TestSite());
            cpu.ah = 0x15;
            cpu.al = 0x00;

            dos.DispatchInt2f();

            Assert.AreEqual((byte)0, cpu.al);
        }
    }
}
