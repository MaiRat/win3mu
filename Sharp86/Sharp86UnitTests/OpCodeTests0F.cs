using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sharp86UnitTests
{
    [TestClass]
    public class OpCodeTests0F : CPUUnitTests
    {
        void assertSetcc(byte opCode2, byte modRM, bool expected, bool flagO, bool flagC, bool flagZ, bool flagS, bool flagP)
        {
            Reset();

            FlagO = flagO;
            FlagC = flagC;
            FlagZ = flagZ;
            FlagS = flagS;
            FlagP = flagP;

            ushort flags = EFlags;

            if (modRM == 0x04)
            {
                si = 0x8000;
                WriteByte(ds, si, 0xCC);
            }
            else
            {
                al = 0xCC;
            }

            WriteByte(cs, ip, 0x0F);
            WriteByte(cs, (ushort)(ip + 1), opCode2);
            WriteByte(cs, (ushort)(ip + 2), modRM);

            step();

            if (modRM == 0x04)
                Assert.AreEqual((byte)(expected ? 1 : 0), ReadByte(ds, si));
            else
                Assert.AreEqual((byte)(expected ? 1 : 0), al);

            Assert.AreEqual(flags, EFlags);
        }

        void assertJccNear(byte opCode2, bool expected, ushort offset, bool flagO, bool flagC, bool flagZ, bool flagS, bool flagP)
        {
            Reset();

            FlagO = flagO;
            FlagC = flagC;
            FlagZ = flagZ;
            FlagS = flagS;
            FlagP = flagP;

            ushort flags = EFlags;
            ushort startIp = ip;

            WriteByte(cs, startIp, 0x0F);
            WriteByte(cs, (ushort)(startIp + 1), opCode2);
            WriteWord(cs, (ushort)(startIp + 2), offset);

            step();

            ushort fallthroughIp = (ushort)(startIp + 4);
            ushort jumpIp = (ushort)(fallthroughIp + (ushort)(short)offset);
            Assert.AreEqual(expected ? jumpIp : fallthroughIp, ip);
            Assert.AreEqual(flags, EFlags);
        }

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

        [TestMethod]
        public void setcc_register_conditions()
        {
            assertSetcc(0x90, 0xC0, true,  true,  false, false, false, false);
            assertSetcc(0x90, 0xC0, false, false, false, false, false, false);
            assertSetcc(0x91, 0xC0, true,  false, false, false, false, false);
            assertSetcc(0x91, 0xC0, false, true,  false, false, false, false);
            assertSetcc(0x92, 0xC0, true,  false, true,  false, false, false);
            assertSetcc(0x92, 0xC0, false, false, false, false, false, false);
            assertSetcc(0x93, 0xC0, true,  false, false, false, false, false);
            assertSetcc(0x93, 0xC0, false, false, true,  false, false, false);
            assertSetcc(0x94, 0xC0, true,  false, false, true,  false, false);
            assertSetcc(0x94, 0xC0, false, false, false, false, false, false);
            assertSetcc(0x95, 0xC0, true,  false, false, false, false, false);
            assertSetcc(0x95, 0xC0, false, false, false, true,  false, false);
            assertSetcc(0x96, 0xC0, true,  false, true,  false, false, false);
            assertSetcc(0x96, 0xC0, true,  false, false, true,  false, false);
            assertSetcc(0x96, 0xC0, false, false, false, false, false, false);
            assertSetcc(0x97, 0xC0, true,  false, false, false, false, false);
            assertSetcc(0x97, 0xC0, false, false, true,  false, false, false);
            assertSetcc(0x97, 0xC0, false, false, false, true,  false, false);
            assertSetcc(0x98, 0xC0, true,  false, false, false, true,  false);
            assertSetcc(0x98, 0xC0, false, false, false, false, false, false);
            assertSetcc(0x99, 0xC0, true,  false, false, false, false, false);
            assertSetcc(0x99, 0xC0, false, false, false, false, true,  false);
            assertSetcc(0x9A, 0xC0, true,  false, false, false, false, true);
            assertSetcc(0x9A, 0xC0, false, false, false, false, false, false);
            assertSetcc(0x9B, 0xC0, true,  false, false, false, false, false);
            assertSetcc(0x9B, 0xC0, false, false, false, false, false, true);
            assertSetcc(0x9C, 0xC0, true,  true,  false, false, false, false);
            assertSetcc(0x9C, 0xC0, false, false, false, false, false, false);
            assertSetcc(0x9D, 0xC0, true,  true,  false, false, true,  false);
            assertSetcc(0x9D, 0xC0, false, true,  false, false, false, false);
            assertSetcc(0x9E, 0xC0, true,  false, false, true,  false, false);
            assertSetcc(0x9E, 0xC0, true,  true,  false, false, false, false);
            assertSetcc(0x9E, 0xC0, false, true,  false, false, true,  false);
            assertSetcc(0x9F, 0xC0, true,  true,  false, false, true,  false);
            assertSetcc(0x9F, 0xC0, false, false, false, true,  false, false);
            assertSetcc(0x9F, 0xC0, false, false, false, false, false, false);
        }

        [TestMethod]
        public void setcc_memory_destination()
        {
            assertSetcc(0x94, 0x04, true, false, false, true, false, false);
        }

        [TestMethod]
        public void jcc_near_conditions()
        {
            const ushort offset = 0x0040;

            assertJccNear(0x80, true,  offset, true,  false, false, false, false);
            assertJccNear(0x80, false, offset, false, false, false, false, false);
            assertJccNear(0x81, true,  offset, false, false, false, false, false);
            assertJccNear(0x81, false, offset, true,  false, false, false, false);
            assertJccNear(0x82, true,  offset, false, true,  false, false, false);
            assertJccNear(0x82, false, offset, false, false, false, false, false);
            assertJccNear(0x83, true,  offset, false, false, false, false, false);
            assertJccNear(0x83, false, offset, false, true,  false, false, false);
            assertJccNear(0x84, true,  offset, false, false, true,  false, false);
            assertJccNear(0x84, false, offset, false, false, false, false, false);
            assertJccNear(0x85, true,  offset, false, false, false, false, false);
            assertJccNear(0x85, false, offset, false, false, true,  false, false);
            assertJccNear(0x86, true,  offset, false, true,  false, false, false);
            assertJccNear(0x86, true,  offset, false, false, true,  false, false);
            assertJccNear(0x86, false, offset, false, false, false, false, false);
            assertJccNear(0x87, true,  offset, false, false, false, false, false);
            assertJccNear(0x87, false, offset, false, true,  false, false, false);
            assertJccNear(0x87, false, offset, false, false, true,  false, false);
            assertJccNear(0x88, true,  offset, false, false, false, true,  false);
            assertJccNear(0x88, false, offset, false, false, false, false, false);
            assertJccNear(0x89, true,  offset, false, false, false, false, false);
            assertJccNear(0x89, false, offset, false, false, false, true,  false);
            assertJccNear(0x8A, true,  offset, false, false, false, false, true);
            assertJccNear(0x8A, false, offset, false, false, false, false, false);
            assertJccNear(0x8B, true,  offset, false, false, false, false, false);
            assertJccNear(0x8B, false, offset, false, false, false, false, true);
            assertJccNear(0x8C, true,  offset, true,  false, false, false, false);
            assertJccNear(0x8C, false, offset, false, false, false, false, false);
            assertJccNear(0x8D, true,  offset, true,  false, false, true,  false);
            assertJccNear(0x8D, false, offset, true,  false, false, false, false);
            assertJccNear(0x8E, true,  offset, false, false, true,  false, false);
            assertJccNear(0x8E, true,  offset, true,  false, false, false, false);
            assertJccNear(0x8E, false, offset, true,  false, false, true,  false);
            assertJccNear(0x8F, true,  offset, true,  false, false, true,  false);
            assertJccNear(0x8F, false, offset, false, false, true,  false, false);
            assertJccNear(0x8F, false, offset, false, false, false, false, false);
        }

        [TestMethod]
        public void jcc_near_negative_offset()
        {
            assertJccNear(0x85, true, unchecked((ushort)0xFFFC), false, false, false, false, false);
        }
    }
}
