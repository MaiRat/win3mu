using System;
using System.Collections.Generic;

namespace Win3muCore
{
    public class CommSupport
    {
        class PortState
        {
            public Win16.DCB Dcb;
            public Queue<byte> Input = new Queue<byte>();
            public int UngetChar = -1;
            public ushort EventMask;
            public ushort PendingEvents;
            public ushort NotifyWindow;
            public short WriteNotify;
            public short ReadNotify;
            public bool BreakActive;
        }

        readonly Dictionary<short, PortState> _ports = new Dictionary<short, PortState>();

        public static Win16.DCB CreateDefaultDcb(short cid)
        {
            return new Win16.DCB()
            {
                Id = unchecked((byte)cid),
                BaudRate = 1200,
                ByteSize = 8,
                Parity = 0,
                StopBits = 0,
                XonChar = 0x11,
                XoffChar = 0x13,
                XonLim = 10,
                XoffLim = 10,
                PeChar = 0,
                EofChar = 0,
                EvtChar = 0,
                TxDelay = 0,
                Flags1 = 0x01,
                Flags2 = 0x00,
            };
        }

        public static bool TryBuildDcb(string spec, out Win16.DCB dcb)
        {
            dcb = default;
            if (string.IsNullOrWhiteSpace(spec))
                return false;

            spec = spec.Trim();
            var colonPos = spec.IndexOf(':');
            if (colonPos <= 3 || !spec.StartsWith("COM", StringComparison.OrdinalIgnoreCase))
                return false;

            if (!TryParseCid(spec.Substring(0, colonPos), out var cid))
                return false;

            dcb = CreateDefaultDcb(cid);
            var parts = spec.Substring(colonPos + 1).Split(',');
            if (parts.Length > 0 && parts[0].Length != 0)
            {
                if (!TryParseBaud(parts[0], out var baud))
                    return false;
                dcb.BaudRate = baud;
            }

            if (parts.Length > 1 && parts[1].Length != 0)
            {
                switch (parts[1].Trim().ToUpperInvariant())
                {
                    case "N":
                        dcb.Parity = 0;
                        dcb.fParity = false;
                        break;
                    case "O":
                        dcb.Parity = 1;
                        dcb.fParity = true;
                        break;
                    case "E":
                        dcb.Parity = 2;
                        dcb.fParity = true;
                        break;
                    case "M":
                        dcb.Parity = 3;
                        dcb.fParity = true;
                        break;
                    case "S":
                        dcb.Parity = 4;
                        dcb.fParity = true;
                        break;
                    default:
                        return false;
                }
            }

            if (parts.Length > 2 && parts[2].Length != 0)
            {
                if (!byte.TryParse(parts[2].Trim(), out var byteSize))
                    return false;
                dcb.ByteSize = byteSize;
            }

            if (parts.Length > 3 && parts[3].Length != 0)
            {
                switch (parts[3].Trim())
                {
                    case "1":
                        dcb.StopBits = 0;
                        break;
                    case "1.5":
                        dcb.StopBits = 1;
                        break;
                    case "2":
                        dcb.StopBits = 2;
                        break;
                    default:
                        return false;
                }
            }

            return true;
        }

        static bool TryParseCid(string device, out short cid)
        {
            cid = -1;
            if (string.IsNullOrWhiteSpace(device) || device.Length < 4)
                return false;

            if (!device.StartsWith("COM", StringComparison.OrdinalIgnoreCase))
                return false;

            if (!int.TryParse(device.Substring(3), out var port) || port < 1 || port > 9)
                return false;

            cid = (short)(port - 1);
            return true;
        }

        static bool TryParseBaud(string value, out ushort baud)
        {
            baud = 0;
            switch (value.Trim())
            {
                case "11": baud = 110; return true;
                case "30": baud = 300; return true;
                case "60": baud = 600; return true;
                case "12": baud = 1200; return true;
                case "24": baud = 2400; return true;
                case "48": baud = 4800; return true;
                case "96": baud = 9600; return true;
                case "19": baud = 19200; return true;
            }

            return ushort.TryParse(value.Trim(), out baud);
        }

