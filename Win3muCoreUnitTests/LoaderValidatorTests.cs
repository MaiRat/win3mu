using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Win3muCore;
using Win3muCore.NeFile;
using Win3muCore.Validation;

namespace Win3muCoreUnitTests
{
    [TestClass]
    public class LoaderValidatorTests
    {
        [TestMethod]
        public void Validate_SingleValidExecutable_ReturnsSuccess()
        {
            var tempRoot = CreateTempDirectory();
            try
            {
                var filePath = Path.Combine(tempRoot, "sample.exe");
                WriteMinimalNeFile(filePath, "MINAPP", false);

                var validator = new LoaderValidator();
                var report = validator.Validate(filePath);

                Assert.AreEqual(1, report.FilesDiscovered);
                Assert.AreEqual(1, report.FilesProcessed);
                Assert.AreEqual(1, report.SuccessCount);
                Assert.AreEqual(0, report.FailureCount);

                var result = report.Results[0];
                Assert.IsTrue(result.Success);
                Assert.AreEqual("MINAPP", result.ModuleName);
                Assert.AreEqual(0, result.FixupCount);
                Assert.AreEqual(0, result.ReferencedModules.Count);
            }
            finally
            {
                Directory.Delete(tempRoot, true);
            }
        }

        [TestMethod]
        public void Validate_DirectoryProcessesExeAndDllRecursively()
        {
            var tempRoot = CreateTempDirectory();
            try
            {
                var nested = Path.Combine(tempRoot, "nested");
                Directory.CreateDirectory(nested);

                WriteMinimalNeFile(Path.Combine(nested, "valid.dll"), "VALIDDLL", true);
                File.WriteAllText(Path.Combine(tempRoot, "broken.exe"), "not an NE executable");
                File.WriteAllText(Path.Combine(tempRoot, "ignore.txt"), "ignored");

                var validator = new LoaderValidator();
                var report = validator.Validate(tempRoot);

                Assert.AreEqual(2, report.FilesDiscovered);
                Assert.AreEqual(2, report.FilesProcessed);
                Assert.AreEqual(1, report.SuccessCount);
                Assert.AreEqual(1, report.FailureCount);
                Assert.IsTrue(report.Results.Exists(x => x.Success && x.ModuleName == "VALIDDLL"));
                Assert.IsTrue(report.Results.Exists(x => !x.Success && x.FilePath.EndsWith("broken.exe", StringComparison.InvariantCultureIgnoreCase)));
            }
            finally
            {
                Directory.Delete(tempRoot, true);
            }
        }

        [TestMethod]
        public void TryMapGuestToHost_UsesPlatformDirectorySeparators()
        {
            var tempRoot = CreateTempDirectory();
            try
            {
                var pathMapper = new PathMapper(null);
                pathMapper.AddMount(@"C:\INPUT", tempRoot, tempRoot);

                var mapped = pathMapper.TryMapGuestToHost(@"C:\INPUT\FOLDER\FILE.EXE", false);

                Assert.AreEqual(Path.Combine(tempRoot, "FOLDER", "FILE.EXE"), mapped);
            }
            finally
            {
                Directory.Delete(tempRoot, true);
            }
        }

        static string CreateTempDirectory()
        {
            var path = Path.Combine(Path.GetTempPath(), "win3mu-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return path;
        }

        static void WriteMinimalNeFile(string filePath, string moduleName, bool isDll)
        {
            const int mzHeaderOffset = 0x40;
            const int neHeaderOffset = 0x40;
            var entryTableOffset = Marshal.SizeOf(typeof(NeHeader));
            var residentTableOffset = entryTableOffset + 1;

            var residentNameBytes = Encoding.ASCII.GetBytes(moduleName);
            var nonResidentOffset = mzHeaderOffset + residentTableOffset + 1 + residentNameBytes.Length + 2 + 1;

            using (var stream = File.Create(filePath))
            using (var writer = new BinaryWriter(stream, Encoding.ASCII))
            {
                WriteStruct(writer, new MzHeader()
                {
                    signature = MzHeader.SIGNATURE,
                    offsetNEHeader = neHeaderOffset,
                });

                writer.Write(new byte[mzHeaderOffset - stream.Position]);

                WriteStruct(writer, new NeHeader()
                {
                    signature = NeHeader.SIGNATURE,
                    MajLinkerVersion = 5,
                    EntryTableOffset = (ushort)entryTableOffset,
                    EntryTableLength = 1,
                    ApplFlags = isDll ? AppFlags.DLL : AppFlags.WinPMCompat,
                    SegCount = 0,
                    ModRefs = 0,
                    NoResNamesTabSiz = 0,
                    SegTableOffset = (ushort)entryTableOffset,
                    ResTableOffset = (ushort)residentTableOffset,
                    ResidNamTable = (ushort)residentTableOffset,
                    ModRefTable = (ushort)residentTableOffset,
                    ImportNameTable = (ushort)residentTableOffset,
                    OffStartNonResTab = (uint)nonResidentOffset,
                    MovEntryCount = 0,
                    FileAlnSzShftCnt = 0,
                    nResTabEntries = 0,
                    targOS = TargetOS.Win,
                    expctwinver = 0x030A,
                });

                writer.Write((byte)0);
                writer.Write((byte)residentNameBytes.Length);
                writer.Write(residentNameBytes);
                writer.Write((ushort)0);
                writer.Write((byte)0);
            }
        }

        static void WriteStruct<T>(BinaryWriter writer, T value) where T : struct
        {
            var size = Marshal.SizeOf(typeof(T));
            var buffer = new byte[size];
            var ptr = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr(value, ptr, false);
                Marshal.Copy(ptr, buffer, 0, size);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }

            writer.Write(buffer);
        }
    }
}
