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

namespace Win3muCore.MessageSemantics
{
    class ClassEdit
    {
        // EM_GETRECT: wParam = 0, lParam = pointer to RECT (output)
        // 16-bit: lParam is far pointer to Win16.RECT to receive the formatting rectangle
        // 32-bit: lParam is pointer to Win32.RECT to receive the formatting rectangle
        public class EM_GETRECT : Callable
        {
            public override uint Call32from16(Machine machine, bool hook, bool dlgproc, ref Win16.MSG msg16, ref Win32.MSG msg32, Func<IntPtr> callback)
            {
                unsafe
                {
                    var rc32 = new Win32.RECT();
                    msg32.wParam = IntPtr.Zero;
                    msg32.lParam = new IntPtr(&rc32);
                    var ret = callback();
                    machine.WriteStruct(msg16.lParam, rc32.Convert());
                    return (uint)ret;
                }
            }

            public override IntPtr Call16from32(Machine machine, bool hook, bool dlgproc, ref Win32.MSG msg32, ref Win16.MSG msg16, Func<uint> callback)
            {
                var ptr = machine.SysAlloc(new Win16.RECT());
                msg16.wParam = 0;
                msg16.lParam = ptr;
                var ret = callback();
                var rc16 = machine.SysReadAndFree<Win16.RECT>(ptr);
                Marshal.StructureToPtr(rc16.Convert(), msg32.lParam, true);
                return (IntPtr)ret;
            }
        }

        // EM_SETRECT / EM_SETRECTNP: wParam = 0, lParam = pointer to RECT (input)
        // 16-bit: lParam is far pointer to Win16.RECT
        // 32-bit: lParam is pointer to Win32.RECT
        public class EM_SETRECT : Callable
        {
            public override uint Call32from16(Machine machine, bool hook, bool dlgproc, ref Win16.MSG msg16, ref Win32.MSG msg32, Func<IntPtr> callback)
            {
                unsafe
                {
                    if (msg16.lParam == 0)
                    {
                        msg32.wParam = IntPtr.Zero;
                        msg32.lParam = IntPtr.Zero;
                        return (uint)callback();
                    }

                    var rc32 = machine.ReadStruct<Win16.RECT>(msg16.lParam).Convert();
                    msg32.wParam = IntPtr.Zero;
                    msg32.lParam = new IntPtr(&rc32);
                    return (uint)callback();
                }
            }

            public override IntPtr Call16from32(Machine machine, bool hook, bool dlgproc, ref Win32.MSG msg32, ref Win16.MSG msg16, Func<uint> callback)
            {
                if (msg32.lParam == IntPtr.Zero)
                {
                    msg16.wParam = 0;
                    msg16.lParam = 0;
                    return (IntPtr)callback();
                }

                var rc = Marshal.PtrToStructure<Win32.RECT>(msg32.lParam);
                var ptr = machine.SysAlloc(rc.Convert());
                msg16.wParam = 0;
                msg16.lParam = ptr;
                var ret = callback();
                machine.SysFree(ptr);
                return (IntPtr)ret;
            }
        }

        // EM_GETLINE: wParam = line number, lParam = pointer to buffer
        // The first word of the buffer specifies the maximum number of characters to copy.
        // 16-bit: lParam is far pointer to ANSI buffer (first word = max chars)
        // 32-bit: lParam is pointer to Unicode buffer (first word = max chars)
        public class EM_GETLINE : Callable
        {
            public override uint Call32from16(Machine machine, bool hook, bool dlgproc, ref Win16.MSG msg16, ref Win32.MSG msg32, Func<IntPtr> callback)
            {
                unsafe
                {
                    // Read the max char count from the first word of the 16-bit buffer
                    var maxChars = machine.ReadWord(msg16.lParam.Hiword(), msg16.lParam.Loword());

                    var buf = new char[Math.Max((int)maxChars, 1)];
                    fixed (char* psz = buf)
                    {
                        // Store the max char count in the first word of the 32-bit buffer
                        *(ushort*)psz = maxChars;

                        msg32.wParam = (IntPtr)msg16.wParam;
                        msg32.lParam = (IntPtr)psz;
                        var len = callback().ToInt32();

                        if (len > 0)
                        {
                            var str = new String(psz, 0, len);
                            machine.WriteString(msg16.lParam, str, maxChars);
                        }
                        return (uint)len;
                    }
                }
            }

