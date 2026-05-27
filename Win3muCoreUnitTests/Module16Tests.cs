using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Win3muCore;

namespace Win3muCoreUnitTests
{
    [TestClass]
    public class Module16Tests
    {
        [TestMethod]
        public void MapFPOpCodeToWin87EmInt_KnownOpcodes_ReturnExpected()
        {
            byte triByte = 0;
            Assert.AreEqual((ushort)0x34CD, Module16.MapFPOpCodeToWin87EmInt(0xD89B, ref triByte));
            Assert.AreEqual((byte)0, triByte);

            triByte = 0;
            Assert.AreEqual((ushort)0x35CD, Module16.MapFPOpCodeToWin87EmInt(0xD99B, ref triByte));

            triByte = 0;
            Assert.AreEqual((ushort)0x3DCD, Module16.MapFPOpCodeToWin87EmInt(0x9B90, ref triByte));
        }

        [TestMethod]
        public void MapFPOpCodeToWin87EmInt_SegmentOverride_SetsTriByteTable()
        {
            byte triByte = 0;
            ushort result = Module16.MapFPOpCodeToWin87EmInt(0x2e9B, ref triByte);
            Assert.AreEqual((ushort)0x3CCD, result);
            Assert.AreEqual((byte)0x2e, triByte);

            triByte = 0;
            result = Module16.MapFPOpCodeToWin87EmInt(0x369B, ref triByte);
            Assert.AreEqual((ushort)0x3CCD, result);
            Assert.AreEqual((byte)0x36, triByte);
        }

        [TestMethod]
        public void MapFPOpCodeToWin87EmInt_UnknownOpcode_ReturnsZero()
        {
            byte triByte = 0;
            Assert.AreEqual((ushort)0, Module16.MapFPOpCodeToWin87EmInt(0x1234, ref triByte));
            Assert.AreEqual((byte)0, triByte);
        }

        [TestMethod]
        public void MapFpOpCodeToWin87TriByte_KnownCombos_ReturnExpected()
        {
            Assert.AreEqual((byte)0x98, Module16.MapFpOpCodeToWin87TriByte(0x2e, 0xD8));
            Assert.AreEqual((byte)0x9B, Module16.MapFpOpCodeToWin87TriByte(0x2e, 0xDB));
            Assert.AreEqual((byte)0x58, Module16.MapFpOpCodeToWin87TriByte(0x36, 0xD8));
            Assert.AreEqual((byte)0x5E, Module16.MapFpOpCodeToWin87TriByte(0x36, 0xDE));
        }

        [TestMethod]
        public void MapFpOpCodeToWin87TriByte_UnknownCombo_ReturnsNop()
        {
            // Unknown combo should return 0x90 (NOP) instead of throwing
            byte result = Module16.MapFpOpCodeToWin87TriByte(0xFF, 0xFF);
            Assert.AreEqual((byte)0x90, result);
        }
    }
}