        public short OpenComm(string device, ushort cbInQueue, ushort cbOutQueue)
        {
            if (!TryParseCid(device, out var cid))
                return -1;

            if (_ports.ContainsKey(cid))
                return -1;

            _ports[cid] = new PortState()
            {
                Dcb = CreateDefaultDcb(cid),
            };
            return cid;
        }

        public short CloseComm(short cid)
        {
            return _ports.Remove(cid) ? (short)0 : (short)-1;
        }

        public short SetCommState(Win16.DCB dcb)
        {
            if (!_ports.TryGetValue((short)dcb.Id, out var port))
                return -1;

            port.Dcb = dcb;
            return 0;
        }

        public bool TryGetCommState(short cid, out Win16.DCB dcb)
        {
            if (_ports.TryGetValue(cid, out var port))
            {
                dcb = port.Dcb;
                return true;
            }

            dcb = default;
            return false;
        }

        public short GetCommError(short cid, out Win16.COMSTAT stat)
        {
            if (!_ports.TryGetValue(cid, out var port))
            {
                stat = default;
                return -1;
            }

            stat = new Win16.COMSTAT()
            {
                status = 0,
                cbInQue = (ushort)(port.Input.Count + (port.UngetChar >= 0 ? 1 : 0)),
                cbOutQue = 0,
            };
            return 0;
        }

        public short ReadComm(short cid, byte[] buffer, short count)
        {
            if (!_ports.TryGetValue(cid, out var port) || buffer == null || count < 0)
                return -1;

            var read = 0;
            if (port.UngetChar >= 0 && read < count)
            {
                buffer[read++] = unchecked((byte)port.UngetChar);
                port.UngetChar = -1;
            }

            while (read < count && port.Input.Count > 0)
            {
                buffer[read++] = port.Input.Dequeue();
            }

            return (short)read;
        }

        public short WriteComm(short cid, byte[] buffer, short count)
        {
            if (!_ports.ContainsKey(cid) || buffer == null || count < 0)
                return -1;

            return count;
        }

        public short TransmitCommChar(short cid, byte ch)
        {
            return _ports.ContainsKey(cid) ? (short)0 : (short)-1;
        }

        public uint SetCommEventMask(short cid, ushort mask)
        {
            if (!_ports.TryGetValue(cid, out var port))
                return 0;

            port.EventMask = mask;
            return 0;
        }

        public ushort GetCommEventMask(short cid, ushort clearMask)
        {
            if (!_ports.TryGetValue(cid, out var port))
                return 0;

            var events = (ushort)(port.PendingEvents & clearMask);
            port.PendingEvents &= unchecked((ushort)~clearMask);
            return events;
        }

        public short SetCommBreak(short cid)
        {
            if (!_ports.TryGetValue(cid, out var port))
                return -1;

            port.BreakActive = true;
            return 0;
        }

        public short ClearCommBreak(short cid)
        {
            if (!_ports.TryGetValue(cid, out var port))
                return -1;

            port.BreakActive = false;
            return 0;
        }

        public short UngetCommChar(short cid, byte ch)
        {
            if (!_ports.TryGetValue(cid, out var port))
                return -1;

            port.UngetChar = ch;
            return 0;
        }

        public short BuildCommDCB(string spec, out Win16.DCB dcb)
        {
            return TryBuildDcb(spec, out dcb) ? (short)0 : (short)-1;
        }

        public int EscapeCommFunction(short cid, ushort function)
        {
            return _ports.ContainsKey(cid) ? 0 : -1;
        }

        public short FlushComm(short cid, short function)
        {
            if (!_ports.TryGetValue(cid, out var port))
                return -1;

            port.Input.Clear();
            port.UngetChar = -1;
            return 0;
        }

        public bool EnableCommNotification(short cid, ushort hwnd, short cbWriteNotify, short cbOutQueue)
        {
            if (!_ports.TryGetValue(cid, out var port))
                return false;

            port.NotifyWindow = hwnd;
            port.WriteNotify = cbWriteNotify;
            port.ReadNotify = cbOutQueue;
            return true;
        }
    }
}
