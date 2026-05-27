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
                    0x0020, 0x0036, 0x0037, 0x003F, 0x0041, 0x0048,
                    0x0065, 0x0069, 0x0086, 0x00A1, 0x00AD, 0x00B5,
                    0x0169, 0x016A, 0x016B, 0x016C, 0x016D, 0x016E,
                    0x016F, 0x0170, 0x0172, 0x0175, 0x0176,
                    0x0174, 0x01BC
                },
                exports);
        }

        [TestMethod]
        public void GdiModule_MapsNewOrdinalsToExpectedNames()
        {
            var gdi = new Gdi();

            Assert.AreEqual("OffsetClipRgn", gdi.GetNameFromOrdinal(0x0020));
            Assert.AreEqual("CreateEllipticRgn", gdi.GetNameFromOrdinal(0x0036));
            Assert.AreEqual("CreateEllipticRgnIndirect", gdi.GetNameFromOrdinal(0x0037));
            Assert.AreEqual("CreatePolygonRgn", gdi.GetNameFromOrdinal(0x003F));
            Assert.AreEqual("CreateRectRgnIndirect", gdi.GetNameFromOrdinal(0x0041));
            Assert.AreEqual("EqualRgn", gdi.GetNameFromOrdinal(0x0048));
            Assert.AreEqual("OffsetRgn", gdi.GetNameFromOrdinal(0x0065));
            Assert.AreEqual("SelectVisRgn", gdi.GetNameFromOrdinal(0x0069));
            Assert.AreEqual("GetRgnBox", gdi.GetNameFromOrdinal(0x0086));
            Assert.AreEqual("PtInRegion", gdi.GetNameFromOrdinal(0x00A1));
            Assert.AreEqual("GetClipRgn", gdi.GetNameFromOrdinal(0x00AD));
            Assert.AreEqual("RectInRegion", gdi.GetNameFromOrdinal(0x00B5));
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
            Assert.AreEqual("CreateRoundRectRgn", gdi.GetNameFromOrdinal(0x01BC));
        }
    }
}
