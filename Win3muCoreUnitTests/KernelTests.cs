using Microsoft.VisualStudio.TestTools.UnitTesting;
using Win3muCore;

namespace Win3muCoreUnitTests
{
    [TestClass]
    public class KernelTests
    {
        [TestMethod]
        public void KernelModule_ExportsRecentStubOrdinals()
        {
            var kernel = new Kernel();

            Assert.AreEqual((ushort)0x0098, kernel.GetOrdinalFromName("GetNumTasks"));
            Assert.AreEqual((ushort)0x00AB, kernel.GetOrdinalFromName("AllocDSToCSAlias"));
            Assert.AreEqual((ushort)0x0140, kernel.GetOrdinalFromName("IsTask"));
            Assert.AreEqual((ushort)0x014E, kernel.GetOrdinalFromName("IsBadReadPtr"));
            Assert.AreEqual((ushort)0x014F, kernel.GetOrdinalFromName("IsBadWritePtr"));
            Assert.AreEqual((ushort)0x0150, kernel.GetOrdinalFromName("IsBadCodePtr"));
            Assert.AreEqual((ushort)0x0151, kernel.GetOrdinalFromName("IsBadStringPtr"));
            Assert.AreEqual((ushort)0x015C, kernel.GetOrdinalFromName("hmemcpy"));
            Assert.AreEqual((ushort)0x0161, kernel.GetOrdinalFromName("lstrcpyn"));
        }

        [TestMethod]
        public void Kernel_TaskCompatibilityStubs_ReturnExpectedDefaults()
        {
            var machine = new Machine();
            var kernel = machine.ModuleManager.GetModule("KERNEL") as Kernel;

            Assert.IsNotNull(kernel);
            Assert.AreEqual((ushort)1, kernel.GetNumTasks());
            Assert.IsTrue(kernel.IsTask(kernel.GetCurrentTask()));
            Assert.IsFalse(kernel.IsTask(0x2222));
        }

        [TestMethod]
        public void Kernel_AllocDSToCSAlias_CreatesExecutableAliasForDataSelector()
        {
            var machine = new Machine();
            var kernel = machine.ModuleManager.GetModule("KERNEL") as Kernel;
            ushort dataSelector = machine.GlobalHeap.Alloc("KernelTests Data", 0, 16);

            ushort alias = kernel.AllocDSToCSAlias(dataSelector);
            var source = machine.GlobalHeap.GetSelector(dataSelector);
            var created = machine.GlobalHeap.GetSelector(alias);

            Assert.AreNotEqual((ushort)0, alias);
            Assert.IsNotNull(created);
            Assert.AreSame(source.allocation, created.allocation);
            Assert.IsTrue(created.isCode);
            Assert.IsTrue(created.readOnly);
        }

        [TestMethod]
        public void Kernel_PointerAndCopyHelpers_UseGuestSelectorAccess()
        {
            var machine = new Machine();
            var kernel = machine.ModuleManager.GetModule("KERNEL") as Kernel;
            ushort dataSelector = machine.GlobalHeap.Alloc("KernelTests Data", 0, 32);
            ushort destSelector = machine.GlobalHeap.Alloc("KernelTests Dest", 0, 32);
            ushort codeSelector = kernel.AllocDSToCSAlias(dataSelector);

            var dataBuffer = machine.GlobalHeap.GetBuffer(dataSelector, true);
            var destBuffer = machine.GlobalHeap.GetBuffer(destSelector, true);
            dataBuffer[0] = (byte)'H';
            dataBuffer[1] = (byte)'i';
            dataBuffer[2] = 0;
            dataBuffer[3] = 0x33;
            dataBuffer[4] = 0x44;

            uint dataPtr = BitUtils.MakeDWord(0, dataSelector);
            uint destPtr = BitUtils.MakeDWord(0, destSelector);
            uint codePtr = BitUtils.MakeDWord(0, codeSelector);

            Assert.IsFalse(kernel.IsBadReadPtr(dataPtr, 5));
            Assert.IsFalse(kernel.IsBadWritePtr(destPtr, 5));
            Assert.IsTrue(kernel.IsBadWritePtr(codePtr, 1));
            Assert.IsFalse(kernel.IsBadCodePtr(codePtr));
            Assert.IsTrue(kernel.IsBadCodePtr(dataPtr));
            Assert.IsFalse(kernel.IsBadStringPtr(dataPtr, 3));
            Assert.IsTrue(kernel.IsBadStringPtr(dataPtr, 2));
            Assert.IsTrue(kernel.IsBadReadPtr(BitUtils.MakeDWord(0, (ushort)(dataSelector + 0x100)), 1));

            kernel.hmemcpy(destPtr, dataPtr, 5);
            CollectionAssert.AreEqual(new byte[] { (byte)'H', (byte)'i', 0, 0x33, 0x44 }, Slice(destBuffer, 5));

            kernel.lstrcpyn(destPtr, "Hello", 4);
            CollectionAssert.AreEqual(new byte[] { (byte)'H', (byte)'e', (byte)'l', 0 }, Slice(destBuffer, 4));
        }

        static byte[] Slice(byte[] buffer, int count)
        {
            var result = new byte[count];
            System.Array.Copy(buffer, result, count);
            return result;
        }
    }
}
