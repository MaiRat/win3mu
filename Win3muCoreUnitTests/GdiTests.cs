using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sharp86;
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
                    0x0005, 0x0006, 0x0008, 0x000A, 0x000F, 0x0010, 0x0011, 0x0012, 0x001A, 0x0020, 0x0026, 0x0031, 0x0036, 0x0037, 0x003F, 0x0041, 0x0047, 0x0048, 0x0049,
                    0x0054, 0x0056, 0x0059, 0x005C, 0x005E, 0x005F, 0x0060, 0x0061, 0x0062, 0x0065, 0x0066, 0x0069, 0x006A, 0x0075, 0x0077, 0x007C, 0x007D, 0x007E, 0x0086, 0x0087, 0x0088, 0x0095, 0x0097, 0x009B, 0x009F, 0x00A1, 0x00A2, 0x00A3, 0x00AD, 0x00B5, 0x00C4,
                     0x00AF, 0x00B0, 0x00C1, 0x00C2, 0x00C3,
                     0x015C, 0x015E, 0x0161, 0x0169, 0x016A, 0x016B, 0x016C, 0x016D, 0x016E,
                     0x016F, 0x0170, 0x0172, 0x0175, 0x0176,
                     0x0179, 0x017A, 0x017B, 0x017C, 0x017D, 0x017E, 0x0174, 0x019A,
                    0x01B8, 0x01B9, 0x01BC, 0x01BD, 0x01C1, 0x01C2, 0x01C3,
                    0x01CF, 0x01D0, 0x01D1, 0x01D2, 0x01D3, 0x01D4, 0x01D5, 0x01D6, 0x01D7, 0x01D8, 0x01D9, 0x01DA, 0x01DB, 0x01DC, 0x01DD, 0x01DE, 0x01DF, 0x01E0, 0x01E1, 0x01E2, 0x01E3, 0x01E4, 0x01E5, 0x01E6
                },
                exports);
        }

        [TestMethod]
        public void GdiModule_MapsNewOrdinalsToExpectedNames()
        {
            var gdi = new Gdi();

            Assert.AreEqual("SetRelAbs", gdi.GetNameFromOrdinal(0x0005));
            Assert.AreEqual("SetPolyFillMode", gdi.GetNameFromOrdinal(0x0006));
            Assert.AreEqual("SetTextCharacterExtra", gdi.GetNameFromOrdinal(0x0008));
            Assert.AreEqual("SetTextJustification", gdi.GetNameFromOrdinal(0x000A));
            Assert.AreEqual("OffsetWindowOrg", gdi.GetNameFromOrdinal(0x000F));
            Assert.AreEqual("ScaleWindowExt", gdi.GetNameFromOrdinal(0x0010));
            Assert.AreEqual("OffsetViewportOrg", gdi.GetNameFromOrdinal(0x0011));
            Assert.AreEqual("ScaleViewportExt", gdi.GetNameFromOrdinal(0x0012));
            Assert.AreEqual("Pie", gdi.GetNameFromOrdinal(0x001A));
            Assert.AreEqual("OffsetClipRgn", gdi.GetNameFromOrdinal(0x0020));
            Assert.AreEqual("Escape", gdi.GetNameFromOrdinal(0x0026));
            Assert.AreEqual("CreateBitmapIndirect", gdi.GetNameFromOrdinal(0x0031));
            Assert.AreEqual("CreateEllipticRgn", gdi.GetNameFromOrdinal(0x0036));
            Assert.AreEqual("CreateEllipticRgnIndirect", gdi.GetNameFromOrdinal(0x0037));
            Assert.AreEqual("CreatePolygonRgn", gdi.GetNameFromOrdinal(0x003F));
            Assert.AreEqual("CreateRectRgnIndirect", gdi.GetNameFromOrdinal(0x0041));
            Assert.AreEqual("EnumObjects", gdi.GetNameFromOrdinal(0x0047));
            Assert.AreEqual("EqualRgn", gdi.GetNameFromOrdinal(0x0048));
            Assert.AreEqual("ExcludeVisRect", gdi.GetNameFromOrdinal(0x0049));
            Assert.AreEqual("GetPolyFillMode", gdi.GetNameFromOrdinal(0x0054));
            Assert.AreEqual("GetRelAbs", gdi.GetNameFromOrdinal(0x0056));
            Assert.AreEqual("GetTextCharacterExtra", gdi.GetNameFromOrdinal(0x0059));
            Assert.AreEqual("GetTextFace", gdi.GetNameFromOrdinal(0x005C));
            Assert.AreEqual("GetViewportExt", gdi.GetNameFromOrdinal(0x005E));
            Assert.AreEqual("GetViewportOrg", gdi.GetNameFromOrdinal(0x005F));
            Assert.AreEqual("GetWindowExt", gdi.GetNameFromOrdinal(0x0060));
            Assert.AreEqual("GetWindowOrg", gdi.GetNameFromOrdinal(0x0061));
            Assert.AreEqual("IntersectVisRect", gdi.GetNameFromOrdinal(0x0062));
            Assert.AreEqual("OffsetRgn", gdi.GetNameFromOrdinal(0x0065));
            Assert.AreEqual("OffsetVisRgn", gdi.GetNameFromOrdinal(0x0066));
            Assert.AreEqual("SelectVisRgn", gdi.GetNameFromOrdinal(0x0069));
            Assert.AreEqual("SetBitmapBits", gdi.GetNameFromOrdinal(0x006A));
            Assert.AreEqual("SetDCOrg", gdi.GetNameFromOrdinal(0x0075));
            Assert.AreEqual("AddFontResource", gdi.GetNameFromOrdinal(0x0077));
            Assert.AreEqual("GetMetaFile", gdi.GetNameFromOrdinal(0x007C));
            Assert.AreEqual("CreateMetaFile", gdi.GetNameFromOrdinal(0x007D));
            Assert.AreEqual("CloseMetaFile", gdi.GetNameFromOrdinal(0x007E));
            Assert.AreEqual("GetRgnBox", gdi.GetNameFromOrdinal(0x0086));
            Assert.AreEqual("ScanLR", gdi.GetNameFromOrdinal(0x0087));
            Assert.AreEqual("RemoveFontResource", gdi.GetNameFromOrdinal(0x0088));
            Assert.AreEqual("GetBrushOrg", gdi.GetNameFromOrdinal(0x0095));
            Assert.AreEqual("CopyMetaFile", gdi.GetNameFromOrdinal(0x0097));
            Assert.AreEqual("QueryAbort", gdi.GetNameFromOrdinal(0x009B));
            Assert.AreEqual("GetMetaFileBits", gdi.GetNameFromOrdinal(0x009F));
            Assert.AreEqual("PtInRegion", gdi.GetNameFromOrdinal(0x00A1));
            Assert.AreEqual("GetBitmapDimension", gdi.GetNameFromOrdinal(0x00A2));
            Assert.AreEqual("SetBitmapDimension", gdi.GetNameFromOrdinal(0x00A3));
            Assert.AreEqual("GetClipRgn", gdi.GetNameFromOrdinal(0x00AD));
            Assert.AreEqual("EnumMetaFile", gdi.GetNameFromOrdinal(0x00AF));
            Assert.AreEqual("PlayMetaFileRecord", gdi.GetNameFromOrdinal(0x00B0));
            Assert.AreEqual("RectInRegion", gdi.GetNameFromOrdinal(0x00B5));
            Assert.AreEqual("SetBoundsRect", gdi.GetNameFromOrdinal(0x00C1));
            Assert.AreEqual("GetBoundsRect", gdi.GetNameFromOrdinal(0x00C2));
            Assert.AreEqual("SelectBitmap", gdi.GetNameFromOrdinal(0x00C3));
            Assert.AreEqual("SetMetaFileBitsBetter", gdi.GetNameFromOrdinal(0x00C4));
            Assert.AreEqual("Chord", gdi.GetNameFromOrdinal(0x015C));
            Assert.AreEqual("GetCharWidth", gdi.GetNameFromOrdinal(0x015E));
            Assert.AreEqual("GetAspectRatioFilter", gdi.GetNameFromOrdinal(0x0161));
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
            Assert.AreEqual("CreateDIBPatternBrush", gdi.GetNameFromOrdinal(0x01BD));
            Assert.AreEqual("DeviceColorMatch", gdi.GetNameFromOrdinal(0x01C1));
            Assert.AreEqual("PolyPolygon", gdi.GetNameFromOrdinal(0x01C2));
            Assert.AreEqual("CreatePolyPolygonRgn", gdi.GetNameFromOrdinal(0x01C3));
            Assert.AreEqual("MakeObjectPrivate", gdi.GetNameFromOrdinal(0x01CF));
            Assert.AreEqual("FixupBogusPublisherMetafile", gdi.GetNameFromOrdinal(0x01D0));
            Assert.AreEqual("RectVisible_Ehh", gdi.GetNameFromOrdinal(0x01D1));
            Assert.AreEqual("RectInRegion_Ehh", gdi.GetNameFromOrdinal(0x01D2));
            Assert.AreEqual("UnicodeToAnsi", gdi.GetNameFromOrdinal(0x01D3));
            Assert.AreEqual("GetBitmapDimensionEx", gdi.GetNameFromOrdinal(0x01D4));
            Assert.AreEqual("GetBrushOrgEx", gdi.GetNameFromOrdinal(0x01D5));
            Assert.AreEqual("GetCurrentPositionEx", gdi.GetNameFromOrdinal(0x01D6));
            Assert.AreEqual("GetTextExtentPoint", gdi.GetNameFromOrdinal(0x01D7));
            Assert.AreEqual("GetViewportExtEx", gdi.GetNameFromOrdinal(0x01D8));
            Assert.AreEqual("GetViewportOrgEx", gdi.GetNameFromOrdinal(0x01D9));
            Assert.AreEqual("GetWindowExtEx", gdi.GetNameFromOrdinal(0x01DA));
            Assert.AreEqual("GetWindowOrgEx", gdi.GetNameFromOrdinal(0x01DB));
            Assert.AreEqual("OffsetViewportOrgEx", gdi.GetNameFromOrdinal(0x01DC));
            Assert.AreEqual("OffsetWindowOrgEx", gdi.GetNameFromOrdinal(0x01DD));
            Assert.AreEqual("SetBitmapDimensionEx", gdi.GetNameFromOrdinal(0x01DE));
            Assert.AreEqual("SetViewportExtEx", gdi.GetNameFromOrdinal(0x01DF));
            Assert.AreEqual("SetViewportOrgEx", gdi.GetNameFromOrdinal(0x01E0));
            Assert.AreEqual("SetWindowExtEx", gdi.GetNameFromOrdinal(0x01E1));
            Assert.AreEqual("SetWindowOrgEx", gdi.GetNameFromOrdinal(0x01E2));
            Assert.AreEqual("MoveToEx", gdi.GetNameFromOrdinal(0x01E3));
            Assert.AreEqual("ScaleViewportExtEx", gdi.GetNameFromOrdinal(0x01E4));
            Assert.AreEqual("ScaleWindowExtEx", gdi.GetNameFromOrdinal(0x01E5));
            Assert.AreEqual("GetAspectRatioFilterEx", gdi.GetNameFromOrdinal(0x01E6));
        }

        [TestMethod]
        public void GdiModule_ExportsCommentStubOrdinalsAsMethods()
        {
            var gdi = new Gdi();
            var exports = gdi.GetExports().ToArray();
            var expected = new (ushort Ordinal, string Name)[]
            {
                (0x00C9, "DMBitBlt"),
                (0x00CA, "DMColorInfo"),
                (0x00CE, "DMEnumDFonts"),
                (0x00CF, "DMEnumObj"),
                (0x00D0, "DMOutput"),
                (0x00D1, "DMPixel"),
                (0x00D2, "DMRealizeObject"),
                (0x00D3, "DMStrBlt"),
                (0x00D4, "DMScanLR"),
                (0x00D5, "Brute"),
                (0x00D6, "DMExtTextOut"),
                (0x00D7, "DMGetCharWidth"),
                (0x00D8, "DMStretchBlt"),
                (0x00D9, "DMDibBits"),
                (0x00DA, "DMStretchDibits"),
                (0x00DB, "DMSetDibToDev"),
                (0x00DC, "DMTranspose"),
                (0x00E6, "CreatePQ"),
                (0x00E7, "MinPQ"),
                (0x00E8, "ExtractPQ"),
                (0x00E9, "InsertPQ"),
                (0x00EA, "SizePQ"),
                (0x00EB, "DeletePQ"),
                (0x00F0, "OpenJob"),
                (0x00F1, "WriteSpool"),
                (0x00F2, "WriteDialog"),
                (0x00F3, "CloseJob"),
                (0x00F4, "DeleteJob"),
                (0x00F5, "GetSpoolJob"),
                (0x00F6, "StartSpoolPage"),
                (0x00F7, "EndSpoolPage"),
                (0x00F8, "QueryJob"),
                (0x00FA, "Copy"),
                (0x00FD, "DeleteSpoolPage"),
                (0x00FE, "SpoolFile"),
                (0x012C, "EngineEnumerateFont"),
                (0x012D, "EngineDeleteFont"),
                (0x012E, "EngineRealizeFont"),
                (0x012F, "EngineGetCharWidth"),
                (0x0130, "EngineSetFontContext"),
                (0x0131, "EngineGetGlyphBmp"),
                (0x0132, "EngineMakeFontDir"),
                (0x0133, "GetCharABCWidths"),
                (0x0134, "GetOutlineTextMetrics"),
                (0x0135, "GetGlyphOutline"),
                (0x0136, "CreateScalableFontResource"),
                (0x0137, "GetFontData"),
                (0x0138, "ConvertOutlineFontFile"),
                (0x0139, "GetRasterizerCaps"),
                (0x013A, "EngineExtTextOut"),
                (0x014A, "EnumFontFamilies"),
                (0x014C, "GetKerningPairs"),
                (0x015D, "SetMapperFlags"),
                (0x0160, "GetPhysicalFontHandle"),
                (0x0162, "ShrinkGDIHeap"),
                (0x0163, "FTrapping0"),
                (0x0178, "ResetDC"),
                (0x0190, "FastWindowFrame"),
                (0x0191, "GdiMoveBitmap"),
                (0x0193, "GdiInit2"),
                (0x0195, "FinalGdiInit"),
                (0x0197, "CreateUserBitmap"),
                (0x0199, "CreateUserDiscardableBitmap"),
                (0x019B, "GetCurLogFont"),
                (0x019C, "IsDCCurrentPalette"),
            };

            CollectionAssert.IsSubsetOf(expected.Select(x => x.Ordinal).ToArray(), exports);

            foreach (var entry in expected)
            {
                Assert.AreEqual(entry.Name, gdi.GetNameFromOrdinal(entry.Ordinal));
            }
        }

        [TestMethod]
        public void GdiModule_RelAbsCompatibilityStateRoundsTrip()
        {
            var gdi = new Gdi();

            Assert.AreEqual((ushort)0, gdi.GetRelAbs());
            Assert.AreEqual((ushort)0, gdi.SetRelAbs(1));
            Assert.AreEqual((ushort)1, gdi.GetRelAbs());
            Assert.AreEqual((ushort)1, gdi.SetRelAbs(0));
            Assert.AreEqual((ushort)0, gdi.GetRelAbs());
        }

        [TestMethod]
        public void GdiModule_LegacySpoolApisMaintainCompatibilityState()
        {
            var machine = new Machine();
            var gdi = machine.ModuleManager.GetModule("GDI") as Gdi;

            Assert.IsNotNull(gdi);

            uint rawData = Alloc(machine, "Spool Data", 4);
            machine.WriteBytes(rawData, new byte[] { 1, 2, 3, 4 });

            short hJob = gdi.OpenJob(machine.StringHeap.GetString("HP LaserJet"), machine.StringHeap.GetString("Test Document"), 0);
            Assert.IsTrue(hJob > 0);
            Assert.AreEqual((short)4, gdi.WriteSpool((ushort)hJob, rawData, 4));
            Assert.AreEqual((short)4, gdi.WriteDialog((ushort)hJob, machine.StringHeap.GetString("Page"), 4));
            Assert.AreEqual((ushort)1, gdi.StartSpoolPage((ushort)hJob));
            Assert.AreEqual((nint)2, gdi.QueryJob((ushort)hJob, 0));
            Assert.AreEqual((nint)8, gdi.GetSpoolJob((ushort)hJob, 0));
            Assert.AreEqual((ushort)1, gdi.DeleteSpoolPage((ushort)hJob));
            Assert.AreEqual((nint)8, gdi.GetSpoolJob((ushort)hJob, 0));
            Assert.AreEqual((nint)1, gdi.QueryJob((ushort)hJob, 0));
            Assert.AreEqual((ushort)1, gdi.CloseJob((ushort)hJob));
            Assert.AreEqual((short)0, gdi.WriteSpool((ushort)hJob, rawData, 4));
        }

        [TestMethod]
        public void GdiModule_LegacyCopyExportCopiesGuestMemory()
        {
            var machine = new Machine();
            var gdi = machine.ModuleManager.GetModule("GDI") as Gdi;

            Assert.IsNotNull(gdi);

            uint source = Alloc(machine, "Copy Source", 4);
            uint dest = Alloc(machine, "Copy Dest", 4);
            machine.WriteBytes(source, new byte[] { 0x10, 0x20, 0x30, 0x40 });

            Assert.AreEqual((ushort)1, gdi.Copy(source, dest, 4));
            CollectionAssert.AreEqual(new byte[] { 0x10, 0x20, 0x30, 0x40 }, machine.ReadBytes(dest, 4));
            Assert.AreEqual((ushort)1, gdi.Copy(source, dest, 0));
            Assert.AreEqual((ushort)0, gdi.Copy(0, dest, 1));
        }

        static uint Alloc(Machine machine, string name, uint size)
        {
            return BitUtils.MakeDWord(0, machine.GlobalHeap.Alloc(name, 0, size));
        }
    }
}
