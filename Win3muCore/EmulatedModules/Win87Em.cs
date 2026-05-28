using System.Collections.Generic;
using Sharp86;

namespace Win3muCore
{
    [Module("WIN87EM", @"C:\WINDOWS\SYSTEM\WIN87EM.DLL")]
    public class Win87Em : Module32
    {
        const ushort DefaultControlWord = 0x1332;
        const ushort ControlWordMask = 0x00C3;
        const ushort Version = 0x0600;
        const ushort SaveAreaSize = 0x01D5;
        const ushort Have80x87 = 1;

        readonly HashSet<byte> _warnedInterrupts = new HashSet<byte>();
        readonly HashSet<ushort> _warnedEscInstructions = new HashSet<ushort>();

        ushort _refCount;
        ushort _controlWord;
        ushort _internalControlWord;
        ushort _statusWord1;
        ushort _statusWord2;
        ushort _signalHandlerOffset;
        ushort _signalHandlerSegment;
        ushort _extendedStack;

        public override void Load(Machine machine)
        {
            base.Load(machine);
            ResetState();
        }

        void ResetState()
        {
            _warnedInterrupts.Clear();
            _warnedEscInstructions.Clear();
            _refCount = 0;
            _controlWord = DefaultControlWord;
            _internalControlWord = (ushort)(DefaultControlWord & ~ControlWordMask);
            _statusWord1 = 0x000B;
            _statusWord2 = 0;
            _signalHandlerOffset = 0;
            _signalHandlerSegment = 0;
            _extendedStack = 0;
        }

        void Initialize()
        {
            _controlWord = DefaultControlWord;
            _internalControlWord = (ushort)(DefaultControlWord & ~ControlWordMask);
            _statusWord2 = 0;
        }

        ushort GetStatusWord()
        {
            return (ushort)(((_statusWord1 & 0x003F) | _statusWord2) & ~0xE000);
        }

        void ClearExceptions()
        {
            _machine.ax = 0;
            _statusWord2 = 0;
        }

        void SetControlWord(ushort controlWord)
        {
            _controlWord = controlWord;
            _internalControlWord = (ushort)(controlWord & ~ControlWordMask);
            _machine.ax = _internalControlWord;
        }

        void WriteWordIfFits(uint ptr, ushort byteCount, int wordIndex, ushort value)
        {
            if (ptr == 0)
                return;

            int byteOffset = wordIndex * 2;
            if (byteOffset + 2 > byteCount)
                return;

            _machine.WriteWord(ptr.Hiword(), (ushort)(ptr.Loword() + byteOffset), value);
        }

        ushort ReadWordIfFits(uint ptr, ushort byteCount, int wordIndex, ushort fallback)
        {
            if (ptr == 0)
                return fallback;

            int byteOffset = wordIndex * 2;
            if (byteOffset + 2 > byteCount)
                return fallback;

            return _machine.ReadWord(ptr.Hiword(), (ushort)(ptr.Loword() + byteOffset));
        }

        // WIN87EM's loader-facing contract is mostly state/control APIs.
        // The actual x87 instruction emulation behind INT 34h-3Dh can be
        // expanded later if a guest depends on full software FP behavior.
        public bool HandleInterrupt(byte interruptNumber)
        {
            switch (interruptNumber)
            {
                case 0x34:
                case 0x35:
                case 0x36:
                case 0x37:
                case 0x38:
                case 0x39:
                case 0x3A:
                case 0x3B:
                case 0x3C:
                    if (_warnedInterrupts.Add(interruptNumber))
                    {
                        Log.WriteLine("Warning: WIN87EM interrupt {0:X2} invoked; treating as compatibility no-op", interruptNumber);
                    }
                    return true;

                case 0x3D:
                    return true;
            }

            return false;
        }

        [EntryPoint(1, "__fpMath")]
        public void FpMath()
        {
            switch (_machine.bx)
            {
                case 0:
                    _refCount++;
                    Initialize();
                    ClearExceptions();
                    break;

                case 1:
                case 2:
                    if (_machine.bx == 2 && _refCount != 0)
                        _refCount--;
                    Initialize();
                    break;

                case 3:
                    _signalHandlerOffset = _machine.ax;
                    _signalHandlerSegment = _machine.dx;
                    break;

                case 4:
                    SetControlWord(_machine.ax);
                    break;

                case 5:
                    _machine.ax = _controlWord;
                    break;

                case 6:
                case 7:
                    break;

                case 8:
                    _machine.ax = GetStatusWord();
                    _statusWord2 = _machine.ax;
                    break;

                case 9:
                    ClearExceptions();
                    break;

                case 10:
                    _machine.ax = 0;
                    break;

                case 11:
                    _machine.dx = 0;
                    _machine.ax = Have80x87;
                    break;

                case 12:
                    _extendedStack = _machine.ax;
                    break;

                default:
                    _machine.ax = 0xFFFF;
                    _machine.dx = 0xFFFF;
                    break;
            }
        }

