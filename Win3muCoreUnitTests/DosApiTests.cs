using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sharp86;
using Win3muCore;

namespace Win3muCoreUnitTests
{
    [TestClass]
    public class DosApiTests
    {
        static DosApiTests()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Ansi = Encoding.GetEncoding(1252);
        }

        class TestCpu : CPU
        {
            public TestCpu()
            {
                MemoryBus = new TestMemoryBus();
            }
        }

        class TestMemoryBus : IMemoryBus
        {
            readonly byte[] _memory = new byte[1024 * 1024];

            static int ToAddress(ushort seg, ushort offset)
            {
                return ((seg << 4) + offset) & 0xFFFFF;
            }

            public bool IsExecutableSelector(ushort seg) => true;

            public byte ReadByte(ushort seg, ushort offset) => _memory[ToAddress(seg, offset)];

            public void WriteByte(ushort seg, ushort offset, byte value)
            {
                _memory[ToAddress(seg, offset)] = value;
            }
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

        sealed class TempMappedTestSite : DosApi.ISite, IDisposable
        {
            readonly string _rootPath = Path.Combine(Path.GetTempPath(), "win3mu-dosapi-" + Guid.NewGuid().ToString("N"));

            public TempMappedTestSite()
            {
                Directory.CreateDirectory(_rootPath);
            }

            public void Dispose()
            {
                if (Directory.Exists(_rootPath))
                    Directory.Delete(_rootPath, true);
            }

            string MapGuestPath(string path)
            {
                if (string.IsNullOrEmpty(path) || path.Length < 3 || path[1] != ':' || path[2] != '\\')
                    return null;

                var relativePath = path.Substring(3).Replace('\\', Path.DirectorySeparatorChar);
                return string.IsNullOrEmpty(relativePath) ? _rootPath : Path.Combine(_rootPath, relativePath);
            }

            public void ExitProcess(short exitCode) { }
            public bool DoesGuestDirectoryExist(string path)
            {
                var mapped = MapGuestPath(path);
                return mapped != null && Directory.Exists(mapped);
            }
            public string TryMapGuestPathToHost(string path, bool forWrite) => MapGuestPath(path);
            public string TryMapHostPathToGuest(string path, bool forWrite) => path;
            public IEnumerable<string> GetVirtualSubFolders(string guestPath) => Array.Empty<string>();
            public uint Alloc(ushort size) => 0;
            public void Free(uint ptr) { }

            public void CreateFile(string guestPath, byte[] contents)
            {
                var mapped = MapGuestPath(guestPath);
                Directory.CreateDirectory(Path.GetDirectoryName(mapped));
                File.WriteAllBytes(mapped, contents);
            }
        }

        static readonly Encoding Ansi;

        static void WriteFcb(CPU cpu, ushort seg, ushort offset, byte[] fcb)
        {
            cpu.MemoryBus.WriteBytes(seg, offset, fcb);
        }

        static byte[] ReadFcb(CPU cpu, ushort seg, ushort offset, int length = 37)
        {
            return cpu.MemoryBus.ReadBytes(seg, offset, length);
        }

        static byte[] CreateStandardFcb(byte drive, string name, string ext)
        {
            var fcb = new byte[37];
            fcb[0] = drive;
            Array.Copy(Ansi.GetBytes((name + "        ").Substring(0, 8)), 0, fcb, 1, 8);
            Array.Copy(Ansi.GetBytes((ext + "   ").Substring(0, 3)), 0, fcb, 9, 3);
            return fcb;
        }

        static byte[] CreateExtendedFcb(byte drive, byte attributes, string name, string ext)
        {
            var fcb = new byte[37];
            fcb[0] = 0xFF;
            fcb[6] = attributes;
            fcb[7] = drive;
            Array.Copy(Ansi.GetBytes((name + "        ").Substring(0, 8)), 0, fcb, 8, 8);
            Array.Copy(Ansi.GetBytes((ext + "   ").Substring(0, 3)), 0, fcb, 16, 3);
            return fcb;
        }

        static int FromBcd(byte value)
        {
            return ((value >> 4) * 10) + (value & 0x0F);
        }

