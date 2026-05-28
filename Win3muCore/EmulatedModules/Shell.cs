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
    [Module("SHELL", @"C:\WINDOWS\SYSTEM\SHELL.DLL")]
    public class Shell : Module32
    {
        // Ordinal 1 - RegOpenKey
        [EntryPoint(0x0001)]
        public ushort RegOpenKey(uint hKey, string lpSubKey, uint lphkResult)
        {
            // Win3.x registry is minimal; return ERROR_BADKEY for most requests
            Log.WriteLine("Shell.RegOpenKey: hKey=0x{0:X8}, subKey={1}", hKey, lpSubKey);
            return 2; // ERROR_BADKEY
        }

        // Ordinal 2 - RegCreateKey
        [EntryPoint(0x0002)]
        public ushort RegCreateKey(uint hKey, string lpSubKey, uint lphkResult)
        {
            Log.WriteLine("Shell.RegCreateKey: hKey=0x{0:X8}, subKey={1}", hKey, lpSubKey);
            return 2; // ERROR_BADKEY
        }

        // Ordinal 3 - RegCloseKey
        [EntryPoint(0x0003)]
        public ushort RegCloseKey(uint hKey)
        {
            return 0; // ERROR_SUCCESS
        }

        // Ordinal 4 - RegDeleteKey
        [EntryPoint(0x0004)]
        public ushort RegDeleteKey(uint hKey, string lpSubKey)
        {
            Log.WriteLine("Shell.RegDeleteKey: hKey=0x{0:X8}, subKey={1}", hKey, lpSubKey);
            return 2; // ERROR_BADKEY
        }

        // Ordinal 5 - RegSetValue
        [EntryPoint(0x0005)]
        public ushort RegSetValue(uint hKey, string lpSubKey, uint dwType, string lpData, uint cbData)
        {
            Log.WriteLine("Shell.RegSetValue: hKey=0x{0:X8}, subKey={1}, data={2}", hKey, lpSubKey, lpData);
            return 2; // ERROR_BADKEY
        }

        // Ordinal 6 - RegQueryValue
        [EntryPoint(0x0006)]
        public ushort RegQueryValue(uint hKey, string lpSubKey, uint lpValue, uint lpcbValue)
        {
            Log.WriteLine("Shell.RegQueryValue: hKey=0x{0:X8}, subKey={1}", hKey, lpSubKey);
            return 2; // ERROR_BADKEY
        }

        // Ordinal 7 - RegEnumKey
        [EntryPoint(0x0007)]
        public ushort RegEnumKey(uint hKey, uint dwIndex, uint lpName, uint cbName)
        {
            return 259; // ERROR_NO_MORE_ITEMS
        }

        // Ordinal 9 - DragAcceptFiles
        [EntryPoint(0x0009)]
        public void DragAcceptFiles(HWND hWnd, bool fAccept)
        {
            // Stub: Win3.x drag-drop accept registration
            Log.WriteLine("Shell.DragAcceptFiles: hWnd=0x{0:X4}, accept={1}", HWND.To16(hWnd.value), fAccept);
        }

        // Ordinal 11 - DragQueryFile
        [EntryPoint(0x000B)]
        public short DragQueryFile(ushort hDrop, ushort iFile, uint lpszFile, short cb)
        {
            // Stub: no drag-drop files available
            return 0;
        }

        // Ordinal 12 - DragFinish
        [EntryPoint(0x000C)]
        public void DragFinish(ushort hDrop)
        {
            // Stub: free drag-drop data
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr ExtractIcon(IntPtr hInst, string lpszExeFileName, uint nIconIndex);

        // Ordinal 34 - ExtractIcon
        [EntryPoint(0x0022)]
        public ushort ExtractIcon(ushort hInst, string lpszExeFileName, ushort nIconIndex)
        {
            var hostPath = _machine.PathMapper.MapGuestToHost(lpszExeFileName, false);
            var hIcon = ExtractIcon(IntPtr.Zero, hostPath, nIconIndex);
            if (hIcon == IntPtr.Zero || hIcon == (IntPtr)1)
                return (ushort)(int)hIcon;
            return HGDIOBJ.To16(hIcon);
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        static extern int FindExecutableW(string lpFile, string lpDirectory, StringBuilder lpResult);

        // Ordinal 33 - FindExecutable
        [EntryPoint(0x0021)]
        public ushort FindExecutable(string lpFile, string lpDirectory, uint lpResult)
        {
            Log.WriteLine("Shell.FindExecutable: file={0}, dir={1}", lpFile, lpDirectory);
            // Return > 32 for success, <= 32 for error
            return 2; // SE_ERR_FNF - file not found (stub)
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr ShellExecuteW(IntPtr hwnd, string lpOperation, string lpFile, string lpParameters, string lpDirectory, int nShowCmd);

        // Ordinal 20 - ShellExecute
        [EntryPoint(0x0014)]
        public ushort ShellExecute(HWND hWnd, string lpOperation, string lpFile, string lpParameters, string lpDirectory, short nShowCmd)
        {
            Log.WriteLine("Shell.ShellExecute: op={0}, file={1}, params={2}, dir={3}", lpOperation, lpFile, lpParameters, lpDirectory);
            var result = ShellExecuteW(hWnd.value, lpOperation, lpFile, lpParameters, lpDirectory, nShowCmd);
            var retVal = result.ToInt64();
            if (retVal > 32)
                return 33; // Success (instance handle > 32)
            return (ushort)retVal;
        }

        // Ordinal 36 - DoEnvironmentSubst
        [EntryPoint(0x0024)]
        public uint DoEnvironmentSubst(uint lpszSrc, ushort cchSrc)
        {
            // Stub: return original string unchanged
            // HIWORD = original length, LOWORD = cchSrc (unchanged)
            return BitUtils.MakeDWord(cchSrc, cchSrc);
        }

        // Ordinal 37 - RegisterShellHook (Win3.1)
        [EntryPoint(0x0025)]
        public bool RegisterShellHook(HWND hWnd, bool install)
        {
            Log.WriteLine("Shell.RegisterShellHook: hWnd=0x{0:X4}, install={1}", HWND.To16(hWnd.value), install);
            return false;
        }

        // Ordinal 39 - AboutDlgProc (internal, usually not called directly)

        // Ordinal 22 - ShellAbout  (alias for AboutBox)
        [DllImport("shell32.dll", CharSet = CharSet.Unicode, EntryPoint = "ShellAboutW")]
        static extern int ShellAbout(IntPtr hWnd, string szApp, string szOtherStuff, IntPtr hIcon);

        [EntryPoint(0x0016)]
        public short ShellAbout(HWND hWnd, string szApp, string szOtherStuff, ushort hIcon)
        {
            return (short)ShellAbout(hWnd.value, szApp, szOtherStuff, HGDIOBJ.To32(hIcon).value);
        }
    }
}
