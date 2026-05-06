using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sharp86UnitTests
{
    [TestClass]
    public class OpCodeTests0F : CPUUnitTests
    {
        [TestMethod]
        public void movzx_Gv_Eb_register()
        {
            ax = 0xFFFF;
            bl = 0xFE;
            FlagC = true;
            FlagZ = false;
            FlagO = true;

            WriteByte(cs, ip, 0x0F);
            WriteByte(cs, (ushort)(ip + 1), 0xB6);
            WriteByte(cs, (ushort)(ip + 2), 0xC3);

            step();

            Assert.AreEqual((ushort)0x00FE, ax);
            Assert.AreEqual((ushort)0x00FE, bx);
            Assert.IsTrue(FlagC);
            Assert.IsFalse(FlagZ);
            Assert.IsTrue(FlagO);
        }

        [TestMethod]
        public void movsx_Gv_Eb_memory()
        {
            si = 0x8000;
            WriteByte(ds, si, 0x88);
            ax = 0;
            FlagC = false;
            FlagZ = true;
            FlagO = false;

            WriteByte(cs, ip, 0x0F);
            WriteByte(cs, (ushort)(ip + 1), 0xBE);
            WriteByte(cs, (ushort)(ip + 2), 0x04);

            step();

            Assert.AreEqual(unchecked((ushort)0xFF88), ax);
            Assert.AreEqual((byte)0x88, ReadByte(ds, si));
            Assert.IsFalse(FlagC);
            Assert.IsTrue(FlagZ);
            Assert.IsFalse(FlagO);
        }
    }
}
