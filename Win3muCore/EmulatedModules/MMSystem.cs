/*
Win3mu - Windows 3 Emulator
Copyright (C) 2017 Topten Software.

Win3mu is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

Win3mu is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with Win3mu.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Sharp86;

namespace Win3muCore
{
    [Module("MMSYSTEM", @"C:\WINDOWS\SYSTEM\MMSYSTEM.DLL")]
    public class MMSystem : Module32
    {
        public string ResolveMediaFile(string filename)
        {
            if (string.IsNullOrEmpty(filename))
                return filename;

            // If plain filename, look in same folder as exe
            if (!filename.Contains('\\'))
            {
                var exeHostPath = System.IO.Path.GetDirectoryName(_machine.ProgramHostPath);
                var testFile = System.IO.Path.Combine(exeHostPath, filename);
                if (System.IO.File.Exists(testFile))
                    return testFile;
            }

            // Qualify path using current directory
            var fullPath = _machine.Dos.QualifyPath(filename);

            // Try to map to host path
            var hostPath = _machine.PathMapper.TryMapGuestToHost(fullPath, false);

            // If couldn't use filename as is
            if (hostPath == null)
                return filename;

            // If file doesn't exist, use filename as is
            if (!System.IO.File.Exists(hostPath))
                return filename;

            return hostPath;
        }


        // 0001 - WEP - 0001

        [DllImport("winmm.dll")]
        public static extern bool sndPlaySound(IntPtr ptr, uint flags);
        [DllImport("winmm.dll")]
        public static extern bool sndPlaySound(string str, uint flags);

        [EntryPoint(0x0002)]
        public bool sndPlaySound(uint pszSound, ushort flags)
        {
            if ((flags & 0x0004) != 0) // SND_MEMORY
            {
                // Very conveniently, Windows 7 copies up to 2mb wave file to its own memory block
                // So ok to pass fixed ptr and unfix immediately
                // See:  https://blogs.msdn.microsoft.com/larryosterman/2009/06/24/windows-7-fixes-the-playsoundxxx-snd_memorysnd_async-anti-pattern/
                using (var hp = _machine.GlobalHeap.GetHeapPointer(pszSound, false))
                {
                    return sndPlaySound(hp, flags);
                }
            }
            else
            {
                var str = ResolveMediaFile(_machine.ReadString(pszSound));
                return sndPlaySound(str, flags);
            }
        }




        // 0005 - MMSYSTEMGETVERSION - 0005
        // 0006 - DRIVERPROC - 0006
        // 001E - OUTPUTDEBUGSTR - 001E
        // 001F - DRIVERCALLBACK - 001F
        // 0020 - STACKENTER - 0020
        // 0021 - STACKLEAVE - 0021
        // 0022 - MMDRVINSTALL - 0022
        // 0065 - JOYGETNUMDEVS - 0065
        // 0066 - JOYGETDEVCAPS - 0066
        // 0067 - JOYGETPOS - 0067
        // 0068 - JOYGETTHRESHOLD - 0068
        // 0069 - JOYRELEASECAPTURE - 0069
        // 006A - JOYSETCAPTURE - 006A
        // 006B - JOYSETTHRESHOLD - 006B
        // 006D - JOYSETCALIBRATION - 006D

        [EntryPoint(0x00c9)]
        [DllImport("winmm.dll")]
        public static extern nuint midiOutGetNumDevs();

        // 00CA - MIDIOUTGETDEVCAPS - 00CA
        // 00CB - MIDIOUTGETERRORTEXT - 00CB
        // 00CC - MIDIOUTOPEN - 00CC
        // 00CD - MIDIOUTCLOSE - 00CD
        // 00CE - MIDIOUTPREPAREHEADER - 00CE
        // 00CF - MIDIOUTUNPREPAREHEADER - 00CF
        // 00D0 - MIDIOUTSHORTMSG - 00D0
        // 00D1 - MIDIOUTLONGMSG - 00D1
        // 00D2 - MIDIOUTRESET - 00D2
        // 00D3 - MIDIOUTGETVOLUME - 00D3
        // 00D4 - MIDIOUTSETVOLUME - 00D4
        // 00D5 - MIDIOUTCACHEPATCHES - 00D5
        // 00D6 - MIDIOUTCACHEDRUMPATCHES - 00D6
        // 00D7 - MIDIOUTGETID - 00D7
        // 00D8 - MIDIOUTMESSAGE - 00D8
        // 012D - MIDIINGETNUMDEVS - 012D
        // 012E - MIDIINGETDEVCAPS - 012E
        // 012F - MIDIINGETERRORTEXT - 012F
        // 0130 - MIDIINOPEN - 0130
        // 0131 - MIDIINCLOSE - 0131
        // 0132 - MIDIINPREPAREHEADER - 0132
        // 0133 - MIDIINUNPREPAREHEADER - 0133
        // 0134 - MIDIINADDBUFFER - 0134
        // 0135 - MIDIINSTART - 0135
        // 0136 - MIDIINSTOP - 0136
        // 0137 - MIDIINRESET - 0137
        // 0138 - MIDIINGETID - 0138
        // 0139 - MIDIINMESSAGE - 0139
        // 015E - AUXGETNUMDEVS - 015E
        // 015F - AUXGETDEVCAPS - 015F
        // 0160 - AUXGETVOLUME - 0160
        // 0161 - AUXSETVOLUME - 0161
        // 0162 - AUXOUTMESSAGE - 0162

        [EntryPoint(0x0191)]
        [DllImport("winmm.dll")]
        public static extern nuint waveOutGetNumDevs();

        // 0192 - WAVEOUTGETDEVCAPS - 0192
        // 0193 - WAVEOUTGETERRORTEXT - 0193
        // 0194 - WAVEOUTOPEN - 0194
        // 0195 - WAVEOUTCLOSE - 0195
        // 0196 - WAVEOUTPREPAREHEADER - 0196
        // 0197 - WAVEOUTUNPREPAREHEADER - 0197
        // 0198 - WAVEOUTWRITE - 0198
        // 0199 - WAVEOUTPAUSE - 0199
        // 019A - WAVEOUTRESTART - 019A
        // 019B - WAVEOUTRESET - 019B
        // 019C - WAVEOUTGETPOSITION - 019C
        // 019D - WAVEOUTGETPITCH - 019D
        // 019E - WAVEOUTSETPITCH - 019E
        // 019F - WAVEOUTGETVOLUME - 019F
        // 01A0 - WAVEOUTSETVOLUME - 01A0
        // 01A1 - WAVEOUTGETPLAYBACKRATE - 01A1
        // 01A2 - WAVEOUTSETPLAYBACKRATE - 01A2
        // 01A3 - WAVEOUTBREAKLOOP - 01A3
        // 01A4 - WAVEOUTGETID - 01A4
        // 01A5 - WAVEOUTMESSAGE - 01A5
        // 01F5 - WAVEINGETNUMDEVS - 01F5
        // 01F6 - WAVEINGETDEVCAPS - 01F6
        // 01F7 - WAVEINGETERRORTEXT - 01F7
        // 01F8 - WAVEINOPEN - 01F8
        // 01F9 - WAVEINCLOSE - 01F9
        // 01FA - WAVEINPREPAREHEADER - 01FA
        // 01FB - WAVEINUNPREPAREHEADER - 01FB
        // 01FC - WAVEINADDBUFFER - 01FC
        // 01FD - WAVEINSTART - 01FD
        // 01FE - WAVEINSTOP - 01FE
        // 01FF - WAVEINRESET - 01FF
        // 0200 - WAVEINGETPOSITION - 0200
        // 0201 - WAVEINGETID - 0201
        // 0202 - WAVEINMESSAGE - 0202
        // 0259 - TIMEGETSYSTEMTIME - 0259
        // 025A - TIMESETEVENT - 025A
        // 025B - TIMEKILLEVENT - 025B
        // 025C - TIMEGETDEVCAPS - 025C
        // 025D - TIMEBEGINPERIOD - 025D
        // 025E - TIMEENDPERIOD - 025E
        // 025F - TIMEGETTIME - 025F

        [DllImport("winmm.dll", CharSet = CharSet.Unicode, EntryPoint = "mciSendCommandW")]
        public static extern uint mciSendCommand(uint IDDevice, uint message, IntPtr fdwCommand, IntPtr dwParam);

        [DllImport("winmm.dll", CharSet = CharSet.Unicode, EntryPoint = "mciGetErrorStringW")]
        public static extern bool _mciGetErrorString(uint error, StringBuilder buffer, uint length);

        HandleMap _mciDeviceIdMap = new HandleMap();

        ushort DeviceIdTo16(uint deviceId)
        {
            return _mciDeviceIdMap.To16(BitUtils.DWordToIntPtr(deviceId));
        }

        uint DeviceIdTo32(ushort deviceId)
        {
            return _mciDeviceIdMap.To32(deviceId).DWord();
        }

        void SetCallback(ref Win32.MCI_GENERIC_PARAMS st32, uint callback)
        {
            if (HWND.Map.IsValid16(callback.Loword()))
                st32.dwCallback = HWND.Map.To32(callback.Loword());
        }

        uint SendGenericMciCommand(ushort uDeviceId, ushort uMessage, uint dwParam1, uint dwParam2)
        {
            if (dwParam2 == 0)
                return mciSendCommand(DeviceIdTo32(uDeviceId), uMessage, (IntPtr)dwParam1, IntPtr.Zero);

            var st16 = _machine.ReadStruct<Win16.MCI_GENERIC_PARAMS>(dwParam2);
            var st32 = new Win32.MCI_GENERIC_PARAMS();
            SetCallback(ref st32, st16.dwCallback);

            unsafe
            {
                return mciSendCommand(DeviceIdTo32(uDeviceId), uMessage, (IntPtr)dwParam1, (IntPtr)(&st32));
            }
        }

        [EntryPoint(0x02bd)]
        public uint mciSendCommand(ushort uDeviceId, ushort uMessage, uint dwParam1, uint dwParam2)
        {
            switch (uMessage)
            {
                case Win16.MCI_OPEN:
                {
                    using (var ctx = new TempContext(_machine))
                    {
                        var op16 = _machine.ReadStruct<Win16.MCI_OPEN_PARAMS>(dwParam2);
                        var op32 = new Win32.MCI_OPEN_PARAMS();

                        // Convert type
                        if ((dwParam1 & Win16.MCI_OPEN_TYPE) != 0)
                        {
                            if ((dwParam1 & Win16.MCI_OPEN_TYPE_ID) != 0)
                                op32.lpstrDeviceName = BitUtils.DWordToIntPtr(op16.lpstrDeviceName);
                            else
                                op32.lpstrDeviceName = ctx.AllocUnmanagedString(_machine.ReadString(op16.lpstrDeviceName));
                        }

                        // Convert element
                        if ((dwParam1 & Win16.MCI_OPEN_ELEMENT) != 0)
                        {
                            if ((dwParam1 & Win16.MCI_OPEN_ELEMENT_ID) != 0)
                                op32.lpstrElementName = BitUtils.DWordToIntPtr(op16.lpstrElementName);
                            else
                                op32.lpstrElementName = ctx.AllocUnmanagedString(ResolveMediaFile(_machine.ReadString(op16.lpstrElementName)));
                        }

                        // Convert element
                        if ((dwParam1 & Win16.MCI_OPEN_ALIAS) != 0)
                        {
                            op32.lpstrAlias = Marshal.StringToHGlobalUni(_machine.ReadString(op16.lpstrAlias));
                        }

                        // Callback
                        if (HWND.Map.IsValid16(op16.dwCallback.Loword()))
                            op32.dwCallback = HWND.Map.To32(op16.dwCallback.Loword());

                        // Open
                        unsafe
                        {
                            Win32.MCI_OPEN_PARAMS* p = &op32;
                            uint retv = mciSendCommand(uDeviceId, Win32.MCI_OPEN, (IntPtr)dwParam1, (IntPtr)p);
                            if (retv == 0)
                            {
                                op16.wDeviceID = DeviceIdTo16(op32.nDeviceID);
                            }
                            else
                            {
                                op16.wDeviceID = 0;
                            }
                            _machine.WriteStruct(dwParam2, op16);
                            return retv;
                        }
                    }
                }

                case Win16.MCI_CLOSE:
                {
                    if (dwParam2 == 0)
                    {
                        return mciSendCommand(DeviceIdTo32(uDeviceId), Win32.MCI_CLOSE, (IntPtr)dwParam1, IntPtr.Zero);
                    }
                    else
                    {
                        var st16 = _machine.ReadStruct<Win16.MCI_GENERIC_PARAMS>(dwParam2);
                        var st32 = new Win32.MCI_GENERIC_PARAMS();

                        if (HWND.Map.IsValid16(st16.dwCallback.Loword()))
                            st32.dwCallback = HWND.Map.To32(st16.dwCallback.Loword());

                        unsafe
                        {
                            return mciSendCommand(DeviceIdTo32(uDeviceId), Win32.MCI_CLOSE, (IntPtr)dwParam1, (IntPtr)(&st32));
                        }
                    }
                }

                case Win16.MCI_PLAY:
                {
                    var st16 = _machine.ReadStruct<Win16.MCI_PLAY_PARAMS>(dwParam2);
                    var st32 = new Win32.MCI_PLAY_PARAMS();

                    st32.dwFrom = st16.dwFrom;
                    st32.dwTo = st16.dwTo;

                    if (HWND.Map.IsValid16(st16.dwCallback.Loword()))
                        st32.dwCallback = HWND.Map.To32(st16.dwCallback.Loword());

                    unsafe
                    {
                        return mciSendCommand(DeviceIdTo32(uDeviceId), Win32.MCI_PLAY, (IntPtr)dwParam1, (IntPtr)(&st32));
                    }
                }

                case Win16.MCI_STATUS:
                {
                    
                    var st16 = _machine.ReadStruct<Win16.MCI_STATUS_PARAMS>(dwParam2);
                    var st32 = new Win32.MCI_STATUS_PARAMS();

                    st32.dwItem = st16.dwItem;
                    st32.dwTrack = st16.dwTrack;
                                                   
                    if (HWND.Map.IsValid16(st16.dwCallback.Loword()))
                        st32.dwCallback = HWND.Map.To32(st16.dwCallback.Loword());

                    unsafe
                    {
                        uint retv = mciSendCommand(DeviceIdTo32(uDeviceId), Win32.MCI_STATUS, (IntPtr)dwParam1, (IntPtr)(&st32));
                        st16.dwReturn = st32.dwReturn.DWord(); 
                        _machine.WriteStruct(dwParam2, st16);
                        return retv;
                    }
                }

                case Win16.MCI_STOP:
                case Win16.MCI_PAUSE:
                case Win16.MCI_RESUME:
                case Win16.MCI_SPIN:
                case Win16.MCI_STEP:
                case Win16.MCI_CUE:
                case Win16.MCI_BREAK:
                case Win16.MCI_ESCAPE:
                case Win16.MCI_REALIZE:
                case Win16.MCI_FREEZE:
                case Win16.MCI_UNFREEZE:
                case Win16.MCI_CUT:
                case Win16.MCI_COPY:
                case Win16.MCI_PASTE:
                case Win16.MCI_DELETE:
                    return SendGenericMciCommand(uDeviceId, uMessage, dwParam1, dwParam2);

                case Win16.MCI_SET:
                {
                    var st32 = new Win32.MCI_SET_PARAMS();
                    if (dwParam2 != 0)
                    {
                        var st16 = _machine.ReadStruct<Win16.MCI_SET_PARAMS>(dwParam2);
                        st32.dwTimeFormat = st16.dwTimeFormat;
                        st32.dwAudio = st16.dwAudio;
                        if (HWND.Map.IsValid16(st16.dwCallback.Loword()))
                            st32.dwCallback = HWND.Map.To32(st16.dwCallback.Loword());
                    }

                    unsafe
                    {
                        return mciSendCommand(DeviceIdTo32(uDeviceId), Win32.MCI_SET, (IntPtr)dwParam1, (IntPtr)(&st32));
                    }
                }

                case Win16.MCI_GETDEVCAPS:
                {
                    var st32 = new Win32.MCI_GETDEVCAPS_PARAMS();
                    if (dwParam2 != 0)
                    {
                        var st16 = _machine.ReadStruct<Win16.MCI_GETDEVCAPS_PARAMS>(dwParam2);
                        st32.dwItem = st16.dwItem;
                        if (HWND.Map.IsValid16(st16.dwCallback.Loword()))
                            st32.dwCallback = HWND.Map.To32(st16.dwCallback.Loword());
                    }

                    unsafe
                    {
                        uint retv = mciSendCommand(DeviceIdTo32(uDeviceId), Win32.MCI_GETDEVCAPS, (IntPtr)dwParam1, (IntPtr)(&st32));
                        if (dwParam2 != 0)
                        {
                            var st16 = _machine.ReadStruct<Win16.MCI_GETDEVCAPS_PARAMS>(dwParam2);
                            st16.dwReturn = st32.dwReturn;
                            _machine.WriteStruct(dwParam2, st16);
                        }
                        return retv;
                    }
                }

                case Win16.MCI_INFO:
                {
                    if (dwParam2 == 0)
                        return mciSendCommand(DeviceIdTo32(uDeviceId), Win32.MCI_INFO, (IntPtr)dwParam1, IntPtr.Zero);

                    var st16 = _machine.ReadStruct<Win16.MCI_INFO_PARAMS>(dwParam2);
                    var bufferSize = (int)st16.dwRetSize;
                    if (bufferSize <= 0)
                        bufferSize = 256;

                    var nativeBuffer = Marshal.AllocHGlobal(bufferSize * 2); // Unicode chars
                    try
                    {
                        var st32 = new Win32.MCI_INFO_PARAMS();
                        st32.lpstrReturn = nativeBuffer;
                        st32.dwRetSize = (uint)bufferSize;
                        if (HWND.Map.IsValid16(st16.dwCallback.Loword()))
                            st32.dwCallback = HWND.Map.To32(st16.dwCallback.Loword());

                        unsafe
                        {
                            uint retv = mciSendCommand(DeviceIdTo32(uDeviceId), Win32.MCI_INFO, (IntPtr)dwParam1, (IntPtr)(&st32));
                            if (retv == 0 && st16.lpstrReturn != 0)
                            {
                                var result = Marshal.PtrToStringUni(nativeBuffer) ?? "";
                                _machine.WriteString(st16.lpstrReturn, result, (ushort)Math.Min((uint)ushort.MaxValue, st16.dwRetSize));
                            }
                            return retv;
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(nativeBuffer);
                    }
                }

                case Win16.MCI_RECORD:
                {
                    var st32 = new Win32.MCI_RECORD_PARAMS();
                    if (dwParam2 != 0)
                    {
                        var st16 = _machine.ReadStruct<Win16.MCI_RECORD_PARAMS>(dwParam2);
                        st32.dwFrom = st16.dwFrom;
                        st32.dwTo = st16.dwTo;
                        if (HWND.Map.IsValid16(st16.dwCallback.Loword()))
                            st32.dwCallback = HWND.Map.To32(st16.dwCallback.Loword());
                    }

                    unsafe
                    {
                        return mciSendCommand(DeviceIdTo32(uDeviceId), Win32.MCI_RECORD, (IntPtr)dwParam1, (IntPtr)(&st32));
                    }
                }

                case Win16.MCI_SAVE:
                case Win16.MCI_LOAD:
                {
                    // MCI_SAVE and MCI_LOAD use MCI_SAVE_PARAMS / MCI_LOAD_PARAMS which have the same layout:
                    // dwCallback + lpfilename
                    // We pass them through as generic since the filename pointer requires conversion
                    if (dwParam2 == 0)
                        return mciSendCommand(DeviceIdTo32(uDeviceId), uMessage, (IntPtr)dwParam1, IntPtr.Zero);

                    // Read the 16-bit struct: dwCallback (4 bytes) + lpfilename (4 bytes = seg:ofs)
                    var st16Callback = _machine.ReadDWord(dwParam2);
                    var st16Filename = _machine.ReadDWord(dwParam2 + 4);

                    using (var ctx = new TempContext(_machine))
                    {
                        var st32 = new Win32.MCI_GENERIC_PARAMS();
                        if (HWND.Map.IsValid16(st16Callback.Loword()))
                            st32.dwCallback = HWND.Map.To32(st16Callback.Loword());

                        // Allocate a native buffer big enough for callback + filename pointer
                        var bufSize = IntPtr.Size * 2;
                        var nativeBuf = Marshal.AllocHGlobal(bufSize);
                        try
                        {
                            Marshal.WriteIntPtr(nativeBuf, 0, st32.dwCallback);
                            if (st16Filename != 0)
                            {
                                var filename = _machine.ReadString(st16Filename);
                                filename = ResolveMediaFile(filename);
                                Marshal.WriteIntPtr(nativeBuf, IntPtr.Size, ctx.AllocUnmanagedString(filename));
                            }
                            else
                            {
                                Marshal.WriteIntPtr(nativeBuf, IntPtr.Size, IntPtr.Zero);
                            }

                            return mciSendCommand(DeviceIdTo32(uDeviceId), uMessage, (IntPtr)dwParam1, nativeBuf);
                        }
                        finally
                        {
                            Marshal.FreeHGlobal(nativeBuf);
                        }
                    }
                }

                case Win16.MCI_SEEK:
                {
                    var st32 = new Win32.MCI_SEEK_PARAMS();
                    if (dwParam2 != 0)
                    {
                        var st16 = _machine.ReadStruct<Win16.MCI_SEEK_PARAMS>(dwParam2);
                        st32.dwTo = st16.dwTo;
                        if (HWND.Map.IsValid16(st16.dwCallback.Loword()))
                            st32.dwCallback = HWND.Map.To32(st16.dwCallback.Loword());
                    }

                    unsafe
                    {
                        return mciSendCommand(DeviceIdTo32(uDeviceId), Win32.MCI_SEEK, (IntPtr)dwParam1, (IntPtr)(&st32));
                    }
                }

                case Win16.MCI_SYSINFO:
                {
                    if (dwParam2 == 0)
                        return mciSendCommand(DeviceIdTo32(uDeviceId), Win32.MCI_SYSINFO, (IntPtr)dwParam1, IntPtr.Zero);

                    var st16 = _machine.ReadStruct<Win16.MCI_SYSINFO_PARAMS>(dwParam2);
                    var bufferSize = (int)st16.dwRetSize;
                    if (bufferSize <= 0)
                        bufferSize = 256;

                    var nativeBuffer = Marshal.AllocHGlobal(bufferSize * 2); // Unicode chars
                    try
                    {
                        var st32 = new Win32.MCI_SYSINFO_PARAMS();
                        st32.lpstrReturn = nativeBuffer;
                        st32.dwRetSize = (uint)bufferSize;
                        st32.dwNumber = st16.dwNumber;
                        st32.wDeviceType = st16.wDeviceType;
                        if (HWND.Map.IsValid16(st16.dwCallback.Loword()))
                            st32.dwCallback = HWND.Map.To32(st16.dwCallback.Loword());

                        unsafe
                        {
                            uint retv = mciSendCommand(DeviceIdTo32(uDeviceId), Win32.MCI_SYSINFO, (IntPtr)dwParam1, (IntPtr)(&st32));
                            if (retv == 0 && st16.lpstrReturn != 0)
                            {
                                // SYSINFO_QUANTITY returns an integer in the buffer
                                if ((dwParam1 & WinCommon.MCI_SYSINFO_QUANTITY) != 0)
                                {
                                    // The result is stored as a DWORD at the start of the buffer
                                    uint count = (uint)Marshal.ReadInt32(nativeBuffer);
                                    _machine.MemoryBus.WriteDWord(st16.lpstrReturn.Hiword(), st16.lpstrReturn.Loword(), count);
                                }
                                else
                                {
                                    var result = Marshal.PtrToStringUni(nativeBuffer) ?? "";
                                    _machine.WriteString(st16.lpstrReturn, result, (ushort)Math.Min((uint)ushort.MaxValue, st16.dwRetSize));
                                }
                            }
                            return retv;
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(nativeBuffer);
                    }
                }

                case Win16.MCI_WINDOW:
                case Win16.MCI_PUT:
                case Win16.MCI_WHERE:
                case Win16.MCI_UPDATE:
                    // These commands relate to video/animation window management.
                    // Pass through as generic commands — the host MCI driver will
                    // handle them or return an appropriate error for audio-only devices.
                    return SendGenericMciCommand(uDeviceId, uMessage, dwParam1, dwParam2);
            }

            Log.WriteLine("[mciSendCommand] Unsupported MCI command: 0x{0:X4}", uMessage);
            return 263; // MCIERR_UNSUPPORTED_FUNCTION
        }

        [DllImport("winmm.dll", CharSet = CharSet.Unicode, EntryPoint = "mciSendStringW")]
        static extern uint _mciSendString(string lpstrCommand, StringBuilder lpstrReturnString, uint uReturnLength, IntPtr hwndCallback);

        [EntryPoint(0x02be)]
        public uint mciSendString(uint pszCommand, uint pszReturnString, ushort uReturnLength, ushort hwndCallback)
        {
            var command = _machine.ReadString(pszCommand);

            StringBuilder sb = null;
            uint returnLen = uReturnLength;
            if (pszReturnString != 0 && uReturnLength > 0)
            {
                sb = new StringBuilder((int)returnLen);
            }

            IntPtr hWndCb = IntPtr.Zero;
            if (hwndCallback != 0 && HWND.Map.IsValid16(hwndCallback))
                hWndCb = HWND.Map.To32(hwndCallback);

            uint retv = _mciSendString(command, sb, returnLen, hWndCb);

            if (retv == 0 && sb != null && pszReturnString != 0)
            {
                _machine.WriteString(pszReturnString, sb.ToString(), uReturnLength);
            }

            return retv;
        }

        // 02BF - MCIGETDEVICEID - 02BF
        // 02C0 - MCIPARSECOMMAND - 02C0
        // 02C1 - MCILOADCOMMANDRESOURCE - 02C1

        [EntryPoint(0x02c2)]
        public bool mciGetErrorString(uint error, uint buffer, nuint length)
        {
            if (buffer == 0 || length == 0)
                return false;

            uint length32 = length;
            var sb = new StringBuilder((int)length32);
            var retv = _mciGetErrorString(error, sb, length32);
            if (!retv)
                return false;

            _machine.WriteString(buffer, sb.ToString(), (ushort)Math.Min((uint)ushort.MaxValue, length32));
            return true;
        }

        // 02C3 - MCISETDRIVERDATA - 02C3
        // 02C4 - MCIGETDRIVERDATA - 02C4
        // 02C6 - MCIDRIVERYIELD - 02C6
        // 02C7 - MCIDRIVERNOTIFY - 02C7
        // 02C8 - MCIEXECUTE - 02C8
        // 02C9 - MCIFREECOMMANDRESOURCE - 02C9
        // 02CA - MCISETYIELDPROC - 02CA
        // 02CB - MCIGETDEVICEIDFROMELEMENTID - 02CB
        // 02CC - MCIGETYIELDPROC - 02CC
        // 02CD - MCIGETCREATORTASK - 02CD
        // 0320 - MIXERGETNUMDEVS - 0320
        // 0321 - MIXERGETDEVCAPS - 0321
        // 0322 - MIXEROPEN - 0322
        // 0323 - MIXERCLOSE - 0323
        // 0324 - MIXERMESSAGE - 0324
        // 0325 - MIXERGETLINEINFO - 0325
        // 0326 - MIXERGETID - 0326
        // 0327 - MIXERGETLINECONTROLS - 0327
        // 0328 - MIXERGETCONTROLDETAILS - 0328
        // 0329 - MIXERSETCONTROLDETAILS - 0329
        // 0384 - MMTASKCREATE - 0384
        // 0386 - MMTASKBLOCK - 0386
        // 0387 - MMTASKSIGNAL - 0387
        // 0388 - MMGETCURRENTTASK - 0388
        // 0389 - MMTASKYIELD - 0389
        // 044C - DRVOPEN - 044C
        // 044D - DRVCLOSE - 044D
        // 044E - DRVSENDMESSAGE - 044E
        // 044F - DRVGETMODULEHANDLE - 044F
        // 0450 - DRVDEFDRIVERPROC - 0450
        // 04BA - MMIOOPEN - 04BA
        // 04BB - MMIOCLOSE - 04BB
        // 04BC - MMIOREAD - 04BC
        // 04BD - MMIOWRITE - 04BD
        // 04BE - MMIOSEEK - 04BE
        // 04BF - MMIOGETINFO - 04BF
        // 04C0 - MMIOSETINFO - 04C0
        // 04C1 - MMIOSETBUFFER - 04C1
        // 04C2 - MMIOFLUSH - 04C2
        // 04C3 - MMIOADVANCE - 04C3
        // 04C4 - MMIOSTRINGTOFOURCC - 04C4
        // 04C5 - MMIOINSTALLIOPROC - 04C5
        // 04C6 - MMIOSENDMESSAGE - 04C6
        // 04C7 - MMIODESCEND - 04C7
        // 04C8 - MMIOASCEND - 04C8
        // 04C9 - MMIOCREATECHUNK - 04C9
        // 04CA - MMIORENAME - 04CA
    }
}
