using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Win3muCore;

namespace Win3muCoreUnitTests
{
    [TestClass]
    public class CommSupportTests
    {
        [TestMethod]
        public void TryBuildDcb_ParsesClassicSpec()
        {
            Assert.IsTrue(CommSupport.TryBuildDcb("COM1:96,n,8,1", out var dcb));
            Assert.AreEqual((byte)0, dcb.Id);
            Assert.AreEqual((ushort)9600, dcb.BaudRate);
            Assert.AreEqual((byte)8, dcb.ByteSize);
            Assert.AreEqual((byte)0, dcb.Parity);
            Assert.AreEqual((byte)0, dcb.StopBits);
            Assert.IsTrue(dcb.fBinary);
            Assert.IsFalse(dcb.fParity);
        }

        [TestMethod]
        public void TryBuildDcb_InvalidSpec_ReturnsFalse()
        {
            Assert.IsFalse(CommSupport.TryBuildDcb("LPT1:96,n,8,1", out _));
            Assert.IsFalse(CommSupport.TryBuildDcb("COM1:=96,n,8,1", out _));
        }

        [TestMethod]
        public void OpenComm_SetState_GetState_RoundTrips()
        {
            var comm = new CommSupport();
            var cid = comm.OpenComm("COM1", 128, 128);
            Assert.AreEqual(0, cid);
            Assert.AreEqual(-1, comm.OpenComm("COM1", 128, 128));

            var dcb = CommSupport.CreateDefaultDcb((short)cid);
            dcb.BaudRate = 4800;
            dcb.ByteSize = 7;
            dcb.Parity = 2;
            dcb.fParity = true;
            Assert.AreEqual(0, comm.SetCommState(dcb));

            Assert.IsTrue(comm.TryGetCommState((short)cid, out var roundTrip));
            Assert.AreEqual((ushort)4800, roundTrip.BaudRate);
            Assert.AreEqual((byte)7, roundTrip.ByteSize);
            Assert.AreEqual((byte)2, roundTrip.Parity);
            Assert.IsTrue(roundTrip.fParity);
            Assert.AreEqual(0, comm.CloseComm((short)cid));
            Assert.AreEqual(-1, comm.CloseComm((short)cid));
        }

        [TestMethod]
        public void UngetCommChar_ReadComm_ReturnsQueuedByte()
        {
            var comm = new CommSupport();
            var cid = (short)comm.OpenComm("COM2", 64, 64);
            Assert.AreEqual(0, comm.UngetCommChar(cid, (byte)'A'));

            var buffer = new byte[4];
            var read = comm.ReadComm(cid, buffer, 4);

            Assert.AreEqual(1, read);
            Assert.AreEqual((byte)'A', buffer[0]);
        }

        [TestMethod]
        public void CommModule_ExportsStandardOrdinals()
        {
            var exports = new Comm().GetExports().OrderBy(x => x).ToArray();
            CollectionAssert.AreEqual(Enumerable.Range(1, 16).Select(x => (ushort)x).ToArray(), exports);
            Assert.AreEqual(1, new Comm().GetOrdinalFromName("OpenComm"));
            Assert.AreEqual("FlushComm", new Comm().GetNameFromOrdinal(16));
        }
    }
}
