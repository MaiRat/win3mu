using System;
using Sharp86;

namespace Win3muCore
{
    [Module("COMM", @"C:\WINDOWS\SYSTEM\COMM.DRV")]
    public class Comm : Module32
    {
        [EntryPoint(0x0001)]
        public short OpenComm(string device, ushort cbInQueue, ushort cbOutQueue)
        {
            return _machine.Comm.OpenComm(device, cbInQueue, cbOutQueue);
        }

        [EntryPoint(0x0002)]
        public short SetCommState(uint lpDcb)
        {
            return lpDcb == 0 ? (short)-1 : _machine.Comm.SetCommState(_machine.ReadStruct<Win16.DCB>(lpDcb));
        }

        [EntryPoint(0x0003)]
        public short GetCommState(short cid, uint lpDcb)
        {
            if (lpDcb == 0 || !_machine.Comm.TryGetCommState(cid, out var dcb))
                return -1;

            _machine.WriteStruct(lpDcb, dcb);
            return 0;
        }

        [EntryPoint(0x0004)]
        public short GetCommError(short cid, uint lpStat)
        {
            var result = _machine.Comm.GetCommError(cid, out var stat);
            if (lpStat != 0)
                _machine.WriteStruct(lpStat, stat);
            return result;
        }

        [EntryPoint(0x0005)]
        public short ReadComm(short cid, uint lpBuf, short cbRead)
        {
            if (lpBuf == 0 || cbRead < 0)
                return -1;

            var buffer = new byte[cbRead];
            var read = _machine.Comm.ReadComm(cid, buffer, cbRead);
            if (read > 0)
            {
                if (read != buffer.Length)
                {
                    var trimmed = new byte[read];
                    Array.Copy(buffer, trimmed, read);
                    buffer = trimmed;
                }
                _machine.WriteBytes(lpBuf, buffer);
            }
            return read;
        }

        [EntryPoint(0x0006)]
        public short WriteComm(short cid, uint lpBuf, short cbWrite)
        {
            if (lpBuf == 0 || cbWrite < 0)
                return -1;

            return _machine.Comm.WriteComm(cid, _machine.ReadBytes(lpBuf, cbWrite), cbWrite);
        }

        [EntryPoint(0x0007)]
        public short TransmitCommChar(short cid, byte ch)
        {
            return _machine.Comm.TransmitCommChar(cid, ch);
        }

        [EntryPoint(0x0008)]
        public short CloseComm(short cid)
        {
            return _machine.Comm.CloseComm(cid);
        }

        [EntryPoint(0x0009)]
        public uint SetCommEventMask(short cid, ushort mask)
        {
            return _machine.Comm.SetCommEventMask(cid, mask);
        }

        [EntryPoint(0x000A)]
        public ushort GetCommEventMask(short cid, ushort clearMask)
        {
            return _machine.Comm.GetCommEventMask(cid, clearMask);
        }

        [EntryPoint(0x000B)]
        public short SetCommBreak(short cid)
        {
            return _machine.Comm.SetCommBreak(cid);
        }

        [EntryPoint(0x000C)]
        public short ClearCommBreak(short cid)
        {
            return _machine.Comm.ClearCommBreak(cid);
        }

        [EntryPoint(0x000D)]
        public short UngetCommChar(short cid, byte ch)
        {
            return _machine.Comm.UngetCommChar(cid, ch);
        }

        [EntryPoint(0x000E)]
        public short BuildCommDCB(string spec, uint lpDcb)
        {
            var result = _machine.Comm.BuildCommDCB(spec, out var dcb);
            if (result == 0 && lpDcb != 0)
                _machine.WriteStruct(lpDcb, dcb);
            return result;
        }

        [EntryPoint(0x000F)]
        public int EscapeCommFunction(short cid, ushort function)
        {
            return _machine.Comm.EscapeCommFunction(cid, function);
        }

        [EntryPoint(0x0010)]
        public short FlushComm(short cid, short function)
        {
            return _machine.Comm.FlushComm(cid, function);
        }
    }
}