        public bool HandleInvalidOpcodeFault()
        {
            ushort faultIp = _machine.ReadWord(_machine.ss, _machine.sp);
            ushort faultCs = _machine.ReadWord(_machine.ss, (ushort)(_machine.sp + 2));
            ushort nextIp;
            if (!TryHandleEscInstruction(faultCs, faultIp, out nextIp))
                return false;

            _machine.WriteWord(_machine.ss, _machine.sp, nextIp);
            return true;
        }

            bool TryHandleEscInstruction(ushort cs, ushort ip, out ushort nextIp)
            {
                ushort segmentOverride = 0;
                ushort instructionIp = ip;

                while (true)
                {
                    switch (_machine.ReadByte(cs, instructionIp))
                    {
                        case 0x26:
                            segmentOverride = _machine.es;
                            instructionIp++;
                            continue;

                        case 0x2E:
                            segmentOverride = _machine.cs;
                            instructionIp++;
                            continue;

                        case 0x36:
                            segmentOverride = _machine.ss;
                            instructionIp++;
                            continue;

                        case 0x3E:
                            segmentOverride = _machine.ds;
                            instructionIp++;
                            continue;
                    }

                    break;
                }

                byte opcode = _machine.ReadByte(cs, instructionIp);
                if (opcode < 0xD8 || opcode > 0xDF)
                {
                    nextIp = ip;
                    return false;
                }

                byte modrm = _machine.ReadByte(cs, (ushort)(instructionIp + 1));
                ushort decodedNextIp;
                bool hasMemoryOperand = TryResolveMemoryOperand(cs, instructionIp, segmentOverride, out ushort operandSegment, out ushort operandOffset, out decodedNextIp);

                switch (opcode)
                {
                    case 0xD9:
                        if (hasMemoryOperand)
                        {
                            switch ((modrm >> 3) & 0x07)
                            {
                                case 5:
                                    _controlWord = _machine.ReadWord(operandSegment, operandOffset);
                                    _internalControlWord = (ushort)(_controlWord & ~ControlWordMask);
                                    nextIp = decodedNextIp;
                                    return true;

                                case 7:
                                    _machine.WriteWord(operandSegment, operandOffset, _controlWord);
                                    nextIp = decodedNextIp;
                                    return true;
                            }
                        }
                        break;

                    case 0xDB:
                        switch (modrm)
                        {
                            case 0xE1:
                                _machine.ax = GetStatusWord();
                                _statusWord2 = _machine.ax;
                                nextIp = decodedNextIp;
                                return true;

                            case 0xE2:
                                ClearExceptions();
                                nextIp = decodedNextIp;
                                return true;

                            case 0xE3:
                                Initialize();
                                nextIp = decodedNextIp;
                                return true;
                        }
                        break;
                }

                ushort warningKey = (ushort)((opcode << 8) | modrm);
                if (_warnedEscInstructions.Add(warningKey))
                {
                    Log.WriteLine("Warning: WIN87EM ESC opcode {0:X2} {1:X2} invoked; treating as compatibility no-op", opcode, modrm);
                }

                nextIp = decodedNextIp;
                return true;
        }