        static uint ToClockCount(int hour, int minute, int second)
        {
            return (uint)(hour * 65520 + minute * 1092 + second * 18.2);
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
        public void Int1A_SetClockCount_UpdatesSubsequentClockReads()
        {
            var cpu = new TestCpu();
            var dos = new DosApi(cpu, new TestSite());
            var target = ToClockCount(6, 30, 0);
            cpu.ah = 1;
            cpu.cx = (ushort)(target >> 16);
            cpu.dx = (ushort)target;
            cpu.FlagC = true;

            dos.DispatchInt1A();

            Assert.IsFalse(cpu.FlagC);

            cpu.ah = 0;
            dos.DispatchInt1A();

            var actual = ((uint)cpu.cx << 16) | cpu.dx;
            Assert.IsTrue(Math.Abs((long)actual - target) <= 36);
        }

        [TestMethod]
        public void Int1A_SetRealTimeClockTime_UpdatesSubsequentReads()
        {
            var cpu = new TestCpu();
            var dos = new DosApi(cpu, new TestSite());
            cpu.ah = 3;
            cpu.ch = 0x12;
            cpu.cl = 0x34;
            cpu.dh = 0x56;
            cpu.FlagC = true;

            dos.DispatchInt1A();

            Assert.IsFalse(cpu.FlagC);

            cpu.ah = 2;
            dos.DispatchInt1A();

            var decodedSeconds = (FromBcd(cpu.ch) * 3600) + (FromBcd(cpu.cl) * 60) + FromBcd(cpu.dh);
            var expectedSeconds = (12 * 3600) + (34 * 60) + 56;
            Assert.IsTrue(Math.Abs(decodedSeconds - expectedSeconds) <= 1);
        }

        [TestMethod]
        public void Int1A_SetRealTimeClockDate_UpdatesSubsequentReads()
        {
            var cpu = new TestCpu();
            var dos = new DosApi(cpu, new TestSite());
            cpu.ah = 5;
            cpu.ch = 0x20;
            cpu.cl = 0x24;
            cpu.dh = 0x12;
            cpu.dl = 0x31;
            cpu.FlagC = true;

            dos.DispatchInt1A();

            Assert.IsFalse(cpu.FlagC);

            cpu.ah = 4;
            dos.DispatchInt1A();

            Assert.AreEqual((byte)0x20, cpu.ch);
            Assert.AreEqual((byte)0x24, cpu.cl);
            Assert.AreEqual((byte)0x12, cpu.dh);
            Assert.AreEqual((byte)0x31, cpu.dl);
        }

        [TestMethod]
        public void Int2F_WindowsDetectionServices_ReturnNotPresent()
        {
            foreach (ushort ax in new ushort[] { 0x1600, 0x1601, 0x1602, 0x1606, 0x160A, 0x4680 })
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

        [TestMethod]
        public void Int21_FindFirstWithStandardFcb_WritesMatchedNameBackToGuestMemory()
        {
            using var site = new TempMappedTestSite();
            site.CreateFile(@"A:\HELLO.TXT", new byte[] { 1, 2, 3 });

            var cpu = new TestCpu();
            var dos = new DosApi(cpu, site);
            cpu.ds = 0x1000;
            cpu.dx = 0x0200;
            cpu.ah = 0x11;

            WriteFcb(cpu, cpu.ds, cpu.dx, CreateStandardFcb(1, "hello", "txt"));

            dos.DispatchInt21();

            var result = ReadFcb(cpu, cpu.ds, cpu.dx);
            Assert.AreEqual((byte)0x00, cpu.al);
            Assert.IsFalse(cpu.FlagC);
            Assert.AreEqual("HELLO   ", Ansi.GetString(result, 1, 8));
            Assert.AreEqual("TXT", Ansi.GetString(result, 9, 3));
        }

        [TestMethod]
        public void Int21_FindFirstWithExtendedFcb_WritesMatchedNameBackToGuestMemory()
        {
            using var site = new TempMappedTestSite();
            site.CreateFile(@"A:\SETUP.EXE", new byte[] { 1, 2, 3 });

            var cpu = new TestCpu();
            var dos = new DosApi(cpu, site);
            cpu.ds = 0x1000;
            cpu.dx = 0x0200;
            cpu.ah = 0x11;

            WriteFcb(cpu, cpu.ds, cpu.dx, CreateExtendedFcb(1, 0, "setup", "exe"));

            dos.DispatchInt21();

            var result = ReadFcb(cpu, cpu.ds, cpu.dx);
            Assert.AreEqual((byte)0x00, cpu.al);
            Assert.IsFalse(cpu.FlagC);
            Assert.AreEqual((byte)0xFF, result[0]);
            Assert.AreEqual("SETUP   ", Ansi.GetString(result, 8, 8));
            Assert.AreEqual("EXE", Ansi.GetString(result, 16, 3));
        }

        [TestMethod]
        public void Int21_FindNextWithFcb_ReturnsNoMoreFilesWithoutSettingCarry()
        {
            using var site = new TempMappedTestSite();
            site.CreateFile(@"A:\ONE.TXT", new byte[] { 1 });

            var cpu = new TestCpu();
            var dos = new DosApi(cpu, site);
            cpu.ds = 0x1000;
            cpu.dx = 0x0200;

            WriteFcb(cpu, cpu.ds, cpu.dx, CreateStandardFcb(1, "one", "txt"));

            cpu.ah = 0x11;
            dos.DispatchInt21();
            Assert.AreEqual((byte)0x00, cpu.al);

            cpu.ah = 0x12;
            cpu.FlagC = true;
            dos.DispatchInt21();

            Assert.AreEqual((byte)0xFF, cpu.al);
            Assert.IsFalse(cpu.FlagC);
        }

        [TestMethod]
        public void Int1A_UnsupportedService_SetsCarryWithoutThrowing()
        {
            var cpu = new TestCpu();
            var dos = new DosApi(cpu, new TestSite());
            cpu.ah = 0x80;
            cpu.FlagC = false;

            dos.DispatchInt1A();

            Assert.IsTrue(cpu.FlagC);
        }

        [TestMethod]
        public void Int21_UnsupportedFunction_SetsCarryAndErrorCodeWithoutThrowing()
        {
            var cpu = new TestCpu();
            var dos = new DosApi(cpu, new TestSite());
            cpu.ah = 0xFE;
            cpu.FlagC = false;

            dos.DispatchInt21();

            Assert.IsTrue(cpu.FlagC);
            Assert.AreEqual(DosError.FunctionNumberInvalid, cpu.ax);
        }

        [TestMethod]
        public void Int2F_UnsupportedMultiplexService_ReturnsZeroWithoutThrowing()
        {
            var cpu = new TestCpu();
            var dos = new DosApi(cpu, new TestSite());
            cpu.ax = 0xFF00;

            dos.DispatchInt2f();

            Assert.AreEqual((ushort)0, cpu.ax);
        }

        [TestMethod]
        public void Int21_GetCtrlCCheckFlag_ReturnsZero()
        {
            var cpu = new TestCpu();
            var dos = new DosApi(cpu, new TestSite());
            cpu.ah = 0x33;
            cpu.al = 0x00;

            dos.DispatchInt21();

            Assert.IsFalse(cpu.FlagC);
            Assert.AreEqual((byte)0, cpu.dl);
        }

        [TestMethod]
        public void Int21_SetCtrlCCheckFlag_SucceedsWithoutError()
        {
            var cpu = new TestCpu();
            var dos = new DosApi(cpu, new TestSite());
            cpu.ah = 0x33;
            cpu.al = 0x01;
            cpu.dl = 1;

            dos.DispatchInt21();

            Assert.IsFalse(cpu.FlagC);
        }

        [TestMethod]
        public void Int21_GetBootDrive_ReturnsDriveC()
        {
            var cpu = new TestCpu();
            var dos = new DosApi(cpu, new TestSite());
            cpu.ah = 0x33;
            cpu.al = 0x05;

            dos.DispatchInt21();

            Assert.IsFalse(cpu.FlagC);
            Assert.AreEqual((byte)3, cpu.dl); // C: drive
        }

        [TestMethod]
        public void Int21_GetDiskFreeSpace_ReturnsValidValues()
        {
            var cpu = new TestCpu();
            var dos = new DosApi(cpu, new TestSite());
            cpu.ah = 0x36;
            cpu.dl = 0; // Default drive

            dos.DispatchInt21();

            Assert.IsFalse(cpu.FlagC);
            Assert.AreNotEqual((ushort)0xFFFF, cpu.ax); // Not invalid drive
            Assert.IsTrue(cpu.ax > 0);     // sectors per cluster
            Assert.IsTrue(cpu.cx > 0);     // bytes per sector
            Assert.IsTrue(cpu.dx > 0);     // total clusters
            Assert.IsTrue(cpu.bx > 0);     // available clusters
        }

        [TestMethod]
        public void Int21_AllocateMemory_ReturnsInsufficientMemoryError()
        {
            var cpu = new TestCpu();
            var dos = new DosApi(cpu, new TestSite());
            cpu.ah = 0x48;
            cpu.bx = 0x100; // Request 256 paragraphs

            dos.DispatchInt21();

            Assert.IsTrue(cpu.FlagC);
            Assert.AreEqual((ushort)0x08, cpu.ax); // Insufficient memory
        }

        [TestMethod]
        public void Int21_FreeMemory_SucceedsWithoutError()
        {
            var cpu = new TestCpu();
            var dos = new DosApi(cpu, new TestSite());
            cpu.ah = 0x49;
            cpu.es = 0x1000;

            dos.DispatchInt21();

            Assert.IsFalse(cpu.FlagC);
        }

        [TestMethod]
        public void Int21_ResizeMemoryBlock_SucceedsWithoutError()
        {
            var cpu = new TestCpu();
            var dos = new DosApi(cpu, new TestSite());
            cpu.ah = 0x4A;
            cpu.es = 0x1000;
            cpu.bx = 0x200;

            dos.DispatchInt21();

            Assert.IsFalse(cpu.FlagC);
        }

        [TestMethod]
        public void Int21_GetFileDateTime_UnopenedFileHandle_SetsCarry()
        {
            var cpu = new TestCpu();
            var dos = new DosApi(cpu, new TestSite());

            // Try to get date/time on an invalid handle
            cpu.ah = 0x57;
            cpu.al = 0x00;
            cpu.bx = 0xFFFF; // Invalid handle

            dos.DispatchInt21();

            // Should set carry for invalid handle
            Assert.IsTrue(cpu.FlagC);
        }

    }
}