            public override IntPtr Call16from32(Machine machine, bool hook, bool dlgproc, ref Win32.MSG msg32, ref Win16.MSG msg16, Func<uint> callback)
            {
                // Read the max char count from the first word of the 32-bit buffer
                var maxChars = (ushort)Marshal.ReadInt16(msg32.lParam);

                // Allocate 16-bit buffer
                var ptr = machine.SysAlloc((ushort)Math.Max((int)maxChars, 2));
                // Write max chars into first word of 16-bit buffer
                machine.WriteWord(ptr.Hiword(), ptr.Loword(), maxChars);

                msg16.wParam = (ushort)(int)msg32.wParam;
                msg16.lParam = ptr;
                var ret = callback();

                if (ret > 0)
                {
                    // Read the ANSI string from 16-bit buffer and copy to 32-bit buffer
                    var str = machine.ReadString(ptr);
                    var unibytes = Encoding.Unicode.GetBytes(str);
                    Marshal.Copy(unibytes, 0, msg32.lParam, Math.Min(maxChars * 2, unibytes.Length));
                }

                machine.SysFree(ptr);
                return (IntPtr)ret;
            }
        }

        // EM_SETTABSTOPS: wParam = count, lParam = pointer to array of INT (tab stop positions)
        // 16-bit: array of 16-bit signed integers
        // 32-bit: array of 32-bit signed integers
        public class EM_SETTABSTOPS : Callable
        {
            public override uint Call32from16(Machine machine, bool hook, bool dlgproc, ref Win16.MSG msg16, ref Win32.MSG msg32, Func<IntPtr> callback)
            {
                unsafe
                {
                    int count = msg16.wParam;
                    if (count == 0 || msg16.lParam == 0)
                    {
                        msg32.wParam = (IntPtr)count;
                        msg32.lParam = IntPtr.Zero;
                        return (uint)callback();
                    }

                    // Read 16-bit tab stop array and widen to 32-bit
                    var tabs32 = new int[count];
                    for (int i = 0; i < count; i++)
                    {
                        tabs32[i] = (short)machine.ReadWord(msg16.lParam.Hiword(), (ushort)(msg16.lParam.Loword() + i * 2));
                    }

                    fixed (int* pTabs = tabs32)
                    {
                        msg32.wParam = (IntPtr)count;
                        msg32.lParam = (IntPtr)pTabs;
                        return (uint)callback();
                    }
                }
            }

            public override IntPtr Call16from32(Machine machine, bool hook, bool dlgproc, ref Win32.MSG msg32, ref Win16.MSG msg16, Func<uint> callback)
            {
                int count = (int)msg32.wParam;
                if (count == 0 || msg32.lParam == IntPtr.Zero)
                {
                    msg16.wParam = (ushort)count;
                    msg16.lParam = 0;
                    return (IntPtr)callback();
                }

                // Allocate 16-bit array and narrow 32-bit tab stops
                var ptr = machine.SysAlloc((ushort)(count * 2));
                for (int i = 0; i < count; i++)
                {
                    var val = Marshal.ReadInt32(msg32.lParam, i * 4);
                    machine.WriteWord(ptr.Hiword(), (ushort)(ptr.Loword() + i * 2), unchecked((ushort)(short)val));
                }

                msg16.wParam = (ushort)count;
                msg16.lParam = ptr;
                var ret = callback();
                machine.SysFree(ptr);
                return (IntPtr)ret;
            }
        }
    }
}
