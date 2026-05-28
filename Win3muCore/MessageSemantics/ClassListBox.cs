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
    class ClassListBox
    {
        const uint LBS_OWNERDRAWFIXED = 0x0010;
        const uint LBS_OWNERDRAWVARIABLE = 0x0020;
        const uint LBS_HASSTRINGS = 0x0040;

        static bool HasStrings(IntPtr hWnd)
        {
            var style = User._GetWindowLong(hWnd, Win32.GWL_STYLE);
            if ((style & (LBS_OWNERDRAWFIXED | LBS_OWNERDRAWVARIABLE)) == 0)
                return true;
            return (style & LBS_HASSTRINGS)!= 0;
        }

        public class LB_ADDSTRING : copy_string
        {
            public override IntPtr Call16from32(Machine machine, bool hook, bool dlgproc, ref Win32.MSG msg32, ref Win16.MSG msg16, Func<uint> callback)
            {
                if (HasStrings(msg32.hWnd))
                    return base.Call16from32(machine, hook, dlgproc, ref msg32, ref msg16, callback);

                msg16.wParam = msg32.wParam.Loword();
                msg16.lParam = msg32.lParam.Loword();
                return BitUtils.DWordToIntPtr(callback());
            }


            public override uint Call32from16(Machine machine, bool hook, bool dlgproc, ref Win16.MSG msg16, ref Win32.MSG msg32, Func<IntPtr> callback)
            {
                if (HasStrings(msg32.hWnd))
                    return base.Call32from16(machine, hook, dlgproc, ref msg16, ref msg32, callback);

                msg32.wParam = BitUtils.DWordToIntPtr(msg16.wParam);
                msg32.lParam = BitUtils.DWordToIntPtr(msg16.lParam);
                return callback().DWord();
            }
        }

        /*
        public class LB_DIR : copy_string
        {
            public override uint Call32from16(Machine machine, bool hook, bool dlgproc, ref Win16.MSG msg16, ref Win32.MSG msg32, Func<IntPtr> callback)
            {
                if (hook)
                    return 0;
                System.Diagnostics.Debug.Assert(!dlgproc);

                // Get the filespec
                string text = null;
                if (msg32.lParam != IntPtr.Zero)
                {
                    text = Marshal.PtrToStringUni(msg32.lParam);
                }

                return 0;
            }
        }
        */

        // LB_GETITEMRECT: wParam = item index, lParam = pointer to RECT (output)
        // 16-bit: lParam is far pointer to Win16.RECT
        // 32-bit: lParam is pointer to Win32.RECT
        public class LB_GETITEMRECT : Callable
        {
            public override uint Call32from16(Machine machine, bool hook, bool dlgproc, ref Win16.MSG msg16, ref Win32.MSG msg32, Func<IntPtr> callback)
            {
                unsafe
                {
                    var rc32 = new Win32.RECT();
                    msg32.wParam = (IntPtr)msg16.wParam;
                    msg32.lParam = new IntPtr(&rc32);
                    var ret = callback();
                    machine.WriteStruct(msg16.lParam, rc32.Convert());
                    return (uint)ret;
                }
            }

            public override IntPtr Call16from32(Machine machine, bool hook, bool dlgproc, ref Win32.MSG msg32, ref Win16.MSG msg16, Func<uint> callback)
            {
                var ptr = machine.SysAlloc(new Win16.RECT());
                msg16.wParam = msg32.wParam.Loword();
                msg16.lParam = ptr;
                var ret = callback();
                var rc16 = machine.SysReadAndFree<Win16.RECT>(ptr);
                Marshal.StructureToPtr(rc16.Convert(), msg32.lParam, true);
                return (IntPtr)ret;
            }
        }

        // LB_GETSELITEMS: wParam = max items, lParam = pointer to buffer of INT (output)
        // 16-bit: array of 16-bit signed integers
        // 32-bit: array of 32-bit signed integers
        public class LB_GETSELITEMS : Callable
        {
            public override uint Call32from16(Machine machine, bool hook, bool dlgproc, ref Win16.MSG msg16, ref Win32.MSG msg32, Func<IntPtr> callback)
            {
                unsafe
                {
                    int count = msg16.wParam;
                    if (count == 0)
                    {
                        msg32.wParam = IntPtr.Zero;
                        msg32.lParam = IntPtr.Zero;
                        return (uint)callback();
                    }

                    var items32 = new int[count];
                    fixed (int* pItems = items32)
                    {
                        msg32.wParam = (IntPtr)count;
                        msg32.lParam = (IntPtr)pItems;
                        var ret = callback();
                        var retCount = ret.ToInt32();

                        // Write selected item indices back to 16-bit buffer as 16-bit values
                        if (retCount > 0)
                        {
                            for (int i = 0; i < retCount; i++)
                            {
                                machine.WriteWord(msg16.lParam.Hiword(),
                                    (ushort)(msg16.lParam.Loword() + i * 2),
                                    unchecked((ushort)(short)pItems[i]));
                            }
                        }
                        return ret.DWord();
                    }
                }
            }

            public override IntPtr Call16from32(Machine machine, bool hook, bool dlgproc, ref Win32.MSG msg32, ref Win16.MSG msg16, Func<uint> callback)
            {
                int count = (int)msg32.wParam;
                if (count == 0)
                {
                    msg16.wParam = 0;
                    msg16.lParam = 0;
                    return (IntPtr)callback();
                }

                // Allocate 16-bit buffer for indices
                var ptr = machine.SysAlloc((ushort)(count * 2));
                msg16.wParam = (ushort)count;
                msg16.lParam = ptr;
                var ret = callback();

                // Widen 16-bit indices to 32-bit and write to output buffer
                var retCount = (int)ret;
                if (retCount > 0)
                {
                    for (int i = 0; i < retCount; i++)
                    {
                        var val = (short)machine.ReadWord(ptr.Hiword(), (ushort)(ptr.Loword() + i * 2));
                        Marshal.WriteInt32(msg32.lParam, i * 4, val);
                    }
                }

                machine.SysFree(ptr);
                return (IntPtr)ret;
            }
        }

        // LB_SETTABSTOPS: wParam = count, lParam = pointer to array of INT (tab stop positions)
        // Same semantics as EM_SETTABSTOPS
        public class LB_SETTABSTOPS : Callable
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
