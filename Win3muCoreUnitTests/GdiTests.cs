using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Win3muCore;

namespace Win3muCoreUnitTests
{
    [TestClass]
    public class GdiTests
    {
        [TestMethod]
        public void GdiModule_ExportsNewRegionAndDrawingOrdinals()
        {
            var gdi = new Gdi();
            var exports = gdi.GetExports().ToArray();

            CollectionAssert.IsSubsetOf(
                new ushort[]
                {
                    0x0006, 0x0008, 0x000A, 0x001A, 0x0020, 0x0026, 0x0031, 0x0036, 0x0037, 0x003F, 0x0041, 0x0048,
                     0x0065, 0x0069, 0x007C, 0x007D, 0x007E, 0x0086, 0x0097, 0x009B, 0x009F, 0x00A1, 0x00AD, 0x00B5, 0x00C4,
                     0x00AF, 0x00B0, 0x00C1, 0x00C2, 0x00C3,
                     0x0169, 0x016A, 0x016B, 0x016C, 0x016D, 0x016E,
                     0x016F, 0x0170, 0x0172, 0x0175, 0x0176,
                     0x0179, 0x017A, 0x017B, 0x017C, 0x017D, 0x017E, 0x0174, 0x019A,
                    0x01B8, 0x01B9, 0x01BC, 0x01C2
                },
                exports);
        }

        [TestMethod]
        public void GdiModule_MapsNewOrdinalsToExpectedNames()
        {
            var gdi = new Gdi();

            Assert.AreEqual("SetPolyFillMode", gdi.GetNameFromOrdinal(0x0006));
            Assert.AreEqual("SetTextCharacterExtra", gdi.GetNameFromOrdinal(0x0008));
            Assert.AreEqual("SetTextJustification", gdi.GetNameFromOrdinal(0x000A));
            Assert.AreEqual("Pie", gdi.GetNameFromOrdinal(0x001A));
            Assert.AreEqual("OffsetClipRgn", gdi.GetNameFromOrdinal(0x0020));
            Assert.AreEqual("Escape", gdi.GetNameFromOrdinal(0x0026));
            Assert.AreEqual("CreateBitmapIndirect", gdi.GetNameFromOrdinal(0x0031));
            Assert.AreEqual("CreateEllipticRgn", gdi.GetNameFromOrdinal(0x0036));
            Assert.AreEqual("CreateEllipticRgnIndirect", gdi.GetNameFromOrdinal(0x0037));
            Assert.AreEqual("CreatePolygonRgn", gdi.GetNameFromOrdinal(0x003F));
            Assert.AreEqual("CreateRectRgnIndirect", gdi.GetNameFromOrdinal(0x0041));
            Assert.AreEqual("EqualRgn", gdi.GetNameFromOrdinal(0x0048));
            Assert.AreEqual("OffsetRgn", gdi.GetNameFromOrdinal(0x0065));
            Assert.AreEqual("SelectVisRgn", gdi.GetNameFromOrdinal(0x0069));
            Assert.AreEqual("GetMetaFile", gdi.GetNameFromOrdinal(0x007C));
            Assert.AreEqual("CreateMetaFile", gdi.GetNameFromOrdinal(0x007D));
            Assert.AreEqual("CloseMetaFile", gdi.GetNameFromOrdinal(0x007E));
            Assert.AreEqual("GetRgnBox", gdi.GetNameFromOrdinal(0x0086));
            Assert.AreEqual("CopyMetaFile", gdi.GetNameFromOrdinal(0x0097));
            Assert.AreEqual("QueryAbort", gdi.GetNameFromOrdinal(0x009B));
            Assert.AreEqual("GetMetaFileBits", gdi.GetNameFromOrdinal(0x009F));
            Assert.AreEqual("PtInRegion", gdi.GetNameFromOrdinal(0x00A1));
            Assert.AreEqual("GetClipRgn", gdi.GetNameFromOrdinal(0x00AD));
            Assert.AreEqual("EnumMetaFile", gdi.GetNameFromOrdinal(0x00AF));
            Assert.AreEqual("PlayMetaFileRecord", gdi.GetNameFromOrdinal(0x00B0));
            Assert.AreEqual("RectInRegion", gdi.GetNameFromOrdinal(0x00B5));
            Assert.AreEqual("SetBoundsRect", gdi.GetNameFromOrdinal(0x00C1));
            Assert.AreEqual("GetBoundsRect", gdi.GetNameFromOrdinal(0x00C2));
            Assert.AreEqual("SelectBitmap", gdi.GetNameFromOrdinal(0x00C3));
            Assert.AreEqual("SetMetaFileBitsBetter", gdi.GetNameFromOrdinal(0x00C4));
            Assert.AreEqual("SelectPalette", gdi.GetNameFromOrdinal(0x0169));
            Assert.AreEqual("RealizePalette", gdi.GetNameFromOrdinal(0x016A));
            Assert.AreEqual("GetPaletteEntries", gdi.GetNameFromOrdinal(0x016B));
            Assert.AreEqual("SetPaletteEntries", gdi.GetNameFromOrdinal(0x016C));
            Assert.AreEqual("RealizeDefaultPalette", gdi.GetNameFromOrdinal(0x016D));
            Assert.AreEqual("UpdateColors", gdi.GetNameFromOrdinal(0x016E));
            Assert.AreEqual("AnimatePalette", gdi.GetNameFromOrdinal(0x016F));
            Assert.AreEqual("ResizePalette", gdi.GetNameFromOrdinal(0x0170));
            Assert.AreEqual("GetNearestPaletteIndex", gdi.GetNameFromOrdinal(0x0172));
            Assert.AreEqual("ExtFloodFill", gdi.GetNameFromOrdinal(0x0174));
            Assert.AreEqual("SetSystemPaletteUse", gdi.GetNameFromOrdinal(0x0175));
            Assert.AreEqual("GetSystemPaletteUse", gdi.GetNameFromOrdinal(0x0176));
            Assert.AreEqual("StartDoc", gdi.GetNameFromOrdinal(0x0179));
            Assert.AreEqual("EndDoc", gdi.GetNameFromOrdinal(0x017A));
            Assert.AreEqual("StartPage", gdi.GetNameFromOrdinal(0x017B));
            Assert.AreEqual("EndPage", gdi.GetNameFromOrdinal(0x017C));
            Assert.AreEqual("SetAbortProc", gdi.GetNameFromOrdinal(0x017D));
            Assert.AreEqual("AbortDoc", gdi.GetNameFromOrdinal(0x017E));
            Assert.AreEqual("IsValidMetaFile", gdi.GetNameFromOrdinal(0x019A));
            Assert.AreEqual("SetDIBits", gdi.GetNameFromOrdinal(0x01B8));
            Assert.AreEqual("GetDIBits", gdi.GetNameFromOrdinal(0x01B9));
            Assert.AreEqual("CreateRoundRectRgn", gdi.GetNameFromOrdinal(0x01BC));
            Assert.AreEqual("PolyPolygon", gdi.GetNameFromOrdinal(0x01C2));
        }
    }
}