        bool TryResolveMemoryOperand(ushort cs, ushort instructionIp, ushort segmentOverride, out ushort operandSegment, out ushort operandOffset, out ushort nextIp)
        {
                byte modrm = _machine.ReadByte(cs, (ushort)(instructionIp + 1));
                int mod = (modrm >> 6) & 0x03;
                int rm = modrm & 0x07;

                ushort displacement = 0;
                int displacementSize = 0;
                bool usesStackSegment = false;
                int baseOffset = 0;

                switch (mod)
                {
                    case 0:
                        if (rm == 6)
                        {
                            displacement = _machine.ReadWord(cs, (ushort)(instructionIp + 2));
                            displacementSize = 2;
                        }
                        break;

                    case 1:
                        displacement = (ushort)(short)(sbyte)_machine.ReadByte(cs, (ushort)(instructionIp + 2));
                        displacementSize = 1;
                        break;

                    case 2:
                        displacement = _machine.ReadWord(cs, (ushort)(instructionIp + 2));
                        displacementSize = 2;
                        break;

                    default:
                        nextIp = (ushort)(instructionIp + 2);
                        operandSegment = 0;
                        operandOffset = 0;
                        return false;
                }

                switch (rm)
                {
                    case 0:
                        baseOffset = _machine.bx + _machine.si;
                        break;

                    case 1:
                        baseOffset = _machine.bx + _machine.di;
                        break;

                    case 2:
                        baseOffset = _machine.bp + _machine.si;
                        usesStackSegment = true;
                        break;

                    case 3:
                        baseOffset = _machine.bp + _machine.di;
                        usesStackSegment = true;
                        break;

                    case 4:
                        baseOffset = _machine.si;
                        break;

                    case 5:
                        baseOffset = _machine.di;
                        break;

                    case 6:
                        if (mod == 0)
                        {
                            baseOffset = 0;
                        }
                        else
                        {
                            baseOffset = _machine.bp;
                            usesStackSegment = true;
                        }
                        break;

                    case 7:
                        baseOffset = _machine.bx;
                        break;
                }

                operandSegment = segmentOverride != 0 ? segmentOverride : (usesStackSegment ? _machine.ss : _machine.ds);
                operandOffset = (ushort)(baseOffset + displacement);
            nextIp = (ushort)(instructionIp + 2 + displacementSize);
            return true;
        }

        [EntryPoint(3, "__WinEm87Info")]
        public void WinEm87Info(uint pWin87EmInfo, ushort cbWin87EmInfo)
        {
            WriteWordIfFits(pWin87EmInfo, cbWin87EmInfo, 0, Version);
            WriteWordIfFits(pWin87EmInfo, cbWin87EmInfo, 1, SaveAreaSize);
            WriteWordIfFits(pWin87EmInfo, cbWin87EmInfo, 2, _machine.ds);
            WriteWordIfFits(pWin87EmInfo, cbWin87EmInfo, 3, _machine.cs);
            WriteWordIfFits(pWin87EmInfo, cbWin87EmInfo, 4, Have80x87);
            WriteWordIfFits(pWin87EmInfo, cbWin87EmInfo, 5, 0);
        }

        [EntryPoint(4, "__WinEm87Restore")]
        public void WinEm87Restore(uint pWin87EmSaveArea, ushort cbWin87EmSaveArea)
        {
            _controlWord = ReadWordIfFits(pWin87EmSaveArea, cbWin87EmSaveArea, 0, _controlWord);
            _internalControlWord = ReadWordIfFits(pWin87EmSaveArea, cbWin87EmSaveArea, 1, _internalControlWord);
            _statusWord1 = ReadWordIfFits(pWin87EmSaveArea, cbWin87EmSaveArea, 2, _statusWord1);
            _statusWord2 = ReadWordIfFits(pWin87EmSaveArea, cbWin87EmSaveArea, 3, _statusWord2);
            _signalHandlerOffset = ReadWordIfFits(pWin87EmSaveArea, cbWin87EmSaveArea, 4, _signalHandlerOffset);
            _signalHandlerSegment = ReadWordIfFits(pWin87EmSaveArea, cbWin87EmSaveArea, 5, _signalHandlerSegment);
            _extendedStack = ReadWordIfFits(pWin87EmSaveArea, cbWin87EmSaveArea, 6, _extendedStack);
        }

        [EntryPoint(5, "__WinEm87Save")]
        public void WinEm87Save(uint pWin87EmSaveArea, ushort cbWin87EmSaveArea)
        {
            WriteWordIfFits(pWin87EmSaveArea, cbWin87EmSaveArea, 0, _controlWord);
            WriteWordIfFits(pWin87EmSaveArea, cbWin87EmSaveArea, 1, _internalControlWord);
            WriteWordIfFits(pWin87EmSaveArea, cbWin87EmSaveArea, 2, _statusWord1);
            WriteWordIfFits(pWin87EmSaveArea, cbWin87EmSaveArea, 3, _statusWord2);
            WriteWordIfFits(pWin87EmSaveArea, cbWin87EmSaveArea, 4, _signalHandlerOffset);
            WriteWordIfFits(pWin87EmSaveArea, cbWin87EmSaveArea, 5, _signalHandlerSegment);
            WriteWordIfFits(pWin87EmSaveArea, cbWin87EmSaveArea, 6, _extendedStack);
        }
    }
}
