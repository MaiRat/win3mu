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
    [Module("GDI", @"C:\WINDOWS\SYSTEM\GDI.EXE")]
    public class Gdi : Module32
    {
        delegate int ABORTPROC(IntPtr hdc, int code);
        delegate int MFENUMPROC(IntPtr hdc, IntPtr lpHTable, IntPtr lpMFR, int nObj, IntPtr lpData);

        readonly Dictionary<IntPtr, ABORTPROC> _abortProcs = new Dictionary<IntPtr, ABORTPROC>();

        [EntryPoint(0x0001)]
        [DllImport("gdi32.dll")]
        public static extern uint SetBkColor(HDC hDC, uint colorref);

        [EntryPoint(0x0002)]
        [DllImport("gdi32.dll")]
        public static extern nint SetBkMode(HDC hDC, nint mode);

        [EntryPoint(0x0003)]
        [DllImport("gdi32.dll")]
        public static extern nint SetMapMode(HDC hDC, nint mode);

        [EntryPoint(0x0004)]
        [DllImport("gdi32.dll")]
        public static extern nint SetROP2(HDC hDC, nint mode);


        // 0005 - SETRELABS

        [EntryPoint(0x0006)]
        [DllImport("gdi32.dll")]
        public static extern nint SetPolyFillMode(HDC hDC, nint mode);

        [EntryPoint(0x0007)]
        [DllImport("gdi32.dll")]
        public static extern nint SetStretchBltMode(HDC hDC, nint mode);

        [EntryPoint(0x0008)]
        [DllImport("gdi32.dll")]
        public static extern nint SetTextCharacterExtra(HDC hDC, nint extra);

        [EntryPoint(0x0009)]
        [DllImport("gdi32.dll")]
        public static extern uint SetTextColor(HDC hDC, uint color);

        [EntryPoint(0x000A)]
        [DllImport("gdi32.dll")]
        public static extern bool SetTextJustification(HDC hDC, nint extra, nint count);

        [DllImport("gdi32.dll")]
        public static extern bool SetWindowOrgEx(HDC hDC, int x, int y, out Win32.POINT point);

        [EntryPoint(0x000B)]
        public uint SetWindowOrg(HDC hDC, short x, short y)
        {
            Win32.POINT point;
            SetWindowOrgEx(hDC, x, y, out point);

            return point.ToDWord();
        }


        [DllImport("gdi32.dll")]
        public static extern bool SetWindowExtEx(HDC hDC, int x, int y, out Win32.SIZE size);

        [EntryPoint(0x000C)]
        public uint SetWindowExt(HDC hDC, short x, short y)
        {
            Win32.SIZE size;
            SetWindowExtEx(hDC, x, y, out size);

            return BitUtils.MakeDWord((ushort)(short)size.Width, (ushort)(short)size.Height);
        }

        [DllImport("gdi32.dll")]
        public static extern bool SetViewportOrgEx(HDC hDC, int x, int y, out Win32.POINT pptOld);

        [EntryPoint(0x000d)]
        public uint SetViewportOrg(HDC hDC, nint x, nint y)
        {
            Win32.POINT old32;
            if (!SetViewportOrgEx(hDC, x, y, out old32))
            {
                return 0;
            }
            return old32.ToDWord();
        }

        [DllImport("gdi32.dll")]
        public static extern bool SetViewportExtEx(HDC hDC, int x, int y, out Win32.SIZE size);

        [EntryPoint(0x000e)]
        public uint SetViewportExt(HDC hDC, short x, short y)
        {
            Win32.SIZE size;
            SetViewportExtEx(hDC, x, y, out size);

            return BitUtils.MakeDWord((ushort)(short)size.Width, (ushort)(short)size.Height);
        }


        [DllImport("gdi32.dll")]
        public static extern bool OffsetWindowOrgEx(HDC hDC, int x, int y, out Win32.POINT pptOld);

        [EntryPoint(0x000F)]
        public uint OffsetWindowOrg(HDC hDC, short x, short y)
        {
            Win32.POINT old32;
            if (!OffsetWindowOrgEx(hDC, x, y, out old32))
                return 0;

            return old32.ToDWord();
        }

        [DllImport("gdi32.dll")]
        public static extern bool ScaleWindowExtEx(HDC hDC, int xn, int xd, int yn, int yd, out Win32.SIZE size);

        [EntryPoint(0x0010)]
        public uint ScaleWindowExt(HDC hDC, short xNum, short xDenom, short yNum, short yDenom)
        {
            Win32.SIZE size;
            if (!ScaleWindowExtEx(hDC, xNum, xDenom, yNum, yDenom, out size))
                return 0;

            return BitUtils.MakeDWord((ushort)(short)size.Width, (ushort)(short)size.Height);
        }

        [DllImport("gdi32.dll")]
        public static extern bool OffsetViewportOrgEx(HDC hDC, int x, int y, out Win32.POINT pptOld);

        [EntryPoint(0x0011)]
        public uint OffsetViewportOrg(HDC hDC, short x, short y)
        {
            Win32.POINT old32;
            if (!OffsetViewportOrgEx(hDC, x, y, out old32))
                return 0;

            return old32.ToDWord();
        }

        [DllImport("gdi32.dll")]
        public static extern bool ScaleViewportExtEx(HDC hDC, int xn, int xd, int yn, int yd, out Win32.SIZE size);

        [EntryPoint(0x0012)]
        public uint ScaleViewportExt(HDC hDC, short xNum, short xDenom, short yNum, short yDenom)
        {
            Win32.SIZE size;
            if (!ScaleViewportExtEx(hDC, xNum, xDenom, yNum, yDenom, out size))
                return 0;

            return BitUtils.MakeDWord((ushort)(short)size.Width, (ushort)(short)size.Height);
        }

        [EntryPoint(0x0013)]
        [DllImport("gdi32.dll")]
        public static extern bool LineTo(HDC hDC, nint x, nint y);

        [DllImport("gdi32.dll")]
        public static extern bool MoveToEx(HDC hDC, int x, int y, IntPtr pptOld);

        [EntryPoint(0x0014)]
        public bool MoveTo(HDC hDC, nint x, nint y)
        {
            return MoveToEx(hDC, x, y, IntPtr.Zero);
        }

        [EntryPoint(0x0015)]
        [DllImport("gdi32.dll")]
        public static extern bool ExcludeClipRect(HDC hDC, nint left, nint top, nint right, nint bottom);

        [EntryPoint(0x0016)]
        [DllImport("gdi32.dll")]
        public static extern bool IntersectClipRect(HDC hDC, nint left, nint top, nint right, nint bottom);

        [EntryPoint(0x0017)]
        [DllImport("gdi32.dll")]
        public static extern bool Arc(HDC hDC, nint left, nint top, nint right, nint bottom, 
                                                    nint xstart, nint ystart, nint xend, nint yend);

        [EntryPoint(0x0018)]
        [DllImport("gdi32.dll")]
        public static extern bool Ellipse(HDC hDC, nint left, nint top, nint right, nint bottom);

        [EntryPoint(0x0019)]
        [DllImport("gdi32.dll")]
        public static extern bool FloodFill(HDC hDC, nint x, nint y, uint colorRef);

        [EntryPoint(0x001A)]
        [DllImport("gdi32.dll", EntryPoint = "Pie")]
        public static extern bool Pie(HDC hDC, nint left, nint top, nint right, nint bottom,
                                                    nint xr1, nint yr1, nint xr2, nint yr2);

        [EntryPoint(0x001b)]
        [DllImport("gdi32.dll")]
        public static extern bool Rectangle(HDC hDC, nint l, nint t, nint r, nint b);

        [EntryPoint(0x001c)]
        [DllImport("gdi32.dll")]
        public static extern bool RoundRect(HDC hDC, nint l, nint t, nint r, nint b, nint r1, nint r2);

        [EntryPoint(0x001d)]
        [DllImport("gdi32.dll")]
        public static extern bool PatBlt(HDC hDC, nint l, nint t, nint r, nint b, uint rop);

        [EntryPoint(0x001e)]
        [DllImport("gdi32.dll")]
        public static extern nint SaveDC(HDC hDC);

        [EntryPoint(0x001f)]
        [DllImport("gdi32.dll")]
        public static extern uint SetPixel(HDC hDC, nint x, nint y, uint color);

        [EntryPoint(0x0020)]
        [DllImport("gdi32.dll")]
        public static extern nint OffsetClipRgn(HDC hDC, nint x, nint y);

        [DllImport("gdi32.dll")]
        public static extern bool TextOut(HDC hDC, int x, int y, string str, int length);

        [DllImport("gdi32.dll")]
        public static extern HGDIOBJ GetCurrentObject(HDC hDC, int objType);

        [DllImport("gdi32.dll")]
        public static extern bool GdiFlush();

        [EntryPoint(0x0021)]
        public bool TextOut(HDC hDC, nint x, nint y, uint pszString, nint cbString)
        {
            var str = _machine.GlobalHeap.ReadCharacters(pszString, cbString);

            bool retv = TextOut(hDC, x, y, str, cbString);

            // This is needed to get stupid Wordzap rendering correctly (text delays appearance with out it because it's
            // doesn't release the DC before spinning a crazy busy loop)
            GdiFlush();

            return retv;

        }


        [EntryPoint(0x0022)]
        [DllImport("gdi32.dll")]
        public static extern bool BitBlt(HDC hDC, nint x, nint y, nint width, nint height, HDC hdcSrc, nint x2, nint y2, uint rop);

        [EntryPoint(0x0023)]
        [DllImport("gdi32.dll")]
        public static extern bool StretchBlt(HDC hDC, nint x, nint y, nint width, nint height, HDC hdcSrc, nint x2, nint y2, nint width2, nint height2, uint rop);

        [DllImport("gdi32.dll")]
        static extern bool Polygon(HDC hdc, Win32.POINT[] lpPoints, int nCount);

        [EntryPoint(0x0024)]
        public bool Polygon(HDC hDC, uint ppts, nint nCount)
        {
            var pts = new Win32.POINT[nCount];
            for (int i = 0; i < nCount; i++)
            {
                pts[i] = _machine.ReadStruct<Win16.POINT>((uint)(ppts + i * Marshal.SizeOf<Win16.POINT>())).Convert();
            }

            return Polygon(hDC, pts, nCount);
        }

        [DllImport("gdi32.dll")]
        static extern bool Polyline(HDC hdc, Win32.POINT[] lpPoints, int nCount);

        [EntryPoint(0x0025)]
        public bool Polyline(HDC hDC, uint ppts, nint nCount)
        {
            var pts = new Win32.POINT[nCount];
            for (int i = 0; i < nCount; i++)
            {
                pts[i] = _machine.ReadStruct<Win16.POINT>((uint)(ppts + i * Marshal.SizeOf<Win16.POINT>())).Convert();
            }

            return Polyline(hDC, pts, nCount);
        }

        [DllImport("gdi32.dll", EntryPoint = "Escape")]
        static extern int _Escape(IntPtr hDC, int escape, int cbInput, IntPtr lpInData, IntPtr lpOutData);

        [EntryPoint(0x0026)]
        public int Escape(HDC hDC, short escape, short cbInput, uint lpInData, uint lpOutData)
        {
            using (var hpIn = _machine.GlobalHeap.GetHeapPointer(lpInData, false))
            using (var hpOut = _machine.GlobalHeap.GetHeapPointer(lpOutData, true))
            {
                return _Escape(hDC.value, escape, cbInput, hpIn, hpOut);
            }
        }

        [EntryPoint(0x0027)]
        [DllImport("gdi32.dll")]
        public static extern bool RestoreDC(HDC hDC, nint nSavedDC);

        [EntryPoint(0x0028)]                   
        [DllImport("gdi32.dll")]
        public static extern bool FillRgn(HDC hDC, HGDIOBJ hRgn, HGDIOBJ hBrush);

        [EntryPoint(0x0029)]
        [DllImport("gdi32.dll")]
        public static extern bool FrameRgn(HDC hDC, HGDIOBJ hRgn, nint w, nint h);

        [EntryPoint(0x002a)]
        [DllImport("gdi32.dll")]
        public static extern bool InvertRgn(HDC hDC, HGDIOBJ hRgn);

        [EntryPoint(0x002b)]
        [DllImport("gdi32.dll")]
        public static extern bool PaintRgn(HDC hDC, HGDIOBJ hRgn);

        [EntryPoint(0x002c)]
        [DllImport("gdi32.dll")]
        public static extern nint SelectClipRgn(HDC hDC, HGDIOBJ hRgn);

        [EntryPoint(0x002d)]
        [DllImport("gdi32.dll", ExactSpelling = true, PreserveSig = true, SetLastError = true)]
        public static extern HGDIOBJ SelectObject(HDC hdc, HGDIOBJ hgdiobj);

        [EntryPoint(0x002f)]
        [DllImport("gdi32.dll")]
        public static extern nint CombineRgn(HGDIOBJ hrgnDest, HGDIOBJ hrgnSrc1, HGDIOBJ hrgnSrc2, nint combineMode);

        [DllImport("gdi32.dll")]
        static extern IntPtr CreateBitmap(int nWidth, int nHeight, uint cPlanes, uint cBitsPerPel, IntPtr lpvBits);

        [EntryPoint(0x0030)]
        public HGDIOBJ CreateBitmap(nint width, nint height, nuint planes, nuint bitcount, uint ptrBits)
        {
            using (var hp = _machine.GlobalHeap.GetHeapPointer(ptrBits, false))
            {
                return CreateBitmap(width, height, planes, bitcount, hp);
            }
        }

        [DllImport("gdi32.dll")]
        static extern HGDIOBJ CreateBitmapIndirect(ref Win32.BITMAP bitmap);

        [EntryPoint(0x0031)]
        public HGDIOBJ CreateBitmapIndirect(ref Win16.BITMAP bitmap)
        {
            var bitmap32 = Win32.BITMAP.To32(bitmap);
            using (var hpBits = _machine.GlobalHeap.GetHeapPointer(bitmap.bmBits, false))
            {
                bitmap32.bmBits = hpBits;
                return CreateBitmapIndirect(ref bitmap32);
            }
        }

        [EntryPoint(0x0032)]
        public HGDIOBJ CreateBrushIndirect(ref Win16.LOGBRUSH brush)
        {
            switch (brush.style)
            {
                case Win16.BS_SOLID:
                    return CreateSolidBrush(brush.color);

                case Win16.BS_NULL:
                    return GetStockObject(5); // NULL_BRUSH / HOLLOW_BRUSH

                case Win16.BS_PATTERN:
                    return CreatePatternBrush(HGDIOBJ.To32((ushort)brush.hatch));

                case Win16.BS_HATCHED:
                    return CreateHatchBrush(brush.style, brush.color);
            }

            Log.WriteLine("CreateBrushIndirect: unsupported brush style {0}, falling back to solid", brush.style);
            return CreateSolidBrush(brush.color);
        }                                 

        [EntryPoint(0x0033)]
        [DllImport("gdi32.dll")]
        public static extern HGDIOBJ CreateCompatibleBitmap(HDC hDC, nint width, nint height);

        [EntryPoint(0x0034)]
        [DllImport("gdi32.dll")]
        public static extern HDC CreateCompatibleDC(HDC hdc);

        [EntryPoint(0x0035)]
        [DllImport("gdi32.dll")]
        public static extern HDC CreateDC(string lpszDriver, string lpszDevice, string lpszOutput, [MustBeNull] IntPtr lpdvmInit);

        [EntryPoint(0x0036)]
        [DllImport("gdi32.dll")]
        public static extern HGDIOBJ CreateEllipticRgn(nint left, nint top, nint right, nint bottom);

        [DllImport("gdi32.dll")]
        static extern HGDIOBJ CreateEllipticRgnIndirect(ref Win32.RECT rc);

        [EntryPoint(0x0037)]
        public HGDIOBJ CreateEllipticRgnIndirect(ref Win16.RECT rc)
        {
            var rc32 = rc.Convert();
            return CreateEllipticRgnIndirect(ref rc32);
        }

        [DllImport("gdi32.dll", EntryPoint = "CreateFontW", CharSet = CharSet.Unicode)]
        public static extern HGDIOBJ _CreateFont(int nHeight, int nWidth, int nEscapement, int nOrientation, int fnWeight,
            uint fdwItalic, uint fdwUnderline, uint fdwStrikeOut, uint fdwCharSet, uint fdwOutputPrecision, uint fdwClipPrecision, uint fdwQuality,
            uint fdwPitchAndFamily, string faceName);

        [EntryPoint(0x0038)]
        public HGDIOBJ CreateFont(nint nHeight, nint nWidth, nint nEscapement, nint nOrientation, nint fnWeight,
            byte fdwItalic, byte fdwUnderline, byte fdwStrikeOut, byte fdwCharSet, byte fdwOutputPrecision, byte fdwClipPrecision, byte fdwQuality,
            byte fdwPitchAndFamily, string faceName)
        {
            return _CreateFont(nHeight, nWidth, nEscapement, nOrientation, fnWeight,
                fdwItalic, fdwUnderline, fdwStrikeOut, fdwCharSet, fdwOutputPrecision, fdwClipPrecision, fdwQuality, fdwPitchAndFamily,
                faceName);
        }

        [EntryPoint(0x0039)]
        [DllImport("gdi32.dll")]
        public static extern HGDIOBJ CreateFontIndirect([In] ref Win32.LOGFONT lf);

        [EntryPoint(0x003A)]
        [DllImport("gdi32.dll")]
        public static extern HGDIOBJ CreateHatchBrush(nint style, uint colorRef);

        [EntryPoint(0x003C)]
        [DllImport("gdi32.dll")]
        public static extern HGDIOBJ CreatePatternBrush(HGDIOBJ hBrush);

        [EntryPoint(0x003d)]
        [DllImport("gdi32.dll")]
        public static extern HGDIOBJ CreatePen(nint penStyle, nint width, uint colorRef);

        [EntryPoint(0x003e)]
        [DllImport("gdi32.dll")]
        public static extern HGDIOBJ CreatePenIndirect(ref Win32.LOGPEN lp);

        [DllImport("gdi32.dll")]
        static extern HGDIOBJ CreatePolygonRgn(Win32.POINT[] lppt, int cPoints, int fnPolyFillMode);

        [EntryPoint(0x003F)]
        public HGDIOBJ CreatePolygonRgn(uint ppts, nint count, nint fillMode)
        {
            var pts = new Win32.POINT[count];
            for (int i = 0; i < count; i++)
            {
                pts[i] = _machine.ReadStruct<Win16.POINT>((uint)(ppts + i * Marshal.SizeOf<Win16.POINT>())).Convert();
            }

            return CreatePolygonRgn(pts, count, fillMode);
        }

        [EntryPoint(0x0040)]
        [DllImport("gdi32.dll")]
        public static extern HGDIOBJ CreateRectRgn(nint left, nint top, nint right, nint bottom);

        [DllImport("gdi32.dll")]
        static extern HGDIOBJ CreateRectRgnIndirect(ref Win32.RECT rc);

        [EntryPoint(0x0041)]
        public HGDIOBJ CreateRectRgnIndirect(ref Win16.RECT rc)
        {
            var rc32 = rc.Convert();
            return CreateRectRgnIndirect(ref rc32);
        }

        [EntryPoint(0x0042)]
        [DllImport("gdi32.dll")]
        public static extern HGDIOBJ CreateSolidBrush(uint colorRef);

        [DllImport("gdi32.dll")]
        static extern bool DPtoLP(IntPtr hdc, [In, Out] Win32.POINT[] lpPoints, int nCount);

        [EntryPoint(0x0043)]
        public bool DPtoLP(HDC hDC, uint ppts, nint nCount)
        {
            // Convert to 32
            var pts = new Win32.POINT[nCount];
            for (int i = 0; i < nCount; i++)
            {
                pts[i] = _machine.ReadStruct<Win16.POINT>((uint)(ppts + i * Marshal.SizeOf<Win16.POINT>())).Convert();
            }

            // Calculate
            bool val = DPtoLP(hDC.value, pts, nCount);

            // And back
            for (int i=0; i< nCount; i++)
            {
                _machine.WriteStruct((uint)(ppts + i * Marshal.SizeOf<Win16.POINT>()), pts[i].Convert());
            }

            return val;
        }


        [EntryPoint(0x0044)]
        [DllImport("gdi32.dll")]
        public static extern bool DeleteDC([Destroyed] HDC hdc);

        [EntryPoint(0x0045)]
        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject([Destroyed] HGDIOBJ hGdiObj);

        public delegate int EnumFontsDelegate(IntPtr pLogFont, IntPtr pTextMetric, uint dwType, IntPtr lParam);
        public delegate int EnumObjectsDelegate(IntPtr pLogObject, IntPtr lParam);

        [DllImport("gdi32.dll", EntryPoint = "EnumFontsW", CharSet = CharSet.Unicode)]
        public static extern int EnumFonts(IntPtr hDC, string faceName, EnumFontsDelegate enumProc, IntPtr lParam);

        [DllImport("gdi32.dll")]
        public static extern int EnumObjects(HDC hDC, int nObjectType, EnumObjectsDelegate enumProc, IntPtr lParam);

        [EntryPoint(0x0046)]
        public nint EnumFonts(HDC hDC, string name, uint enumProc, uint lParam)
        {
            return EnumFonts(hDC.value, name, (pLogFont, pTextMetric, dwType, lp) =>
            {
                var lf = Marshal.PtrToStructure<Win32.LOGFONT>(pLogFont);
                var tm = Marshal.PtrToStructure<Win32.TEXTMETRIC>(pTextMetric);

                var plf16 = _machine.SysAlloc(Win32.LOGFONT.To16(lf));
                var ptm16 = _machine.SysAlloc(Win32.TEXTMETRIC.To16(tm));

                _machine.PushDWord(plf16);
                _machine.PushDWord(ptm16);
                _machine.PushWord(dwType.Loword());
                _machine.PushDWord(lParam);

                _machine.CallVM(enumProc, "EnumFontsProc");

                _machine.SysFree(plf16);
                _machine.SysFree(ptm16);

                return _machine.ax;

            }, IntPtr.Zero);
        }

        [EntryPoint(0x0047)]
        public nint EnumObjects(HDC hDC, nint nObjectType, uint enumProc, uint lParam)
        {
            return EnumObjects(hDC, nObjectType, (pLogObject, lp) =>
            {
                uint pObject16;
                switch ((uint)(int)nObjectType)
                {
                    case Win32.OBJ_PEN:
                        pObject16 = _machine.SysAlloc(Win32.LOGPEN.To16(Marshal.PtrToStructure<Win32.LOGPEN>(pLogObject)));
                        break;

                    case Win32.OBJ_BRUSH:
                        pObject16 = _machine.SysAlloc(Win32.LOGBRUSH.To16(Marshal.PtrToStructure<Win32.LOGBRUSH>(pLogObject)));
                        break;

                    default:
                        Log.WriteLine("EnumObjects: unsupported object type {0}", nObjectType);
                        return 0;
                }

                _machine.PushDWord(pObject16);
                _machine.PushDWord(lParam);
                _machine.CallVM(enumProc, "EnumObjectsProc");
                _machine.SysFree(pObject16);
                return _machine.ax;
            }, IntPtr.Zero);
        }

        [EntryPoint(0x0048)]
        [DllImport("gdi32.dll")]
        public static extern bool EqualRgn(HGDIOBJ hRgn1, HGDIOBJ hRgn2);

        [EntryPoint(0x0049)]
        public bool ExcludeVisRect(HDC hDC, nint left, nint top, nint right, nint bottom)
        {
            // Win16 visible-rectangle exclusion maps closely enough to clipping exclusion for this emulation layer.
            return ExcludeClipRect(hDC, left, top, right, bottom);
        }

        [DllImport("gdi32.dll")]
        public static extern int GetBitmapBits(IntPtr hBitmap, int cbBuffer, IntPtr pBuffer);

        [EntryPoint(0x004a)]
        public int GetBitmapBits(HGDIOBJ hBitmap, int cbBuffer, uint pBuffer)
        {
            using (var hp = _machine.GlobalHeap.GetHeapPointer(pBuffer, true))
            {
                return GetBitmapBits(hBitmap.value, cbBuffer, hp);
            }
        }

        [EntryPoint(0x004b)]
        [DllImport("gdi32.dll")]
        public static extern uint GetBkColor(HDC hDC);

        [EntryPoint(0x004c)]
        [DllImport("gdi32.dll")]
        public static extern nint GetBkMode(HDC hDC);

        [EntryPoint(0x004d)]
        [DllImport("gdi32.dll")]
        public static extern nint GetClipBox(HDC hDC, out Win32.RECT rc);

        [DllImport("gdi32.dll")]
        public static extern bool GetCurrentPositionEx(HDC hdc, out Win32.POINT lpPoint);

        [EntryPoint(0x004e)]
        public uint GetCurrentPosition(HDC hDC)
        {
            Win32.POINT pt;
            GetCurrentPositionEx(hDC, out pt);
            return pt.ToDWord();
        }

        [DllImport("gdi32.dll")]
        public static extern bool GetDCOrgEx(HDC hDC, out Win32.POINT pptOld);

        [EntryPoint(0x004f)]
        public uint GetDCOrg(HDC hDC)
        {
            Win32.POINT pt;
            GetDCOrgEx(hDC, out pt);
            return pt.ToDWord();
        }

        [DllImport("gdi32.dll", EntryPoint = "GetDeviceCaps")]
        public static extern nint _GetDeviceCaps(HDC hDC, nint cap);

        [EntryPoint(0x0050)]
        public nint GetDeviceCaps(ushort hDC, nint cap)
        {
            // Tested on WinXP with 16, 24 and 32-bit color all return
            // 2048 for num colors on 16-bit windows
            // Assumes hDC is a screen DC
            // Also fixes tetris asking for NumColors on a released DC
            if (cap == Win16.NUMCOLORS && (!HDC.Map.IsValid16(hDC) || _GetDeviceCaps(HDC.To32(hDC), Win16.TECHNOLOGY) == Win16.DT_RASDISPLAY))
            {
                return 2048;
            }

            return _GetDeviceCaps(HDC.To32(hDC), cap);
        }

        [EntryPoint(0x0051)]
        [DllImport("gdi32.dll")]
        public static extern nint GetMapMode(HDC hDC);

        [DllImport("gdi32.dll")]
        static extern uint GetObjectType(HGDIOBJ h);

        [DllImport("gdi32.dll")]
        static extern int GetObject(HGDIOBJ hgdiobj, int cbBuffer, IntPtr lpvObject);

        [DllImport("gdi32.dll", EntryPoint = "GetObjectW", CharSet = CharSet.Unicode)]
        static extern int GetObject(HGDIOBJ hgdiobj, int cbBuffer, out Win32.LOGFONT lpvObject);

        [EntryPoint(0x0052)]
        public short GetObject(HGDIOBJ hgdiobj, short cbBuffer, uint ptr)
        {
            unsafe
            {
                var objectType = GetObjectType(hgdiobj);
                switch (objectType)
                {
                    case Win32.OBJ_PEN:
                    {
                        if (ptr == 0)
                            return (short)Marshal.SizeOf<Win16.LOGPEN>();

                        // Check if buffer big enough
                        if (cbBuffer < Marshal.SizeOf<Win16.LOGPEN>())
                            return 0;

                        // Get it
                        var lp32 = new Win32.LOGPEN();
                        var plp32 = &lp32;
                        var size = GetObject(hgdiobj, Marshal.SizeOf<Win32.LOGPEN>(), (IntPtr)plp32);

                        // Convert and write it back
                        _machine.WriteStruct(ptr, Win32.LOGPEN.To16(lp32));

                        return (short)Marshal.SizeOf<Win16.LOGPEN>();
                    }

                    case Win32.OBJ_BRUSH:
                    {
                        if (ptr == 0)
                            return (short)Marshal.SizeOf<Win16.LOGBRUSH>();

                        if (cbBuffer < Marshal.SizeOf<Win16.LOGBRUSH>())
                            return 0;

                        var lb32 = new Win32.LOGBRUSH();
                        var plb32 = &lb32;
                        var size = GetObject(hgdiobj, Marshal.SizeOf<Win32.LOGBRUSH>(), (IntPtr)plb32);

                        _machine.WriteStruct(ptr, Win32.LOGBRUSH.To16(lb32));
                        return (short)Marshal.SizeOf<Win16.LOGBRUSH>();
                    }

                    case Win32.OBJ_BITMAP:
                    {
                        // Just asking for size?
                        if (ptr == 0)
                            return (short)Marshal.SizeOf<Win16.BITMAP>();

                        // Check if buffer big enough
                        if (cbBuffer < Marshal.SizeOf<Win16.BITMAP>())
                            return 0;

                        // Get it
                        var bmp32 = new Win32.BITMAP();
                        var pbmp32 = &bmp32;
                        var size = GetObject(hgdiobj, Marshal.SizeOf<Win32.BITMAP>(), (IntPtr)pbmp32);

                        // Convert and write it back
                        _machine.WriteStruct(ptr, Win32.BITMAP.To16(bmp32));

                        // Return size
                        return (short)Marshal.SizeOf<Win16.BITMAP>();
                    }

                    case Win32.OBJ_FONT:
                    {
                        // Just asking for size?
                        if (ptr == 0)
                            return (short)Marshal.SizeOf<Win16.LOGFONT>();

                        // Check if buffer big enough
                        if (cbBuffer < Marshal.SizeOf<Win16.LOGFONT>())
                            return 0;

                        // Get it
                        var lf32= new Win32.LOGFONT();
                        var size = GetObject(hgdiobj, Marshal.SizeOf<Win32.LOGFONT>(), out lf32);

                        // Convert and write it back
                        _machine.WriteStruct(ptr, Win32.LOGFONT.To16(lf32));

                        // Return size
                        return (short)Marshal.SizeOf<Win16.LOGFONT>();
                    }

                    default:
                        Log.WriteLine("GetObject: unsupported object type {0}", objectType);
                        return 0;
                }
            }
        }

        [EntryPoint(0x0053)]
        [DllImport("gdi32.dll")]
        public static extern uint GetPixel(HDC hDC, nint x, nint y);

        [EntryPoint(0x0054)]
        [DllImport("gdi32.dll")]
        public static extern nint GetPolyFillMode(HDC hDC);

        [EntryPoint(0x0055)]
        [DllImport("gdi32.dll")]
        public static extern nint GetROP2(HDC hDC);

        // 0056 - GETRELABS

        [EntryPoint(0x0057)]
        [DllImport("gdi32.dll")]
        public static extern HGDIOBJ GetStockObject(nint Object);

        [EntryPoint(0x0058)]
        [DllImport("gdi32.dll")]
        public static extern nint GetStretchBltMode(HDC hDC);

        [EntryPoint(0x0059)]
        [DllImport("gdi32.dll")]
        public static extern nint GetTextCharacterExtra(HDC hDC);

        [EntryPoint(0x005A)]
        [DllImport("gdi32.dll")]
        public static extern uint GetTextColor(HDC hDC);

        [DllImport("gdi32.dll")]
        static extern bool GetTextExtentPoint(HDC hdc, string lpString, int cbString, out Win32.SIZE lpSize);

        [EntryPoint(0x005b)]
        public uint GetTextExtent(HDC hDC, uint pszString, short cbString)
        {
            var str = _machine.GlobalHeap.ReadCharacters(pszString, cbString);

            Win32.SIZE size;
            if (!GetTextExtentPoint(hDC, str, cbString, out size))
                return 0xFFFFFFFF;

            return BitUtils.MakeDWord((ushort)(short)size.Width, (ushort)(short)size.Height);
        }                          

        [DllImport("gdi32.dll", EntryPoint = "GetTextFaceW", CharSet = CharSet.Unicode)]
        static extern int _GetTextFace(HDC hDC, int cch, StringBuilder lpFaceName);

        [DllImport("gdi32.dll", EntryPoint = "GetTextFaceW", CharSet = CharSet.Unicode)]
        static extern int _GetTextFaceLength(HDC hDC, int cch, IntPtr lpFaceName);

        [EntryPoint(0x005C)]
        public short GetTextFace(HDC hDC, short cch, uint lpFaceName)
        {
            if (cch <= 0 || lpFaceName == 0)
                return (short)_GetTextFaceLength(hDC, 0, IntPtr.Zero);

            var faceName = new StringBuilder(cch);
            int copied = _GetTextFace(hDC, cch, faceName);
            if (copied > 0)
                _machine.WriteString(lpFaceName, faceName.ToString(), (ushort)cch);
            return (short)copied;
        }

        [EntryPoint(0x005d)]
        [DllImport("gdi32.dll", CharSet = CharSet.Auto)]
        public static extern bool GetTextMetrics(HDC hdc, out Win32.TEXTMETRIC lptm);


        [DllImport("gdi32.dll")]
        public static extern bool GetViewportExtEx(HDC hDC, out Win32.SIZE size);

        [EntryPoint(0x005E)]
        public uint GetViewportExt(HDC hDC)
        {
            Win32.SIZE size;
            if (!GetViewportExtEx(hDC, out size))
                return 0;

            return BitUtils.MakeDWord((ushort)(short)size.Width, (ushort)(short)size.Height);
        }

        [DllImport("gdi32.dll")]
        public static extern bool GetViewportOrgEx(HDC hDC, out Win32.POINT point);

        [EntryPoint(0x005F)]
        public uint GetViewportOrg(HDC hDC)
        {
            Win32.POINT point;
            if (!GetViewportOrgEx(hDC, out point))
                return 0;

            return point.ToDWord();
        }

        [DllImport("gdi32.dll")]
        public static extern bool GetWindowExtEx(HDC hDC, out Win32.SIZE size);

        [EntryPoint(0x0060)]
        public uint GetWindowExt(HDC hDC)
        {
            Win32.SIZE size;
            if (!GetWindowExtEx(hDC, out size))
                return 0;

            return BitUtils.MakeDWord((ushort)(short)size.Width, (ushort)(short)size.Height);
        }

        [DllImport("gdi32.dll")]
        public static extern bool GetWindowOrgEx(HDC hDC, out Win32.POINT point);

        [EntryPoint(0x0061)]
        public uint GetWindowOrg(HDC hDC)
        {
            Win32.POINT point;
            if (!GetWindowOrgEx(hDC, out point))
                return 0;

            return point.ToDWord();
        }

        // 0062 - INTERSECTVISRECT

        [DllImport("gdi32.dll")]
        static extern bool LPtoDP(IntPtr hdc, [In, Out] Win32.POINT[] lpPoints, int nCount);

        [EntryPoint(0x0063)]
        public bool LPtoDP(HDC hDC, uint ppts, nint nCount)
        {
            // Convert to 32
            var pts = new Win32.POINT[nCount];
            for (int i = 0; i < nCount; i++)
            {
                pts[i] = _machine.ReadStruct<Win16.POINT>((uint)(ppts + i * Marshal.SizeOf<Win16.POINT>())).Convert();
            }

            // Calculate
            bool val = LPtoDP(hDC.value, pts, nCount);

            // And back
            for (int i = 0; i < nCount; i++)
            {
                _machine.WriteStruct((uint)(ppts + i * Marshal.SizeOf<Win16.POINT>()), pts[i].Convert());
            }

            return val;
        }


        public delegate void LINEDDAPROC(int x, int y, IntPtr lParam);

        [DllImport("gdi32.dll")]
        public static extern bool LineDDA(int x1, int y1, int x2, int y2, LINEDDAPROC callback, IntPtr lParam);

        [EntryPoint(0x0064)]
        public bool LineDDA(nint x1, nint y1, nint x2, nint y2, uint callback, uint lParam)
        {
            return LineDDA(x1, y1, x2, y2, (x, y, data) =>
            {
                _machine.PushWord((ushort)(short)x);
                _machine.PushWord((ushort)(short)y);
                _machine.PushDWord(lParam);
                _machine.CallVM(callback, "LineDDAProc");
            }, IntPtr.Zero);
        }

        [EntryPoint(0x0065)]
        [DllImport("gdi32.dll")]
        public static extern nint OffsetRgn(HGDIOBJ hRgn, nint x, nint y);

        // 0066 - OFFSETVISRGN

        [EntryPoint(0x0067)]
        [DllImport("gdi32.dll")]
        public static extern bool PtVisible(HDC hDC, nint x, nint y);

        [EntryPoint(0x0068)]
        [DllImport("gdi32.dll")]
        public static extern bool RectVisible(HDC hDC, ref Win32.RECT rc);

        [EntryPoint(0x0069)]
        [DllImport("gdi32.dll")]
        public static extern nint SelectVisRgn(HDC hDC, HGDIOBJ hRgn);

        [DllImport("gdi32.dll")]
        public static extern int SetBitmapBits(IntPtr hBitmap, int cbBuffer, IntPtr pBuffer);

        [EntryPoint(0x006A)]
        public int SetBitmapBits(HGDIOBJ hBitmap, int cbBuffer, uint pBuffer)
        {
            if (cbBuffer <= 0 || pBuffer == 0)
                return SetBitmapBits(hBitmap.value, cbBuffer, IntPtr.Zero);

            using (var hp = _machine.GlobalHeap.GetHeapPointer(pBuffer, false))
            {
                return SetBitmapBits(hBitmap.value, cbBuffer, hp);
            }
        }

        // 0075 - SETDCORG
        // 0077 - ADDFONTRESOURCE
        // 0079 - DEATH
        // 007A - RESURRECTION
 
        [EntryPoint(0x007b)]
        [DllImport("gdi32.dll")]
        public static extern bool PlayMetaFile(HDC hDC, HENHMETAFILE hMetaFile);

        [DllImport("gdi32.dll", EntryPoint = "EnumMetaFile")]
        static extern bool _EnumMetaFile(IntPtr hDC, HENHMETAFILE hMetaFile, MFENUMPROC callback, IntPtr lParam);

        [EntryPoint(0x007C)]
        [DllImport("gdi32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetMetaFileW")]
        public static extern HENHMETAFILE GetMetaFile([FileName(false)] string lpszMetaFile);

        [EntryPoint(0x007D)]
        [DllImport("gdi32.dll", CharSet = CharSet.Unicode, EntryPoint = "CreateMetaFileW")]
        public static extern HDC CreateMetaFile([FileName(true)] string lpszMetaFile);

        [EntryPoint(0x007E)]
        [DllImport("gdi32.dll")]
        public static extern HENHMETAFILE CloseMetaFile([Destroyed] HDC hDC);

        [EntryPoint(0x007f)]
        [DllImport("gdi32.dll")]
        public static extern bool DeleteMetaFile([Destroyed] HENHMETAFILE hMetaFile);

        [EntryPoint(0x0080)]
        public short MulDiv(short a, short b, short c)
        {
            return (short)(a * b / c);
        }

        // 0081 - SAVEVISRGN
        // 0082 - RESTOREVISRGN
        // 0083 - INQUIREVISRGN
        // 0084 - SETENVIRONMENT
        // 0085 - GETENVIRONMENT
        [DllImport("gdi32.dll")]
        static extern nint GetRgnBox(HGDIOBJ hRgn, out Win32.RECT rc);

        [EntryPoint(0x0086)]
        public nint GetRgnBox(HGDIOBJ hRgn, uint pRect)
        {
            Win32.RECT rc;
            var retv = GetRgnBox(hRgn, out rc);
            if (pRect != 0)
                _machine.WriteStruct(pRect, rc.Convert());
            return retv;
        }
        [EntryPoint(0x0087)]
        // Legacy printer-era helper with no gdi32 equivalent; report unsupported.
        public short ScanLR(HDC hDC, short x, short y, uint color, short dirStyle)
        {
            return -1;
        }

        [EntryPoint(0x0088)]
        [DllImport("gdi32.dll", CharSet = CharSet.Unicode, EntryPoint = "RemoveFontResourceW")]
        public static extern bool RemoveFontResource([FileName(false)] string lpszFilename);

        [DllImport("gdi32.dll")]
        public static extern bool SetBrushOrgEx(HDC hDC, int x, int y, out Win32.POINT pptOld);

        [DllImport("gdi32.dll")]
        public static extern bool GetBrushOrgEx(HDC hDC, out Win32.POINT pptOld);

        [EntryPoint(0x0094)]
        public uint SetBrushOrg(HDC hDC, nint x, nint y)
        {
            Win32.POINT old32;
            if (!SetBrushOrgEx(hDC, x, y, out old32))
            {
                return 0;
            }
            return old32.ToDWord();
        }

        [EntryPoint(0x0095)]
        public uint GetBrushOrg(HDC hDC)
        {
            Win32.POINT old32;
            if (!GetBrushOrgEx(hDC, out old32))
                return 0;

            return old32.ToDWord();
        }

        [EntryPoint(0x0096)]
        [DllImport("gdi32.dll")]
        public static extern bool UnrealizeObject(HGDIOBJ hObj);

        [EntryPoint(0x0097)]
        [DllImport("gdi32.dll", CharSet = CharSet.Unicode, EntryPoint = "CopyMetaFileW")]
        public static extern HENHMETAFILE CopyMetaFile(HENHMETAFILE hSrcMetaFile, [FileName(true)] string lpszFile);

        [EntryPoint(0x0099)]
        [DllImport("gdi32.dll")]
        public static extern HDC CreateIC(string lpszDriver, string lpszDevice, string lpszOutput, [MustBeNull] IntPtr lpdvmInit);

        [EntryPoint(0x009a)]
        [DllImport("gdi32.dll")]
        public static extern uint GetNearestColor(HDC hDC, uint color);

        [EntryPoint(0x009B)]
        public int QueryAbort(HDC hDC, nint reserved)
        {
            if (_abortProcs.TryGetValue(hDC.value, out var abortProc))
                return abortProc(hDC.value, 0);

            return 1;
        }

        [EntryPoint(0x009c)]
        [DllImport("gdi32.dll")]
        public static extern HGDIOBJ CreateDiscardableBitmap(HDC hDC, nint width, nint height);

        [DllImport("gdi32.dll", EntryPoint = "GetMetaFileBitsEx")]
        static extern uint _GetMetaFileBitsEx(HENHMETAFILE hMetaFile, uint cbBuffer, IntPtr pBuffer);

        [EntryPoint(0x009F)]
        public ushort GetMetaFileBits(HENHMETAFILE hMetaFile)
        {
            if (hMetaFile.value == IntPtr.Zero)
                return 0;

            uint size = _GetMetaFileBitsEx(hMetaFile, 0, IntPtr.Zero);
            if (size == 0)
                return 0;

            ushort handle = _machine.GlobalHeap.Alloc("MetaFileBits", 0, size);
            using (var hp = _machine.GlobalHeap.GetHeapPointer(BitUtils.MakeDWord(0, handle), true))
            {
                if (_GetMetaFileBitsEx(hMetaFile, size, hp) == 0)
                {
                    _machine.GlobalHeap.Free(handle);
                    return 0;
                }
            }

            return handle;
        }

        [DllImport("gdi32.dll")]
        public static extern IntPtr SetMetaFileBitsEx(uint cbBuffer, IntPtr pBuffer);

        [EntryPoint(0x00a0)]
        public ushort SetMetaFileBits(ushort handle)
        {
            if (handle == 0)
                return 0;

            // Get size of global allocation
            uint size = _machine.GlobalHeap.Size(handle);

            // Get pointer
            var hp = _machine.GlobalHeap.GetHeapPointer(BitUtils.MakeDWord(0, handle), false);

            var hEnhMetaFile = SetMetaFileBitsEx(size, hp);

            return HENHMETAFILE.To16(hEnhMetaFile);
        }

        [EntryPoint(0x00A1)]
        [DllImport("gdi32.dll")]
        public static extern bool PtInRegion(HGDIOBJ hRgn, nint x, nint y);

        [DllImport("gdi32.dll")]
        public static extern bool GetBitmapDimensionEx(HGDIOBJ hBitmap, out Win32.SIZE size);

        [EntryPoint(0x00A2)]
        public uint GetBitmapDimension(HGDIOBJ hBitmap)
        {
            Win32.SIZE size;
            if (!GetBitmapDimensionEx(hBitmap, out size))
                return 0;

            return BitUtils.MakeDWord((ushort)(short)size.Width, (ushort)(short)size.Height);
        }

        [DllImport("gdi32.dll")]
        public static extern bool SetBitmapDimensionEx(HGDIOBJ hBitmap, int width, int height, out Win32.SIZE size);

        [EntryPoint(0x00A3)]
        public uint SetBitmapDimension(HGDIOBJ hBitmap, short width, short height)
        {
            Win32.SIZE size;
            if (!SetBitmapDimensionEx(hBitmap, width, height, out size))
                return 0;

            return BitUtils.MakeDWord((ushort)(short)size.Width, (ushort)(short)size.Height);
        }

        // 00A9 - ISDCDIRTY
        // 00AA - SETDCSTATUS


        [EntryPoint(0x00ac)]
        [DllImport("gdi32.dll")]
        public static extern void SetRectRgn(HGDIOBJ hRgn, nint l, nint t, nint r, nint b);


        [EntryPoint(0x00AD)]
        [DllImport("gdi32.dll")]
        public static extern nint GetClipRgn(HDC hDC, HGDIOBJ hRgn);

        [EntryPoint(0x00AF)]
        public bool EnumMetaFile(HDC hDC, HENHMETAFILE hMetaFile, uint callback, uint lParam)
        {
            if (callback == 0)
                return false;

            return _EnumMetaFile(hDC.value, hMetaFile, (hdc, lpHTable, lpMFR, nObj, lpData) =>
            {
                uint handleTable16 = 0;
                uint metaRecord16 = 0;

                try
                {
                    if (nObj > 0)
                    {
                        uint handleTableBytes = checked((uint)nObj * sizeof(ushort));
                        if (handleTableBytes > ushort.MaxValue)
                        {
                            Log.WriteLine("EnumMetaFile: handle table too large ({0} entries)", nObj);
                            return 0;
                        }

                        handleTable16 = _machine.SysAlloc((ushort)handleTableBytes);
                        for (int i = 0; i < nObj; i++)
                        {
                            var hObject = Marshal.ReadIntPtr(lpHTable, i * IntPtr.Size);
                            uint entryPtr = (uint)(handleTable16 + i * sizeof(ushort));
                            _machine.WriteWord(entryPtr.Hiword(), entryPtr.Loword(), HGDIOBJ.To16(hObject));
                        }
                    }

                    uint recordWords = unchecked((uint)Marshal.ReadInt32(lpMFR));
                    uint recordBytes = checked(recordWords * sizeof(ushort));
                    if (recordBytes > ushort.MaxValue)
                    {
                        Log.WriteLine("EnumMetaFile: record too large ({0} bytes)", recordBytes);
                        return 0;
                    }

                    metaRecord16 = _machine.SysAlloc((ushort)recordBytes);
                    int recordByteCount = (int)recordBytes;
                    byte[] recordBuffer = new byte[recordByteCount];
                    Marshal.Copy(lpMFR, recordBuffer, 0, recordByteCount);
                    using (var hp = _machine.GlobalHeap.GetHeapPointer(metaRecord16, true))
                    {
                        Marshal.Copy(recordBuffer, 0, hp, recordByteCount);
                    }

                    _machine.PushWord(HDC.To16(hdc));
                    _machine.PushDWord(handleTable16);
                    _machine.PushDWord(metaRecord16);
                    _machine.PushWord((ushort)nObj);
                    _machine.PushDWord(lpData.DWord());
                    _machine.CallVM(callback, "EnumMetaFileProc");
                    return _machine.ax;
                }
                finally
                {
                    if (metaRecord16 != 0)
                        _machine.SysFree(metaRecord16);
                    if (handleTable16 != 0)
                        _machine.SysFree(handleTable16);
                }
            }, BitUtils.DWordToIntPtr(lParam));
        }

        [DllImport("gdi32.dll", EntryPoint = "PlayMetaFileRecord")]
        static extern bool _PlayMetaFileRecord(HDC hDC, IntPtr lpHandleTable, IntPtr lpMetaRecord, uint nHandles);

        [EntryPoint(0x00B0)]
        public bool PlayMetaFileRecord(HDC hDC, uint lpHandleTable, uint lpMetaRecord, ushort nHandles)
        {
            if (lpMetaRecord == 0)
                return false;

            var handleTable32 = IntPtr.Zero;
            var metaRecord32 = IntPtr.Zero;

            try
            {
                if (nHandles != 0 && lpHandleTable != 0)
                {
                    handleTable32 = Marshal.AllocHGlobal(nHandles * IntPtr.Size);
                    for (int i = 0; i < nHandles; i++)
                    {
                        var hObject16 = _machine.ReadWord((uint)(lpHandleTable + i * sizeof(ushort)));
                        Marshal.WriteIntPtr(handleTable32, i * IntPtr.Size, HGDIOBJ.To32(hObject16).value);
                    }
                }

                uint recordWords = _machine.ReadDWord(lpMetaRecord);
                uint recordBytes = checked(recordWords * sizeof(ushort));
                int recordByteCount = (int)recordBytes;
                metaRecord32 = Marshal.AllocHGlobal(recordByteCount);
                byte[] recordBuffer = new byte[recordByteCount];
                using (var hp = _machine.GlobalHeap.GetHeapPointer(lpMetaRecord, false))
                {
                    Marshal.Copy(hp, recordBuffer, 0, recordByteCount);
                }
                Marshal.Copy(recordBuffer, 0, metaRecord32, recordByteCount);

                return _PlayMetaFileRecord(hDC, handleTable32, metaRecord32, nHandles);
            }
            finally
            {
                if (metaRecord32 != IntPtr.Zero)
                    Marshal.FreeHGlobal(metaRecord32);
                if (handleTable32 != IntPtr.Zero)
                    Marshal.FreeHGlobal(handleTable32);
            }
        }
        // 00B3 - GETDCSTATE
        // 00B4 - SETDCSTATE
        [DllImport("gdi32.dll")]
        static extern bool RectInRegion(HGDIOBJ hRgn, ref Win32.RECT rc);

        [EntryPoint(0x00B5)]
        public bool RectInRegion(HGDIOBJ hRgn, ref Win16.RECT rc)
        {
            var rc32 = rc.Convert();
            return RectInRegion(hRgn, ref rc32);
        }
        // 00BE - SETDCHOOK
        // 00BF - GETDCHOOK
        // 00C0 - SETHOOKFLAGS
        [DllImport("gdi32.dll", EntryPoint = "SetBoundsRect")]
        static extern uint _SetBoundsRect(HDC hDC, IntPtr lprcBounds, uint flags);

        [EntryPoint(0x00C1)]
        public uint SetBoundsRect(HDC hDC, uint lprcBounds, uint flags)
        {
            if (lprcBounds == 0)
                return _SetBoundsRect(hDC, IntPtr.Zero, flags);

            var rc32 = _machine.ReadStruct<Win16.RECT>(lprcBounds).Convert();
            unsafe
            {
                return _SetBoundsRect(hDC, (IntPtr)(&rc32), flags);
            }
        }

        [DllImport("gdi32.dll", EntryPoint = "GetBoundsRect")]
        static extern uint _GetBoundsRect(HDC hDC, IntPtr lprcBounds, uint flags);

        [EntryPoint(0x00C2)]
        public uint GetBoundsRect(HDC hDC, uint lprcBounds, uint flags)
        {
            if (lprcBounds == 0)
                return _GetBoundsRect(hDC, IntPtr.Zero, flags);

            Win32.RECT rc32 = default;
            unsafe
            {
                uint retv = _GetBoundsRect(hDC, (IntPtr)(&rc32), flags);
                _machine.WriteStruct(lprcBounds, rc32.Convert());
                return retv;
            }
        }

        [EntryPoint(0x00C3)]
        public HGDIOBJ SelectBitmap(HDC hDC, HGDIOBJ hBitmap)
        {
            return SelectObject(hDC, hBitmap);
        }

        [EntryPoint(0x00C4)]
        public ushort SetMetaFileBitsBetter(ushort handle)
        {
            return SetMetaFileBits(handle);
        }
        // 00C9 - DMBITBLT
        // 00CA - DMCOLORINFO
        // 00CE - DMENUMDFONTS
        // 00CF - DMENUMOBJ
        // 00D0 - DMOUTPUT
        // 00D1 - DMPIXEL
        // 00D2 - DMREALIZEOBJECT
        // 00D3 - DMSTRBLT
        // 00D4 - DMSCANLR
        // 00D5 - BRUTE
        // 00D6 - DMEXTTEXTOUT
        // 00D7 - DMGETCHARWIDTH
        // 00D8 - DMSTRETCHBLT
        // 00D9 - DMDIBBITS
        // 00DA - DMSTRETCHDIBITS
        // 00DB - DMSETDIBTODEV
        // 00DC - DMTRANSPOSE
        // 00E6 - CREATEPQ
        // 00E7 - MINPQ
        // 00E8 - EXTRACTPQ
        // 00E9 - INSERTPQ
        // 00EA - SIZEPQ
        // 00EB - DELETEPQ
        // 00F0 - OPENJOB
        // 00F1 - WRITESPOOL
        // 00F2 - WRITEDIALOG
        // 00F3 - CLOSEJOB
        // 00F4 - DELETEJOB
        // 00F5 - GETSPOOLJOB
        // 00F6 - STARTSPOOLPAGE
        // 00F7 - ENDSPOOLPAGE
        // 00F8 - QUERYJOB
        // 00FA - COPY
        // 00FD - DELETESPOOLPAGE
        // 00FE - SPOOLFILE
        // 012C - ENGINEENUMERATEFONT
        // 012D - ENGINEDELETEFONT
        // 012E - ENGINEREALIZEFONT
        // 012F - ENGINEGETCHARWIDTH
        // 0130 - ENGINESETFONTCONTEXT
        // 0131 - ENGINEGETGLYPHBMP
        // 0132 - ENGINEMAKEFONTDIR
        // 0133 - GETCHARABCWIDTHS
        // 0134 - GETOUTLINETEXTMETRICS
        // 0135 - GETGLYPHOUTLINE
        // 0136 - CREATESCALABLEFONTRESOURCE
        // 0137 - GETFONTDATA
        // 0138 - CONVERTOUTLINEFONTFILE
        // 0139 - GETRASTERIZERCAPS
        // 013A - ENGINEEXTTEXTOUT
        // 014A - ENUMFONTFAMILIES
        // 014C - GETKERNINGPAIRS

        [EntryPoint(0x0159)]
        [DllImport("gdi32.dll")]
        public static extern nuint GetTextAlign(HDC hDC);

        [EntryPoint(0x015A)]
        [DllImport("gdi32.dll")]
        public static extern nuint SetTextAlign(HDC hDC, nuint align);

        [EntryPoint(0x015C)]
        [DllImport("gdi32.dll")]
        public static extern bool Chord(HDC hDC, nint left, nint top, nint right, nint bottom,
                                                    nint xr1, nint yr1, nint xr2, nint yr2);

        // 015D - SETMAPPERFLAGS

        [DllImport("gdi32.dll", EntryPoint = "GetCharWidthW", CharSet = CharSet.Unicode)]
        static extern bool _GetCharWidth(HDC hDC, uint iFirstChar, uint iLastChar, [Out] int[] lpBuffer);

        [EntryPoint(0x015E)]
        public bool GetCharWidth(HDC hDC, ushort iFirstChar, ushort iLastChar, uint lpBuffer)
        {
            if (lpBuffer == 0 || iLastChar < iFirstChar)
                return false;

            int count = iLastChar - iFirstChar + 1;
            var widths = new int[count];
            if (!_GetCharWidth(hDC, iFirstChar, iLastChar, widths))
                return false;

            for (int i = 0; i < count; i++)
            {
                uint entryPtr = (uint)(lpBuffer + i * sizeof(ushort));
                _machine.WriteWord(entryPtr.Hiword(), entryPtr.Loword(), unchecked((ushort)(short)widths[i]));
            }

            return true;
        }

        [DllImport("gdi32.dll")]
        public static extern bool ExtTextOut(IntPtr hDC, int x, int y, uint fuOptions, IntPtr prc, string str, uint cch, IntPtr lpDX);

        [EntryPoint(0x015f)]
        public bool ExtTextOut(HDC hDC, nint x, nint y, nuint fuOptions, uint prcRect, uint lpstr, nuint cch, uint lpDX)
        {
            // Convert the rectangle
            Win32.RECT rc;
            if (prcRect!=0)
            {
                rc = _machine.ReadStruct<Win16.RECT>(prcRect).Convert();
            }

            // Read the string
            var str = _machine.GlobalHeap.ReadCharacters(lpstr, (int)cch.value);

            // Read deltas
            int[] dx = null;
            if (lpDX!=0)
            {
                dx = new int[cch.value];
                for (int i=0; i < cch; i++)
                {
                    dx[i] = _machine.ReadWord((uint)(lpDX + i * 2));
                }
            }

            // Call
            unsafe
            {
                fixed (int* pdx = dx)
                {
                    ExtTextOut(hDC.value, x, y, fuOptions, prcRect == 0 ? IntPtr.Zero : (IntPtr)(&rc), str, cch, lpDX == 0 ? IntPtr.Zero : (IntPtr)pdx);
                }
            }

            return false;
        }

        // 0160 - GETPHYSICALFONTHANDLE
        [DllImport("gdi32.dll")]
        public static extern bool GetAspectRatioFilterEx(HDC hDC, out Win32.SIZE size);

        [EntryPoint(0x0161)]
        public uint GetAspectRatioFilter(HDC hDC)
        {
            Win32.SIZE size;
            if (!GetAspectRatioFilterEx(hDC, out size))
                return 0;

            return BitUtils.MakeDWord((ushort)(short)size.Width, (ushort)(short)size.Height);
        }

        // 0162 - SHRINKGDIHEAP
        // 0163 - FTRAPPING0

        [DllImport("gdi32.dll")]
        public static extern HGDIOBJ CreatePalette(IntPtr ptr);

        [EntryPoint(0x0168)]
        public HGDIOBJ CreatePalette(uint pLogPalette)
        {
            using (var hp = _machine.GlobalHeap.GetHeapPointer(pLogPalette, false))
            {
                return CreatePalette(hp);
            }
        }

        [EntryPoint(0x0169)]
        [DllImport("gdi32.dll")]
        public static extern HGDIOBJ SelectPalette(HDC hDC, HGDIOBJ hPalette, bool forceBackground);

        [EntryPoint(0x016A)]
        [DllImport("gdi32.dll")]
        public static extern nuint RealizePalette(HDC hDC);

        [DllImport("gdi32.dll", EntryPoint = "GetPaletteEntries")]
        public static extern uint _GetPaletteEntries(HGDIOBJ hPalette, uint iStartIndex, uint nEntries, IntPtr ptr);

        [EntryPoint(0x016B)]
        public nuint GetPaletteEntries(HGDIOBJ hPalette, nuint iStartIndex, nuint nEntries, uint lppe)
        {
            if (lppe == 0)
                return _GetPaletteEntries(hPalette, iStartIndex, nEntries, IntPtr.Zero);

            using (var hp = _machine.GlobalHeap.GetHeapPointer(lppe, true))
            {
                return _GetPaletteEntries(hPalette, iStartIndex, nEntries, hp);
            }
        }

        [DllImport("gdi32.dll", EntryPoint = "SetPaletteEntries")]
        public static extern uint _SetPaletteEntries(HGDIOBJ hPalette, uint iStartIndex, uint nEntries, IntPtr ptr);

        [EntryPoint(0x016C)]
        public nuint SetPaletteEntries(HGDIOBJ hPalette, nuint iStartIndex, nuint nEntries, uint lppe)
        {
            if (lppe == 0)
                return _SetPaletteEntries(hPalette, iStartIndex, nEntries, IntPtr.Zero);

            using (var hp = _machine.GlobalHeap.GetHeapPointer(lppe, false))
            {
                return _SetPaletteEntries(hPalette, iStartIndex, nEntries, hp);
            }
        }

        [EntryPoint(0x016D)]
        public nuint RealizeDefaultPalette(HDC hDC)
        {
            var previousPalette = SelectPalette(hDC, GetStockObject(15), false);
            var realizedEntries = RealizePalette(hDC);
            if (previousPalette.value != IntPtr.Zero)
            {
                SelectPalette(hDC, previousPalette, false);
            }
            return realizedEntries;
        }

        [EntryPoint(0x016E)]
        [DllImport("gdi32.dll")]
        public static extern bool UpdateColors(HDC hDC);

        [EntryPoint(0x016F)]
        [DllImport("gdi32.dll")]
        public static extern bool AnimatePalette(HGDIOBJ hPalette, uint iStartIndex, uint cEntries, IntPtr ppe);

        [EntryPoint(0x0170)]
        [DllImport("gdi32.dll")]
        public static extern bool ResizePalette(HGDIOBJ hPalette, uint nEntries);

        [EntryPoint(0x0172)]
        [DllImport("gdi32.dll")]
        public static extern uint GetNearestPaletteIndex(HGDIOBJ hPalette, uint color);

        [EntryPoint(0x0174)]
        [DllImport("gdi32.dll")]
        public static extern bool ExtFloodFill(HDC hDC, nint x, nint y, uint color, nuint fillType);

        [EntryPoint(0x0175)]
        [DllImport("gdi32.dll")]
        public static extern uint SetSystemPaletteUse(HDC hDC, uint use);

        [EntryPoint(0x0176)]
        [DllImport("gdi32.dll")]
        public static extern uint GetSystemPaletteUse(HDC hDC);

        [DllImport("gdi32.dll", EntryPoint = "GetSystemPaletteEntries")]
        public static extern uint _GetSystemPaletteEntries(HDC hDC, uint iStartIndex, uint nEntries, IntPtr ptr);

        [EntryPoint(0x0177)]
        public nuint GetSystemPaletteEntries(HDC hDC, nuint iStartIndex, nuint nEntries, uint lppe)
        {
            using (var hp = _machine.GlobalHeap.GetHeapPointer(lppe, true))
            {
                return _GetSystemPaletteEntries(hDC, iStartIndex, nEntries, hp);
            }
        }

        // 0178 - RESETDC
        [DllImport("gdi32.dll", CharSet = CharSet.Unicode, EntryPoint = "StartDocW")]
        static extern int _StartDoc(IntPtr hDC, ref Win32.DOCINFO lpdi);

        [EntryPoint(0x0179)]
        public int StartDoc(HDC hDC, uint lpDocInfo)
        {
            if (lpDocInfo == 0)
                return 0;

            var docInfo16 = _machine.ReadStruct<Win16.DOCINFO>(lpDocInfo);
            using (var ctx = new TempContext(_machine))
            {
                var docInfo32 = new Win32.DOCINFO()
                {
                    cbSize = Marshal.SizeOf<Win32.DOCINFO>(),
                    lpszDocName = docInfo16.lpszDocName != 0 ? ctx.AllocUnmanagedString(_machine.ReadString(docInfo16.lpszDocName)) : IntPtr.Zero,
                    lpszOutput = docInfo16.lpszOutput != 0 ? ctx.AllocUnmanagedString(_machine.ReadString(docInfo16.lpszOutput)) : IntPtr.Zero,
                };
                return _StartDoc(hDC.value, ref docInfo32);
            }
        }

        [EntryPoint(0x017A)]
        [DllImport("gdi32.dll")]
        public static extern int EndDoc(HDC hDC);

        [EntryPoint(0x017B)]
        [DllImport("gdi32.dll")]
        public static extern int StartPage(HDC hDC);

        [EntryPoint(0x017C)]
        [DllImport("gdi32.dll")]
        public static extern int EndPage(HDC hDC);

        [DllImport("gdi32.dll", EntryPoint = "SetAbortProc")]
        static extern int _SetAbortProc(IntPtr hDC, ABORTPROC lpAbortProc);

        [EntryPoint(0x017D)]
        public int SetAbortProc(HDC hDC, uint lpAbortProc)
        {
            ABORTPROC abortProc32 = null;
            if (lpAbortProc != 0)
            {
                abortProc32 = (hdc, code) =>
                {
                    _machine.PushWord(HDC.To16(hdc));
                    _machine.PushWord((ushort)(short)code);
                    _machine.CallVM(lpAbortProc, "AbortProc");
                    return (short)_machine.ax;
                };
            }

            int retv = _SetAbortProc(hDC.value, abortProc32);
            if (retv > 0)
            {
                if (abortProc32 != null)
                    _abortProcs[hDC.value] = abortProc32;
                else
                    _abortProcs.Remove(hDC.value);
            }
            return retv;
        }

        [EntryPoint(0x017E)]
        [DllImport("gdi32.dll")]
        public static extern int AbortDoc(HDC hDC);

        // 0190 - FASTWINDOWFRAME
        // 0191 - GDIMOVEBITMAP
        // 0193 - GDIINIT2
        // 0195 - FINALGDIINIT
        // 0197 - CREATEUSERBITMAP
        // 0199 - CREATEUSERDISCARDABLEBITMAP
        [EntryPoint(0x019A)]
        public bool IsValidMetaFile(HENHMETAFILE hMetaFile)
        {
            return hMetaFile.value != IntPtr.Zero;
        }
        // 019B - GETCURLOGFONT
        // 019C - ISDCCURRENTPALETTE

        [DllImport("gdi32.dll")]
        public static extern int StretchDIBits(IntPtr hdc,
               int xDest, int yDest, int destWidth, int destHeight,
               int xSrc, int ySrc, int srcWidth, int srcHeight,
               IntPtr pBits, IntPtr pBitsInfo, uint iUsage, uint rop);

        [EntryPoint(0x01b7)]
        public nint StretchDIBits(HDC hdc,
                    nint xDest, nint yDest, nint destWidth, nint destHeight,
                    nint xSrc, nint ySrc, nint srcWidth, nint srcHeight,
                    uint pBits, uint pBitsInfo, nuint iUsage, uint rop)
        {
            using (var hpBits = _machine.GlobalHeap.GetHeapPointer(pBits, false))
            using (var hpBitsInfo = _machine.GlobalHeap.GetHeapPointer(pBitsInfo, false))
            {
                return StretchDIBits(hdc.value, xDest, yDest, destWidth, destHeight,
                                    xSrc, ySrc, srcWidth, srcHeight,
                                    hpBits, hpBitsInfo,
                                    iUsage, rop);
            }
        }

        [DllImport("gdi32.dll")]
        public static extern int SetDIBits(HDC hDC, HGDIOBJ hBitmap, uint startScan, uint scanLines, IntPtr bits, IntPtr bitsInfo, uint usage);

        [EntryPoint(0x01B8)]
        public nint SetDIBits(HDC hDC, HGDIOBJ hBitmap, nuint startScan, nuint scanLines, uint bits, uint bitsInfo, nuint usage)
        {
            using (var hpBits = _machine.GlobalHeap.GetHeapPointer(bits, false))
            using (var hpBitsInfo = _machine.GlobalHeap.GetHeapPointer(bitsInfo, false))
            {
                return SetDIBits(hDC, hBitmap, startScan, scanLines, hpBits, hpBitsInfo, usage);
            }
        }

        [DllImport("gdi32.dll")]
        public static extern int GetDIBits(HDC hDC, HGDIOBJ hBitmap, uint startScan, uint scanLines, IntPtr bits, IntPtr bitsInfo, uint usage);

        [EntryPoint(0x01B9)]
        public nint GetDIBits(HDC hDC, HGDIOBJ hBitmap, nuint startScan, nuint scanLines, uint bits, uint bitsInfo, nuint usage)
        {
            using (var hpBits = _machine.GlobalHeap.GetHeapPointer(bits, true))
            using (var hpBitsInfo = _machine.GlobalHeap.GetHeapPointer(bitsInfo, true))
            {
                return GetDIBits(hDC, hBitmap, startScan, scanLines, hpBits, hpBitsInfo, usage);
            }
        }

        [DllImport("gdi32.dll")]
        public static extern HGDIOBJ CreateDIBitmap(HDC hdc, IntPtr lpbmih, uint fdwInit, IntPtr lpbInit, IntPtr lpbmi, uint fuUsage);

        [EntryPoint(0x01BA)]
        public HGDIOBJ CreateDIBitmap(HDC hDC, uint lpbmih, uint dwInit, uint lpbInit, uint lpbmi, ushort fuUsage)
        {
            using (var hpbmih = _machine.GlobalHeap.GetHeapPointer(lpbmih, false))
            using (var hpbInit = _machine.GlobalHeap.GetHeapPointer(lpbInit, false))
            using (var hpbmi = _machine.GlobalHeap.GetHeapPointer(lpbmi, false))
            {
                return CreateDIBitmap(hDC, hpbmih, dwInit, hpbInit, hpbmi, fuUsage);
            }
        }

        [DllImport("gdi32.dll")]
        public static extern int SetDIBitsToDevice(HDC hDC, int XDest, int YDest, uint dwWidth, uint dwHeight,
                                                    int XSrc, int YSrc, uint uStartScan, uint cScanLines,
                                                    IntPtr lpvBits, IntPtr lpbmi, uint fuColorUse);

        [EntryPoint(0x01bb)]
        public nint SetDIBitsToDevice(HDC hDC, nint XDest, nint YDest, nuint dwWidth, nuint dwHeight,
                                                    nint XSrc, nint YSrc, nuint uStartScan, nuint cScanLines,
                                                    uint lpvbits, uint lpbmi, nuint fuColorUse)
        {
            using (var hpvbits = _machine.GlobalHeap.GetHeapPointer(lpvbits, false))
            using (var hpbmi = _machine.GlobalHeap.GetHeapPointer(lpbmi, false))
            {
                return SetDIBitsToDevice(hDC, XDest, YDest, dwWidth, dwHeight,
                                        XSrc, YSrc, uStartScan, cScanLines,
                                        hpvbits, hpbmi, fuColorUse);
            }
        }


        [EntryPoint(0x01BC)]
        [DllImport("gdi32.dll")]
        public static extern HGDIOBJ CreateRoundRectRgn(nint left, nint top, nint right, nint bottom, nint widthEllipse, nint heightEllipse);
        // 01BD - CREATEDIBPATTERNBRUSH
        // 01C1 - DEVICECOLORMATCH

        [DllImport("gdi32.dll")]
        static extern HGDIOBJ CreatePolyPolygonRgn(Win32.POINT[] points, int[] polygonPointCounts, int polygonCount, int fillMode);

        [DllImport("gdi32.dll")]
        static extern bool PolyPolygon(HDC hdc, Win32.POINT[] points, int[] polygonPointCounts, int polygonCount);

        [EntryPoint(0x01C2)]
        public bool PolyPolygon(HDC hDC, uint ppts, uint ppolyCounts, nint polygonCount)
        {
            var polyCounts = new int[polygonCount];
            int totalPointCount = 0;
            for (int i = 0; i < polygonCount; i++)
            {
                polyCounts[i] = _machine.ReadWord((uint)(ppolyCounts + i * sizeof(ushort)));
                totalPointCount += polyCounts[i];
            }

            var points = new Win32.POINT[totalPointCount];
            for (int i = 0; i < totalPointCount; i++)
            {
                points[i] = _machine.ReadStruct<Win16.POINT>((uint)(ppts + i * Marshal.SizeOf<Win16.POINT>())).Convert();
            }

            return PolyPolygon(hDC, points, polyCounts, polygonCount);
        }

        [EntryPoint(0x01C3)]
        public HGDIOBJ CreatePolyPolygonRgn(uint ppts, uint ppolyCounts, nint polygonCount, nint fillMode)
        {
            var polyCounts = new int[polygonCount];
            int totalPointCount = 0;
            for (int i = 0; i < polygonCount; i++)
            {
                polyCounts[i] = _machine.ReadWord((uint)(ppolyCounts + i * sizeof(ushort)));
                totalPointCount += polyCounts[i];
            }

            var points = new Win32.POINT[totalPointCount];
            for (int i = 0; i < totalPointCount; i++)
            {
                points[i] = _machine.ReadStruct<Win16.POINT>((uint)(ppts + i * Marshal.SizeOf<Win16.POINT>())).Convert();
            }

            return CreatePolyPolygonRgn(points, polyCounts, polygonCount, fillMode);
        }

        // 01C4 - GDISEEGDIDO
        // 01CC - GDITASKTERMINATION
        // 01CD - SETOBJECTOWNER
        [EntryPoint(0x01CD)]
        // does not exist anymore in gdi32.dll
        public void SetObjectOwner(HGDIOBJ hObject, uint hTask)
        {
            // nothing to do
            return;
        }
        // 01CE - ISGDIOBJECT
        [EntryPoint(0x01CE)]
        // does not exist anymore in gdi32.dll
        public bool IsGdiObject(HGDIOBJ hObject)
        {
            return hObject.value != IntPtr.Zero;
        }
        [EntryPoint(0x01CF)]
        // Internal Win16 GDI helper; modern gdi32 has no equivalent.
        public bool MakeObjectPrivate(HGDIOBJ hObject, nint reserved)
        {
            return hObject.value != IntPtr.Zero;
        }

        [EntryPoint(0x01D0)]
        // Internal Win16 GDI metafile fixup hook; no modern equivalent.
        public bool FixupBogusPublisherMetafile(HENHMETAFILE hMetaFile, uint reserved)
        {
            return hMetaFile.value != IntPtr.Zero;
        }

        [EntryPoint(0x01D1)]
        public bool RectVisible_Ehh(HDC hDC, uint prcRect)
        {
            if (prcRect == 0)
                return false;

            var rc32 = _machine.ReadStruct<Win16.RECT>(prcRect).Convert();
            return RectVisible(hDC, ref rc32);
        }

        [EntryPoint(0x01D2)]
        public bool RectInRegion_Ehh(HGDIOBJ hRgn, uint prcRect)
        {
            if (prcRect == 0)
                return false;

            var rc32 = _machine.ReadStruct<Win16.RECT>(prcRect).Convert();
            return RectInRegion(hRgn, ref rc32);
        }

        [EntryPoint(0x01D3)]
        // Win16 helper for copying UTF-16 input into an ANSI output buffer.
        public short UnicodeToAnsi(uint lpszUnicode, uint lpszAnsi, short cchAnsi)
        {
            if (lpszUnicode == 0)
                return 0;

            var chars = new List<char>();
            for (int i = 0; i < 0x7FFF; i++)
            {
                ushort ch = _machine.ReadWord((uint)(lpszUnicode + i * sizeof(ushort)));
                if (ch == 0)
                    break;
                chars.Add((char)ch);
            }

            var ansiBytes = Machine.AnsiEncoding.GetBytes(chars.ToArray());
            if (lpszAnsi == 0 || cchAnsi <= 0)
                return unchecked((short)ansiBytes.Length);

            int copied = Math.Min(ansiBytes.Length, cchAnsi - 1);
            ushort ansiSeg = lpszAnsi.Hiword();
            ushort ansiOff = lpszAnsi.Loword();
            for (int i = 0; i < copied; i++)
            {
                _machine.WriteByte(ansiSeg, (ushort)(ansiOff + i), ansiBytes[i]);
            }
            _machine.WriteByte(ansiSeg, (ushort)(ansiOff + copied), 0);
            return unchecked((short)copied);
        }

        [EntryPoint(0x01D4)]
        public bool GetBitmapDimensionEx(HGDIOBJ hBitmap, uint lpSize)
        {
            Win32.SIZE size;
            if (!GetBitmapDimensionEx(hBitmap, out size))
                return false;

            if (lpSize != 0)
                _machine.WriteStruct(lpSize, size.Convert());
            return true;
        }

        [EntryPoint(0x01D5)]
        public bool GetBrushOrgEx(HDC hDC, uint lpPoint)
        {
            Win32.POINT point;
            if (!GetBrushOrgEx(hDC, out point))
                return false;

            if (lpPoint != 0)
                _machine.WriteStruct(lpPoint, point.Convert());
            return true;
        }

        [EntryPoint(0x01D6)]
        public bool GetCurrentPositionEx(HDC hDC, uint lpPoint)
        {
            Win32.POINT point;
            if (!GetCurrentPositionEx(hDC, out point))
                return false;

            if (lpPoint != 0)
                _machine.WriteStruct(lpPoint, point.Convert());
            return true;
        }

        [EntryPoint(0x01D7)]
        public bool GetTextExtentPoint(HDC hDC, uint pszString, nint cbString, uint lpSize)
        {
            if (cbString < 0)
                return false;

            var str = _machine.GlobalHeap.ReadCharacters(pszString, cbString);
            Win32.SIZE size;
            if (!GetTextExtentPoint(hDC, str, cbString, out size))
                return false;

            if (lpSize != 0)
                _machine.WriteStruct(lpSize, size.Convert());
            return true;
        }

        [EntryPoint(0x01D8)]
        public bool GetViewportExtEx(HDC hDC, uint lpSize)
        {
            Win32.SIZE size;
            if (!GetViewportExtEx(hDC, out size))
                return false;

            if (lpSize != 0)
                _machine.WriteStruct(lpSize, size.Convert());
            return true;
        }

        [EntryPoint(0x01D9)]
        public bool GetViewportOrgEx(HDC hDC, uint lpPoint)
        {
            Win32.POINT point;
            if (!GetViewportOrgEx(hDC, out point))
                return false;

            if (lpPoint != 0)
                _machine.WriteStruct(lpPoint, point.Convert());
            return true;
        }

        [EntryPoint(0x01DA)]
        public bool GetWindowExtEx(HDC hDC, uint lpSize)
        {
            Win32.SIZE size;
            if (!GetWindowExtEx(hDC, out size))
                return false;

            if (lpSize != 0)
                _machine.WriteStruct(lpSize, size.Convert());
            return true;
        }

        [EntryPoint(0x01DB)]
        public bool GetWindowOrgEx(HDC hDC, uint lpPoint)
        {
            Win32.POINT point;
            if (!GetWindowOrgEx(hDC, out point))
                return false;

            if (lpPoint != 0)
                _machine.WriteStruct(lpPoint, point.Convert());
            return true;
        }

        [EntryPoint(0x01DC)]
        public bool OffsetViewportOrgEx(HDC hDC, short x, short y, uint lpPoint)
        {
            Win32.POINT point;
            if (!OffsetViewportOrgEx(hDC, x, y, out point))
                return false;

            if (lpPoint != 0)
                _machine.WriteStruct(lpPoint, point.Convert());
            return true;
        }

        [EntryPoint(0x01DD)]
        public bool OffsetWindowOrgEx(HDC hDC, short x, short y, uint lpPoint)
        {
            Win32.POINT point;
            if (!OffsetWindowOrgEx(hDC, x, y, out point))
                return false;

            if (lpPoint != 0)
                _machine.WriteStruct(lpPoint, point.Convert());
            return true;
        }

        [EntryPoint(0x01DE)]
        public bool SetBitmapDimensionEx(HGDIOBJ hBitmap, short width, short height, uint lpSize)
        {
            Win32.SIZE size;
            if (!SetBitmapDimensionEx(hBitmap, width, height, out size))
                return false;

            if (lpSize != 0)
                _machine.WriteStruct(lpSize, size.Convert());
            return true;
        }

        [EntryPoint(0x01DF)]
        public bool SetViewportExtEx(HDC hDC, short x, short y, uint lpSize)
        {
            Win32.SIZE size;
            if (!SetViewportExtEx(hDC, x, y, out size))
                return false;

            if (lpSize != 0)
                _machine.WriteStruct(lpSize, size.Convert());
            return true;
        }

        [EntryPoint(0x01E0)]
        public bool SetViewportOrgEx(HDC hDC, short x, short y, uint lpPoint)
        {
            Win32.POINT point;
            if (!SetViewportOrgEx(hDC, x, y, out point))
                return false;

            if (lpPoint != 0)
                _machine.WriteStruct(lpPoint, point.Convert());
            return true;
        }

        [EntryPoint(0x01E1)]
        public bool SetWindowExtEx(HDC hDC, short x, short y, uint lpSize)
        {
            Win32.SIZE size;
            if (!SetWindowExtEx(hDC, x, y, out size))
                return false;

            if (lpSize != 0)
                _machine.WriteStruct(lpSize, size.Convert());
            return true;
        }

        [EntryPoint(0x01E2)]
        public bool SetWindowOrgEx(HDC hDC, short x, short y, uint lpPoint)
        {
            Win32.POINT point;
            if (!SetWindowOrgEx(hDC, x, y, out point))
                return false;

            if (lpPoint != 0)
                _machine.WriteStruct(lpPoint, point.Convert());
            return true;
        }

        [EntryPoint(0x01E3)]
        public bool MoveToEx(HDC hDC, nint x, nint y, uint lpPoint)
        {
            if (lpPoint == 0)
                return MoveToEx(hDC, x, y, IntPtr.Zero);

            IntPtr pPoint = Marshal.AllocHGlobal(Marshal.SizeOf<Win32.POINT>());
            try
            {
                if (!MoveToEx(hDC, x, y, pPoint))
                    return false;

                _machine.WriteStruct(lpPoint, Marshal.PtrToStructure<Win32.POINT>(pPoint).Convert());
                return true;
            }
            finally
            {
                Marshal.FreeHGlobal(pPoint);
            }
        }

        [EntryPoint(0x01E4)]
        public bool ScaleViewportExtEx(HDC hDC, short xNum, short xDenom, short yNum, short yDenom, uint lpSize)
        {
            Win32.SIZE size;
            if (!ScaleViewportExtEx(hDC, xNum, xDenom, yNum, yDenom, out size))
                return false;

            if (lpSize != 0)
                _machine.WriteStruct(lpSize, size.Convert());
            return true;
        }

        [EntryPoint(0x01E5)]
        public bool ScaleWindowExtEx(HDC hDC, short xNum, short xDenom, short yNum, short yDenom, uint lpSize)
        {
            Win32.SIZE size;
            if (!ScaleWindowExtEx(hDC, xNum, xDenom, yNum, yDenom, out size))
                return false;

            if (lpSize != 0)
                _machine.WriteStruct(lpSize, size.Convert());
            return true;
        }

        [EntryPoint(0x01E6)]
        public bool GetAspectRatioFilterEx(HDC hDC, uint lpSize)
        {
            Win32.SIZE size;
            if (!GetAspectRatioFilterEx(hDC, out size))
                return false;

            if (lpSize != 0)
                _machine.WriteStruct(lpSize, size.Convert());
            return true;
        }
    }
}
