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
    class ClassComboBox
    {
        const uint CBS_OWNERDRAWFIXED = 0x0010;
        const uint CBS_OWNERDRAWVARIABLE = 0x0020;
        const uint CBS_HASSTRINGS = 0x0200;

        static bool HasStrings(IntPtr hWnd)
        {
            var style = User._GetWindowLong(hWnd, Win32.GWL_STYLE);
            if ((style & (CBS_OWNERDRAWFIXED | CBS_OWNERDRAWVARIABLE)) == 0)
                return true;
            return (style & CBS_HASSTRINGS)!= 0;
        }

        // CB_GETDROPPEDCONTROLRECT: wParam = 0, lParam = pointer to RECT (output)
        // 16-bit: lParam is far pointer to Win16.RECT
        // 32-bit: lParam is pointer to Win32.RECT
        public class CB_GETDROPPEDCONTROLRECT : Callable
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

        public class CB_ADDSTRING : copy_string
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
    }
}
