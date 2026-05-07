using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sharp86;

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
        public void smsw_Ev_register_returns_machine_status_word()
        {
            ax = 0xFFFF;
            MachineStatusWord = 0x0001;

            WriteByte(cs, ip, 0x0F);
            WriteByte(cs, (ushort)(ip + 1), 0x01);
            WriteByte(cs, (ushort)(ip + 2), 0xE0);

            step();

            Assert.AreEqual((ushort)0x0001, ax);
        }

        [TestMethod]
        public void sldt_Ev_register_returns_ldt_selector()
        {
            ax = 0xFFFF;
            LocalDescriptorTableSelector = 0x1234;
            FlagC = true;

            WriteByte(cs, ip, 0x0F);
            WriteByte(cs, (ushort)(ip + 1), 0x00);
            WriteByte(cs, (ushort)(ip + 2), 0xC0);

            step();

            Assert.AreEqual((ushort)0x1234, ax);
            Assert.IsTrue(FlagC);
        }

        [TestMethod]
        public void str_Ev_memory_returns_task_register_selector()
        {
            TaskRegisterSelector = 0x5678;
            si = 0x8000;

            WriteByte(cs, ip, 0x0F);
            WriteByte(cs, (ushort)(ip + 1), 0x00);
            WriteByte(cs, (ushort)(ip + 2), 0x0C);

            step();

            Assert.AreEqual((ushort)0x5678, ReadWord(ds, si));
        }

        [TestMethod]
        public void lldt_Ev_memory_updates_ldt_selector()
        {
            si = 0x8000;
            WriteWord(ds, si, 0x9ABC);

            WriteByte(cs, ip, 0x0F);
            WriteByte(cs, (ushort)(ip + 1), 0x00);
            WriteByte(cs, (ushort)(ip + 2), 0x14);

            step();

            Assert.AreEqual((ushort)0x9ABC, LocalDescriptorTableSelector);
        }

        [TestMethod]
        public void ltr_Ev_register_updates_task_register_selector()
        {
            ax = 0xDEF0;

            WriteByte(cs, ip, 0x0F);
            WriteByte(cs, (ushort)(ip + 1), 0x00);
            WriteByte(cs, (ushort)(ip + 2), 0xD8);

            step();

            Assert.AreEqual((ushort)0xDEF0, TaskRegisterSelector);
        }

        [TestMethod]
        public void verr_Ev_register_sets_zf_for_readable_selector()
        {
            ax = 0x2468;
            FlagZ = false;
            FlagC = true;
            MarkSelectorReadable(ax);

            WriteByte(cs, ip, 0x0F);
            WriteByte(cs, (ushort)(ip + 1), 0x00);
            WriteByte(cs, (ushort)(ip + 2), 0xE0);

            step();

            Assert.IsTrue(FlagZ);
            Assert.IsTrue(FlagC);
            Assert.AreEqual((ushort)0x2468, ax);
        }

        [TestMethod]
        public void verw_Ev_memory_clears_zf_for_nonwritable_selector()
        {
            si = 0x8000;
            WriteWord(ds, si, 0x1357);
            FlagZ = true;
            MarkSelectorReadable(0x1357);

            WriteByte(cs, ip, 0x0F);
            WriteByte(cs, (ushort)(ip + 1), 0x00);
            WriteByte(cs, (ushort)(ip + 2), 0x2C);

            step();

            Assert.IsFalse(FlagZ);
            Assert.AreEqual((ushort)0x1357, ReadWord(ds, si));
        }

        [TestMethod]
        public void verw_Ev_register_sets_zf_for_writable_selector()
        {
            ax = 0x3579;
            FlagZ = false;
            MarkSelectorWritable(ax);

            WriteByte(cs, ip, 0x0F);
            WriteByte(cs, (ushort)(ip + 1), 0x00);
            WriteByte(cs, (ushort)(ip + 2), 0xE8);

            step();

            Assert.IsTrue(FlagZ);
        }

        [TestMethod]
        public void sldt_Ev_disassembles()
        {
            WriteByte(cs, ip, 0x0F);
            WriteByte(cs, (ushort)(ip + 1), 0x00);
            WriteByte(cs, (ushort)(ip + 2), 0xC0);

            var disassembler = new Disassembler(this, cs, ip);

            Assert.AreEqual("sldt ax", disassembler.Read());
        }

        [TestMethod]
        public void ltr_Ev_disassembles()
        {
            WriteByte(cs, ip, 0x0F);
            WriteByte(cs, (ushort)(ip + 1), 0x00);
            WriteByte(cs, (ushort)(ip + 2), 0xD8);

            var disassembler = new Disassembler(this, cs, ip);

            Assert.AreEqual("ltr ax", disassembler.Read());
        }

        [TestMethod]
        public void verr_Ev_disassembles()
        {
            WriteByte(cs, ip, 0x0F);
            WriteByte(cs, (ushort)(ip + 1), 0x00);
            WriteByte(cs, (ushort)(ip + 2), 0xE0);

            var disassembler = new Disassembler(this, cs, ip);

            Assert.AreEqual("verr ax", disassembler.Read());
        }

        [TestMethod]
        public void verw_Ev_disassembles()
        {
            WriteByte(cs, ip, 0x0F);
            WriteByte(cs, (ushort)(ip + 1), 0x00);
            WriteByte(cs, (ushort)(ip + 2), 0x2C);

            var disassembler = new Disassembler(this, cs, ip);

            Assert.AreEqual("verw word ptr [si]", disassembler.Read());
        }

        [TestMethod]
        public void lmsw_Ev_memory_updates_low_machine_status_word_bits()
        {
            MachineStatusWord = 0xFFF0;
            si = 0x8000;
            WriteWord(ds, si, 0x0007);

            WriteByte(cs, ip, 0x0F);
            WriteByte(cs, (ushort)(ip + 1), 0x01);
            WriteByte(cs, (ushort)(ip + 2), 0x34);

            step();

            Assert.AreEqual((ushort)0xFFF7, MachineStatusWord);
            Assert.AreEqual((ushort)0x0007, ReadWord(ds, si));
        }

        [TestMethod]
        public void smsw_Ev_disassembles()
        {
            WriteByte(cs, ip, 0x0F);
            WriteByte(cs, (ushort)(ip + 1), 0x01);
            WriteByte(cs, (ushort)(ip + 2), 0xE0);

            var disassembler = new Disassembler(this, cs, ip);

            Assert.AreEqual("smsw ax", disassembler.Read());
        }

        [TestMethod]
        public void lmsw_Ev_disassembles()
        {
            WriteByte(cs, ip, 0x0F);
            WriteByte(cs, (ushort)(ip + 1), 0x01);
            WriteByte(cs, (ushort)(ip + 2), 0x34);

            var disassembler = new Disassembler(this, cs, ip);

            Assert.AreEqual("lmsw word ptr [si]", disassembler.Read());
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
        public void movzx_Gv_Ew_register()
        {
            ax = 0xFFFF;
            bx = 0x1234;
            FlagC = true;
            FlagZ = false;
            FlagO = true;

            WriteByte(cs, ip, 0x0F);
            WriteByte(cs, (ushort)(ip + 1), 0xB7);
            WriteByte(cs, (ushort)(ip + 2), 0xC3);

            step();

            Assert.AreEqual((ushort)0x1234, ax);
            Assert.AreEqual((ushort)0x1234, bx);
            Assert.IsTrue(FlagC);
            Assert.IsFalse(FlagZ);
            Assert.IsTrue(FlagO);
        }

        [TestMethod]
        public void movsx_Gv_Ew_memory()
        {
            si = 0x8000;
            WriteWord(ds, si, 0x9234);
            ax = 0;
            FlagC = false;
            FlagZ = true;
            FlagO = false;

            WriteByte(cs, ip, 0x0F);
            WriteByte(cs, (ushort)(ip + 1), 0xBF);
            WriteByte(cs, (ushort)(ip + 2), 0x04);

            step();

            Assert.AreEqual((ushort)0x9234, ax);
            Assert.AreEqual((ushort)0x9234, ReadWord(ds, si));
            Assert.IsFalse(FlagC);
            Assert.IsTrue(FlagZ);
            Assert.IsFalse(FlagO);
        }

        [TestMethod]
        public void imul_Gv_Ev_register()
        {
            ax = 20;
            bx = unchecked((ushort)-10);

            WriteByte(cs, ip, 0x0F);
            WriteByte(cs, (ushort)(ip + 1), 0xAF);
            WriteByte(cs, (ushort)(ip + 2), 0xC3);

            step();

            Assert.AreEqual(unchecked((ushort)-200), ax);
            Assert.AreEqual(unchecked((ushort)-10), bx);
            Assert.IsFalse(FlagC);
            Assert.IsFalse(FlagO);
        }

        [TestMethod]
        public void imul_Gv_Ev_memory_overflow()
        {
            ax = 0x4000;
            si = 0x8000;
            WriteWord(ds, si, 4);

            WriteByte(cs, ip, 0x0F);
            WriteByte(cs, (ushort)(ip + 1), 0xAF);
            WriteByte(cs, (ushort)(ip + 2), 0x04);

            step();

            Assert.AreEqual((ushort)0x0000, ax);
            Assert.AreEqual((ushort)4, ReadWord(ds, si));
            Assert.IsTrue(FlagC);
            Assert.IsTrue(FlagO);
        }

        [TestMethod]
        public void bsf_Gv_Ev_register()
        {
            ax = 0xFFFF;
            bx = 0x0120;
            FlagZ = true;

            WriteByte(cs, ip, 0x0F);
            WriteByte(cs, (ushort)(ip + 1), 0xBC);
            WriteByte(cs, (ushort)(ip + 2), 0xC3);

            step();

            Assert.AreEqual((ushort)5, ax);
            Assert.AreEqual((ushort)0x0120, bx);
            Assert.IsFalse(FlagZ);
        }

        [TestMethod]
        public void lss_Gv_Mp_memory_loads_register_and_stack_segment()
        {
            si = 0x8000;
            WriteWord(ds, si, 0x3456);
            WriteWord(ds, (ushort)(si + 2), 0x789A);
            ax = 0;
            ss = 0x1111;

            WriteByte(cs, ip, 0x0F);
            WriteByte(cs, (ushort)(ip + 1), 0xB2);
            WriteByte(cs, (ushort)(ip + 2), 0x04);

            step();

            Assert.AreEqual((ushort)0x3456, ax);
            Assert.AreEqual((ushort)0x789A, ss);
        }

        [TestMethod]
        public void lss_Gv_Mp_register_operand_is_invalid()
        {
            ax = 0x1111;
            ss = 0x2222;

            WriteByte(cs, ip, 0x0F);
            WriteByte(cs, (ushort)(ip + 1), 0xB2);
            WriteByte(cs, (ushort)(ip + 2), 0xC0);

            try
            {
                step();
                Assert.Fail("Expected invalid opcode");
            }
            catch (Sharp86.InvalidOpCodeException)
            {
            }

            Assert.AreEqual((ushort)0x1111, ax);
            Assert.AreEqual((ushort)0x2222, ss);
        }

        [TestMethod]
        public void bsr_Gv_Ev_memory()
        {
            ax = 0xFFFF;
            si = 0x8000;
            WriteWord(ds, si, 0x1200);
            FlagZ = true;

            WriteByte(cs, ip, 0x0F);
            WriteByte(cs, (ushort)(ip + 1), 0xBD);
            WriteByte(cs, (ushort)(ip + 2), 0x04);

            step();

            Assert.AreEqual((ushort)12, ax);
            Assert.AreEqual((ushort)0x1200, ReadWord(ds, si));
            Assert.IsFalse(FlagZ);
        }

        [TestMethod]
        public void bit_scan_zero_source_preserves_destination()
        {
            ax = 0x1357;
            bx = 0;
            FlagZ = false;

            WriteByte(cs, ip, 0x0F);
            WriteByte(cs, (ushort)(ip + 1), 0xBC);
            WriteByte(cs, (ushort)(ip + 2), 0xC3);

            step();

            Assert.AreEqual((ushort)0x1357, ax);
            Assert.AreEqual((ushort)0, bx);
            Assert.IsTrue(FlagZ);
        }

        [TestMethod]
        public void bt_Ev_Gv_register_sets_carry_without_modifying_destination()
        {
            ax = 0x0020;
            bx = 5;
            FlagC = false;

            WriteByte(cs, ip, 0x0F);
            WriteByte(cs, (ushort)(ip + 1), 0xA3);
            WriteByte(cs, (ushort)(ip + 2), 0xD8);

            step();

            Assert.AreEqual((ushort)0x0020, ax);
            Assert.AreEqual((ushort)5, bx);
            Assert.IsTrue(FlagC);
        }

        [TestMethod]
        public void shld_Ev_Gv_Ib_register_updates_destination_and_flags()
        {
            ax = 0x4000;
            bx = 0x0001;
            FlagC = true;
            FlagO = false;

            WriteByte(cs, ip, 0x0F);
            WriteByte(cs, (ushort)(ip + 1), 0xA4);
            WriteByte(cs, (ushort)(ip + 2), 0xD8);
            WriteByte(cs, (ushort)(ip + 3), 1);

            step();

            Assert.AreEqual((ushort)0x8000, ax);
            Assert.AreEqual((ushort)0x0001, bx);
            Assert.IsFalse(FlagC);
            Assert.IsTrue(FlagO);
            Assert.IsTrue(FlagS);
            Assert.IsFalse(FlagZ);
            Assert.IsTrue(FlagP);
        }

        [TestMethod]
        public void shld_Ev_Gv_cl_memory_shifts_bits_in_from_register()
        {
            si = 0x8000;
            WriteWord(ds, si, 0x1234);
            bx = 0xABCD;
            cl = 4;
            FlagC = false;

            WriteByte(cs, ip, 0x0F);
            WriteByte(cs, (ushort)(ip + 1), 0xA5);
            WriteByte(cs, (ushort)(ip + 2), 0x1C);

            step();

            Assert.AreEqual((ushort)0x234A, ReadWord(ds, si));
            Assert.AreEqual((ushort)0xABCD, bx);
            Assert.IsTrue(FlagC);
        }

        [TestMethod]
        public void shrd_Ev_Gv_Ib_register_updates_destination_and_flags()
        {
            ax = 0x8001;
            bx = 0x0000;
            FlagC = false;
            FlagO = false;

            WriteByte(cs, ip, 0x0F);
            WriteByte(cs, (ushort)(ip + 1), 0xAC);
            WriteByte(cs, (ushort)(ip + 2), 0xD8);
            WriteByte(cs, (ushort)(ip + 3), 1);

            step();

            Assert.AreEqual((ushort)0x4000, ax);
            Assert.AreEqual((ushort)0x0000, bx);
            Assert.IsTrue(FlagC);
            Assert.IsTrue(FlagO);
            Assert.IsFalse(FlagS);
            Assert.IsFalse(FlagZ);
            Assert.IsTrue(FlagP);
        }

        [TestMethod]
        public void shrd_Ev_Gv_cl_memory_shifts_bits_in_from_register()
        {
            si = 0x8000;
            WriteWord(ds, si, 0x1234);
            bx = 0xABCD;
            cl = 4;
            FlagC = true;

            WriteByte(cs, ip, 0x0F);
            WriteByte(cs, (ushort)(ip + 1), 0xAD);
            WriteByte(cs, (ushort)(ip + 2), 0x1C);

            step();

            Assert.AreEqual((ushort)0xD123, ReadWord(ds, si));
            Assert.AreEqual((ushort)0xABCD, bx);
            Assert.IsFalse(FlagC);
            Assert.IsTrue(FlagS);
        }

        [TestMethod]
        public void bts_Ev_Gv_register_sets_selected_bit()
        {
            ax = 0x0002;
            bx = 4;
            FlagC = true;

            WriteByte(cs, ip, 0x0F);
            WriteByte(cs, (ushort)(ip + 1), 0xAB);
            WriteByte(cs, (ushort)(ip + 2), 0xD8);

            step();

            Assert.AreEqual((ushort)0x0012, ax);
            Assert.AreEqual((ushort)4, bx);
            Assert.IsFalse(FlagC);
        }

        [TestMethod]
        public void btr_Ev_Gv_memory_uses_bit_offset_across_words()
        {
            si = 0x8000;
            WriteWord(ds, si, 0x0000);
            WriteWord(ds, (ushort)(si + 2), 0x0002);
            bx = 17;
            FlagC = false;

            WriteByte(cs, ip, 0x0F);
            WriteByte(cs, (ushort)(ip + 1), 0xB3);
            WriteByte(cs, (ushort)(ip + 2), 0x1C);

            step();

            Assert.AreEqual((ushort)0x0000, ReadWord(ds, si));
            Assert.AreEqual((ushort)0x0000, ReadWord(ds, (ushort)(si + 2)));
            Assert.AreEqual((ushort)17, bx);
            Assert.IsTrue(FlagC);
        }

        [TestMethod]
        public void btc_Ev_Gv_memory_toggles_selected_bit()
        {
            si = 0x8000;
            WriteWord(ds, si, 0x0000);
            bx = 3;
            FlagC = true;

            WriteByte(cs, ip, 0x0F);
            WriteByte(cs, (ushort)(ip + 1), 0xBB);
            WriteByte(cs, (ushort)(ip + 2), 0x1C);

            step();

            Assert.AreEqual((ushort)0x0008, ReadWord(ds, si));
            Assert.AreEqual((ushort)3, bx);
            Assert.IsFalse(FlagC);
        }

        [TestMethod]
        public void bt_Ev_Ib_register_sets_carry_without_modifying_destination()
        {
            ax = 0x0020;
            FlagC = false;

            WriteByte(cs, ip, 0x0F);
            WriteByte(cs, (ushort)(ip + 1), 0xBA);
            WriteByte(cs, (ushort)(ip + 2), 0xE0);
            WriteByte(cs, (ushort)(ip + 3), 5);

            step();

            Assert.AreEqual((ushort)0x0020, ax);
            Assert.IsTrue(FlagC);
        }

        [TestMethod]
        public void bts_Ev_Ib_memory_uses_bit_offset_across_words()
        {
            si = 0x8000;
            WriteWord(ds, si, 0x0000);
            WriteWord(ds, (ushort)(si + 2), 0x0000);
            FlagC = true;

            WriteByte(cs, ip, 0x0F);
            WriteByte(cs, (ushort)(ip + 1), 0xBA);
            WriteByte(cs, (ushort)(ip + 2), 0x2C);
            WriteByte(cs, (ushort)(ip + 3), 17);

            step();

            Assert.AreEqual((ushort)0x0000, ReadWord(ds, si));
            Assert.AreEqual((ushort)0x0002, ReadWord(ds, (ushort)(si + 2)));
            Assert.IsFalse(FlagC);
        }

        [TestMethod]
        public void btr_Ev_Ib_register_clears_selected_bit()
        {
            ax = 0x000A;
            FlagC = false;

            WriteByte(cs, ip, 0x0F);
            WriteByte(cs, (ushort)(ip + 1), 0xBA);
            WriteByte(cs, (ushort)(ip + 2), 0xF0);
            WriteByte(cs, (ushort)(ip + 3), 1);

            step();

            Assert.AreEqual((ushort)0x0008, ax);
            Assert.IsTrue(FlagC);
        }

        [TestMethod]
        public void btc_Ev_Ib_memory_toggles_selected_bit()
        {
            si = 0x8000;
            WriteWord(ds, si, 0x0008);
            FlagC = false;

            WriteByte(cs, ip, 0x0F);
            WriteByte(cs, (ushort)(ip + 1), 0xBA);
            WriteByte(cs, (ushort)(ip + 2), 0x3C);
            WriteByte(cs, (ushort)(ip + 3), 3);

            step();

            Assert.AreEqual((ushort)0x0000, ReadWord(ds, si));
            Assert.IsTrue(FlagC);
        }

        [TestMethod]
        public void cmpxchg_Eb_Gb_register_match_writes_source_to_destination()
        {
            al = 0x22;
            bl = 0x22;
            cl = 0x55;
            FlagZ = false;
            FlagC = true;

            WriteByte(cs, ip, 0x0F);
            WriteByte(cs, (ushort)(ip + 1), 0xB0);
            WriteByte(cs, (ushort)(ip + 2), 0xCB);

            step();

            Assert.AreEqual((byte)0x22, al);
            Assert.AreEqual((byte)0x55, bl);
            Assert.AreEqual((byte)0x55, cl);
            Assert.IsTrue(FlagZ);
            Assert.IsFalse(FlagC);
        }

        [TestMethod]
        public void cmpxchg_Ev_Gv_register_mismatch_loads_destination_into_accumulator()
        {
            ax = 0x1000;
            bx = 0x1001;
            cx = 0xABCD;
            FlagZ = true;
            FlagC = false;

            WriteByte(cs, ip, 0x0F);
            WriteByte(cs, (ushort)(ip + 1), 0xB1);
            WriteByte(cs, (ushort)(ip + 2), 0xCB);

            step();

            Assert.AreEqual((ushort)0x1001, ax);
            Assert.AreEqual((ushort)0x1001, bx);
            Assert.AreEqual((ushort)0xABCD, cx);
            Assert.IsFalse(FlagZ);
            Assert.IsTrue(FlagC);
        }

        [TestMethod]
        public void cmpxchg_Eb_Gb_memory_match_writes_source_to_memory()
        {
            si = 0x8000;
            WriteByte(ds, si, 0x80);
            al = 0x80;
            bl = 0x7F;
            FlagZ = false;

            WriteByte(cs, ip, 0x0F);
            WriteByte(cs, (ushort)(ip + 1), 0xB0);
            WriteByte(cs, (ushort)(ip + 2), 0x1C);

            step();

            Assert.AreEqual((byte)0x80, al);
            Assert.AreEqual((byte)0x7F, bl);
            Assert.AreEqual((byte)0x7F, ReadByte(ds, si));
            Assert.IsTrue(FlagZ);
        }

        [TestMethod]
        public void cmpxchg_Ev_Gv_memory_mismatch_preserves_memory_and_updates_accumulator()
        {
            si = 0x8000;
            WriteWord(ds, si, 0x00F0);
            ax = 0x000F;
            bx = 0x1234;
            FlagZ = true;

            WriteByte(cs, ip, 0x0F);
            WriteByte(cs, (ushort)(ip + 1), 0xB1);
            WriteByte(cs, (ushort)(ip + 2), 0x1C);

            step();

            Assert.AreEqual((ushort)0x00F0, ax);
            Assert.AreEqual((ushort)0x1234, bx);
            Assert.AreEqual((ushort)0x00F0, ReadWord(ds, si));
            Assert.IsFalse(FlagZ);
            Assert.IsTrue(FlagC);
        }

        [TestMethod]
        public void xadd_Eb_Gb_register_exchanges_operands_and_stores_sum()
        {
            al = 5;
            bl = 7;
            FlagC = true;
            FlagZ = true;

            WriteByte(cs, ip, 0x0F);
            WriteByte(cs, (ushort)(ip + 1), 0xC0);
            WriteByte(cs, (ushort)(ip + 2), 0xD8);

            step();

            Assert.AreEqual((byte)12, al);
            Assert.AreEqual((byte)5, bl);
            Assert.IsFalse(FlagC);
            Assert.IsFalse(FlagZ);
        }

        [TestMethod]
        public void xadd_Ev_Gv_register_updates_flags_from_word_addition()
        {
            ax = 0x7FFF;
            bx = 1;
            FlagO = false;
            FlagS = false;

            WriteByte(cs, ip, 0x0F);
            WriteByte(cs, (ushort)(ip + 1), 0xC1);
            WriteByte(cs, (ushort)(ip + 2), 0xD8);

            step();

            Assert.AreEqual((ushort)0x8000, ax);
            Assert.AreEqual((ushort)0x7FFF, bx);
            Assert.IsTrue(FlagO);
            Assert.IsTrue(FlagS);
        }

        [TestMethod]
        public void xadd_Eb_Gb_memory_destination_can_wrap_to_zero()
        {
            si = 0x8000;
            WriteByte(ds, si, 1);
            al = 0xFF;
            FlagC = false;
            FlagZ = false;

            WriteByte(cs, ip, 0x0F);
            WriteByte(cs, (ushort)(ip + 1), 0xC0);
            WriteByte(cs, (ushort)(ip + 2), 0x04);

            step();

            Assert.AreEqual((byte)0, ReadByte(ds, si));
            Assert.AreEqual((byte)1, al);
            Assert.IsTrue(FlagC);
            Assert.IsTrue(FlagZ);
        }

        [TestMethod]
        public void xadd_Ev_Gv_memory_destination_exchanges_original_value_with_register()
        {
            si = 0x8000;
            WriteWord(ds, si, 0x1000);
            ax = 0x0100;
            FlagC = true;

            WriteByte(cs, ip, 0x0F);
            WriteByte(cs, (ushort)(ip + 1), 0xC1);
            WriteByte(cs, (ushort)(ip + 2), 0x04);

            step();

            Assert.AreEqual((ushort)0x1100, ReadWord(ds, si));
            Assert.AreEqual((ushort)0x1000, ax);
            Assert.IsFalse(FlagC);
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
