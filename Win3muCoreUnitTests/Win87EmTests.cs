using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Win3muCore;

namespace Win3muCoreUnitTests
{
    [TestClass]
    public class Win87EmTests
    {
        [TestMethod]
        public void Win87EmModule_ExportsExpectedOrdinalsAndNames()
        {
            var win87em = new Win87Em();
            var exports = win87em.GetExports().OrderBy(x => x).ToArray();

            CollectionAssert.AreEqual(new ushort[] { 1, 3, 4, 5 }, exports);
            Assert.AreEqual(0, win87em.GetOrdinalFromName("Missing"));
            Assert.AreEqual(1, win87em.GetOrdinalFromName("__fpMath"));
            Assert.AreEqual(3, win87em.GetOrdinalFromName("__WinEm87Info"));
            Assert.AreEqual("__WinEm87Restore", win87em.GetNameFromOrdinal(4));
            Assert.IsNull(win87em.GetNameFromOrdinal(2));
        }

        [TestMethod]
        public void Win87EmModule_FpMathTracksControlWordAndInstalledState()
        {
            var machine = new Machine();
            var win87em = machine.ModuleManager.GetModule("WIN87EM") as Win87Em;

            Assert.IsNotNull(win87em);

            machine.ax = 0xFFFF;
            machine.bx = 0;
            win87em.FpMath();
            Assert.AreEqual((ushort)0, machine.ax);

            machine.bx = 5;
            win87em.FpMath();
            Assert.AreEqual((ushort)0x1332, machine.ax);

            machine.ax = 0x1373;
            machine.bx = 4;
            win87em.FpMath();
            Assert.AreEqual((ushort)(0x1373 & ~0x00C3), machine.ax);

            machine.bx = 5;
            win87em.FpMath();
            Assert.AreEqual((ushort)0x1373, machine.ax);

            machine.ax = 0;
            machine.dx = 0xFFFF;
            machine.bx = 11;
            win87em.FpMath();
            Assert.AreEqual((ushort)1, machine.ax);
            Assert.AreEqual((ushort)0, machine.dx);
        }

        [TestMethod]
        public void Win87EmModule_InfoSaveRestoreAndInterruptsProvideCompatibilityBehavior()
        {
            var machine = new Machine();
            var win87em = machine.ModuleManager.GetModule("WIN87EM") as Win87Em;

            Assert.IsNotNull(win87em);

            uint infoPtr = Alloc(machine, "Win87Em Info", 12);
            uint savePtr = Alloc(machine, "Win87Em Save", 16);

            win87em.WinEm87Info(infoPtr, 12);
            Assert.AreEqual((ushort)0x0600, ReadWord(machine, infoPtr, 0));
            Assert.AreEqual((ushort)0x01D5, ReadWord(machine, infoPtr, 2));
            Assert.AreEqual((ushort)1, ReadWord(machine, infoPtr, 8));

            machine.ax = 0x1455;
            machine.bx = 4;
            win87em.FpMath();
            machine.ax = 0xCAFE;
            machine.bx = 12;
            win87em.FpMath();
            win87em.WinEm87Save(savePtr, 16);

            machine.ax = 0x1777;
            machine.bx = 4;
            win87em.FpMath();
            win87em.WinEm87Restore(savePtr, 16);

            machine.bx = 5;
            win87em.FpMath();
            Assert.AreEqual((ushort)0x1455, machine.ax);

            for (byte interruptNumber = 0x34; interruptNumber <= 0x3D; interruptNumber++)
            {
                machine.RaiseInterrupt(interruptNumber);
            }
        }

        static uint Alloc(Machine machine, string name, uint size)
        {
            return BitUtils.MakeDWord(0, machine.GlobalHeap.Alloc(name, 0, size));
        }

        static ushort ReadWord(Machine machine, uint ptr, ushort offset)
        {
            return machine.GlobalHeap.GetBuffer(ptr.Hiword(), false).ReadWord(offset);
        }
    }
}
