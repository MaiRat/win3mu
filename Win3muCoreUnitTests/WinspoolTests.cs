using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Win3muCore;

namespace Win3muCoreUnitTests
{
    [TestClass]
    public class WinspoolTests
    {
        [TestMethod]
        public void WinspoolModule_ExportsExpectedOrdinalsAndNames()
        {
            var winspool = new Winspool();
            var exports = winspool.GetExports().OrderBy(x => x).ToArray();

            CollectionAssert.IsSubsetOf(new ushort[] { 41, 66, 70, 71, 72, 73, 74, 75, 0x0100, 0x0101, 0x0102, 0x0103 }, exports);
            Assert.AreEqual(41, winspool.GetOrdinalFromName("OpenPrinter"));
            Assert.AreEqual(66, winspool.GetOrdinalFromName("ClosePrinter"));
            Assert.AreEqual(71, winspool.GetOrdinalFromName("StartDocPrinter"));
            Assert.AreEqual(74, winspool.GetOrdinalFromName("WritePrinter"));
            Assert.AreEqual("AbortPrinter", winspool.GetNameFromOrdinal(75));
            Assert.AreEqual("StartDoc", winspool.GetNameFromOrdinal(0x0100));
        }

        [TestMethod]
        public void WinspoolModule_OpenStartWriteAndClose_UsesPreloadedModule()
        {
            var machine = new Machine();
            var winspool = machine.ModuleManager.LoadModule("winspool.drv") as Winspool;

            Assert.IsNotNull(winspool);
            Assert.AreSame(winspool, machine.ModuleManager.GetModule("WINSPOOL"));

            uint printerHandlePtr = Alloc(machine, "Printer Handle", 4);
            uint writtenCountPtr = Alloc(machine, "Written Count", 4);

            Assert.IsTrue(winspool.OpenPrinter("HP LaserJet", printerHandlePtr, 0));
            ushort hPrinter = ReadWord(machine, printerHandlePtr);
            Assert.AreNotEqual((ushort)0, hPrinter);

            Assert.AreEqual((ushort)1, winspool.StartDoc(hPrinter, 0));
            Assert.IsTrue(winspool.StartPage(hPrinter));
            Assert.IsTrue(winspool.WritePrinter(hPrinter, 0, 0, writtenCountPtr));
            Assert.AreEqual((ushort)0, ReadWord(machine, writtenCountPtr));
            Assert.IsTrue(winspool.EndPage(hPrinter));
            Assert.IsTrue(winspool.EndDoc(hPrinter));
            Assert.IsTrue(winspool.ClosePrinter(hPrinter));
            Assert.IsFalse(winspool.ClosePrinter(hPrinter));
        }

        static uint Alloc(Machine machine, string name, uint size)
        {
            return BitUtils.MakeDWord(0, machine.GlobalHeap.Alloc(name, 0, size));
        }

        static ushort ReadWord(Machine machine, uint ptr)
        {
            return machine.GlobalHeap.GetBuffer(ptr.Hiword(), false).ReadWord(ptr.Loword());
        }
    }
}
