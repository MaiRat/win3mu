using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sharp86;

namespace Sharp86UnitTests
{
    [TestClass]
    public class OpCodeTests60 : CPUUnitTests
    {
        bool _boundsExceeded = false;

        [TestInitialize]
        public override void Reset()
        {
            _boundsExceeded = false;
            base.Reset();
        }

        public override void RaiseInterrupt(byte interruptNumber)
        {
            if (interruptNumber==5)
            {
                _boundsExceeded = true;
                return;
            }

            base.RaiseInterrupt(interruptNumber);
        }

        [TestMethod]
        public void pusha_popa()
        {
            sp = 0x1000;
            ax = 1;
            bx = 2;
            cx = 3;
            dx = 4;
            bp = 5;
            si = 6;
            di = 7;

            emit("pusha");
            step();

            Assert.AreEqual(sp, 0x0FF0);
            Assert.AreEqual(this.ReadWord(ss, (ushort)(sp + 0)), di);
            Assert.AreEqual(this.ReadWord(ss, (ushort)(sp + 2)), si);
            Assert.AreEqual(this.ReadWord(ss, (ushort)(sp + 4)), bp);
            Assert.AreEqual(this.ReadWord(ss, (ushort)(sp + 6)), 0x1000);
            Assert.AreEqual(this.ReadWord(ss, (ushort)(sp + 8)), bx);
            Assert.AreEqual(this.ReadWord(ss, (ushort)(sp + 10)), dx);
            Assert.AreEqual(this.ReadWord(ss, (ushort)(sp + 12)), cx);
            Assert.AreEqual(this.ReadWord(ss, (ushort)(sp + 14)), ax);

            ax = 0;
            bx = 0;
            cx = 0;
            dx = 0;
            bp = 0;
            si = 0;
            di = 0;

            emit("popa");
            step();

            Assert.AreEqual(sp, 0x1000);
            Assert.AreEqual(ax, 1);
            Assert.AreEqual(bx, 2);
            Assert.AreEqual(cx, 3);
            Assert.AreEqual(dx, 4);
            Assert.AreEqual(bp, 5);
            Assert.AreEqual(si, 6);
            Assert.AreEqual(di, 7);
        }


        [TestMethod]
        public void bound_r16_m16()
        {
            di = 0x1000;
            WriteWord(ds, di, unchecked((ushort)(short)-10));
            WriteWord(ds, (ushort)(di + 2), unchecked((ushort)(short)20));

            _boundsExceeded = false;
            ax = 0;
            emit("bound ax,word [di]");
            step();
            Assert.IsFalse(_boundsExceeded);

            _boundsExceeded = false;
            ax = unchecked((ushort)(short)-11);
            emit("bound ax,word [di]");
            step();
            Assert.IsTrue(_boundsExceeded);

            _boundsExceeded = false;
            ax = unchecked((ushort)(short)-10);
            emit("bound ax,word [di]");
            step();
            Assert.IsFalse(_boundsExceeded);

            _boundsExceeded = false;
            ax = unchecked((ushort)(short)21);
            emit("bound ax,word [di]");
            step();
            Assert.IsTrue(_boundsExceeded);

            _boundsExceeded = false;
            ax = unchecked((ushort)(short)20);
            emit("bound ax,word [di]");
            step();
            Assert.IsFalse(_boundsExceeded);
        }

        [TestMethod]
        public void arpl_register_adjusts_destination_rpl_and_sets_zero()
        {
            ax = 0x1201;
            cx = 0x3403;
            FlagC = true;
            FlagO = true;
            FlagS = false;
            FlagP = true;

            WriteByte(cs, ip, 0x63);
            WriteByte(cs, (ushort)(ip + 1), 0xC8);

            step();

            Assert.AreEqual((ushort)0x1203, ax);
            Assert.AreEqual((ushort)0x3403, cx);
            Assert.IsTrue(FlagZ);
            Assert.IsTrue(FlagC);
            Assert.IsTrue(FlagO);
            Assert.IsFalse(FlagS);
            Assert.IsTrue(FlagP);
        }

        [TestMethod]
        public void arpl_register_preserves_destination_when_rpl_is_not_lower()
        {
            ax = 0x2203;
            cx = 0x3301;
            FlagC = true;
            FlagO = false;
            FlagS = true;
            FlagP = false;
            FlagZ = true;

            WriteByte(cs, ip, 0x63);
            WriteByte(cs, (ushort)(ip + 1), 0xC8);

            step();

            Assert.AreEqual((ushort)0x2203, ax);
            Assert.AreEqual((ushort)0x3301, cx);
            Assert.IsFalse(FlagZ);
            Assert.IsTrue(FlagC);
            Assert.IsFalse(FlagO);
            Assert.IsTrue(FlagS);
            Assert.IsFalse(FlagP);
        }

        [TestMethod]
        public void arpl_memory_adjusts_destination_rpl()
        {
            si = 0x1000;
            cx = 0x0002;
            WriteWord(ds, si, 0x4440);
            FlagZ = false;

            WriteByte(cs, ip, 0x63);
            WriteByte(cs, (ushort)(ip + 1), 0x0C);

            step();

            Assert.AreEqual((ushort)0x4442, ReadWord(ds, si));
            Assert.IsTrue(FlagZ);
        }

        [TestMethod]
        public void arpl_disassembles_register_and_memory_forms()
        {
            WriteByte(cs, ip, 0x63);
            WriteByte(cs, (ushort)(ip + 1), 0xC8);
            WriteByte(cs, (ushort)(ip + 2), 0x63);
            WriteByte(cs, (ushort)(ip + 3), 0x0C);

            var disassembler = new Disassembler(this, cs, ip);

            Assert.AreEqual("arpl ax,cx", disassembler.Read());
            Assert.AreEqual("arpl word ptr [si],cx", disassembler.Read());
        }

        [TestMethod]
        public void fs_segment_override_prefix_reads_from_fs()
        {
            fs = 0x0120;
            ds = 0x0110;
            WriteByte(fs, 0x1000, 0x5A);
            WriteByte(ds, 0x1000, 0xA5);

            WriteByte(cs, ip, 0x64);
            WriteByte(cs, (ushort)(ip + 1), 0xA0);
            WriteWord(cs, (ushort)(ip + 2), 0x1000);

            step();

            Assert.AreEqual((byte)0x5A, al);
        }

        [TestMethod]
        public void gs_segment_override_prefix_reads_from_gs()
        {
            gs = 0x0130;
            ds = 0x0110;
            WriteWord(gs, 0x1000, 0xBEEF);
            WriteWord(ds, 0x1000, 0x1234);

            WriteByte(cs, ip, 0x65);
            WriteByte(cs, (ushort)(ip + 1), 0xA1);
            WriteWord(cs, (ushort)(ip + 2), 0x1000);

            step();

            Assert.AreEqual((ushort)0xBEEF, ax);
        }

        [TestMethod]
        public void fs_gs_segment_override_prefixes_disassemble()
        {
            WriteByte(cs, ip, 0x64);
            WriteByte(cs, (ushort)(ip + 1), 0xA0);
            WriteWord(cs, (ushort)(ip + 2), 0x1000);
            WriteByte(cs, (ushort)(ip + 4), 0x65);
            WriteByte(cs, (ushort)(ip + 5), 0xA1);
            WriteWord(cs, (ushort)(ip + 6), 0x1000);

            var disassembler = new Disassembler(this, cs, ip);

            Assert.AreEqual("mov al,byte ptr fs:[0x1000]", disassembler.Read());
            Assert.AreEqual("mov ax,word ptr gs:[0x1000]", disassembler.Read());
        }
    }
}
