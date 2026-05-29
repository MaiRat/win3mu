using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
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

        [TestMethod]
        public void Validate_SampleExecutable_ProducesExecutionReportAndSymbolMap()
        {
            var validator = new LoaderValidator();

            var report = validator.Validate(GetRepositoryFile("Samples", "alarm.exe"));

            Assert.AreEqual(1, report.FilesProcessed);
            var result = report.Results[0];
            Assert.IsTrue(result.Success);
            Assert.IsNotNull(result.Execution);
            Assert.IsTrue(result.Execution.Attempted);
            Assert.IsTrue(result.Execution.InstructionsExecuted > 0);
            Assert.IsTrue(result.Execution.ReachedInstructionLimit || result.Execution.Aborted || !string.IsNullOrEmpty(result.Execution.StopReason));
            Assert.IsTrue(result.Symbols.Exists(x => x.Name == "start"));
        }

        [TestMethod]
        public void Validate_SamplesDirectory_ProcessesAllSamplesSuccessfully()
        {
            var validator = new LoaderValidator();

            var report = validator.Validate(GetRepositoryFile("Samples"));

            Assert.AreEqual(report.FilesDiscovered, report.FilesProcessed);
            Assert.AreEqual(0, report.FailureCount);
            Assert.IsTrue(report.Results.All(x => x.Success));
        }

        [TestMethod]
        public void Validate_TestCtlSample_DoesNotStopWithInvalidOpcode()
        {
            var validator = new LoaderValidator();

            var report = validator.Validate(GetRepositoryFile("Samples", "testctl.exe"));

            Assert.AreEqual(1, report.FilesProcessed);
            var result = report.Results[0];
            Assert.IsTrue(result.Success);
            Assert.IsNotNull(result.Execution);
            Assert.IsFalse((result.Execution.StopReason ?? string.Empty).Contains("InvalidOpCodeException", StringComparison.InvariantCulture));
        }

        [TestMethod]
        public void WatzeeSample_DiceBitmapResources_CanBeReadAndRemainDistinct()
        {
            using (var neFile = new NeFileReader(GetRepositoryFile("Samples", "watzee.exe")))
            {
                var bitmapType = neFile.FindResourceType(Win16.ResourceType.RT_BITMAP.ToString());

                Assert.IsNotNull(bitmapType);
                CollectionAssert.AreEqual(
                    new[] { "DICE1", "DICE2", "DICE3", "DICE4", "DICE5", "DICE6" },
                    bitmapType.resources.Select(x => x.name).ToArray());

                var hashes = bitmapType.resources.Select(resource =>
                {
                    var data = neFile.LoadResource(resource);
                    Assert.IsNotNull(data);
                    Assert.AreEqual(336, data.Length);
                    AssertBitmapHeader(data, 36, 36, 1);
                    return Convert.ToHexString(SHA256.HashData(data));
                }).ToArray();

                Assert.AreEqual(hashes.Length, hashes.Distinct(StringComparer.Ordinal).Count());
            }
        }

        [TestMethod]
        public void WatzeeSample_IconMenuAndDialogResources_CanBeResolvedForDisplay()
        {
            using (var neFile = new NeFileReader(GetRepositoryFile("Samples", "watzee.exe")))
            {
                var menuType = neFile.FindResourceType(Win16.ResourceType.RT_MENU.ToString());
                Assert.IsNotNull(menuType);
                CollectionAssert.AreEqual(new[] { "WATZEE" }, menuType.resources.Select(x => x.name).ToArray());
                CollectionAssert.AreEqual(new[] { 98 }, menuType.resources.Select(x => x.length).ToArray());

                var dialogType = neFile.FindResourceType(Win16.ResourceType.RT_DIALOG.ToString());
                Assert.IsNotNull(dialogType);
                CollectionAssert.AreEqual(
                    new[] { "GETNUMPLAYERS", "GETPLAYERSINITIALS", "ABOUTWATZEE", "OPTIONS", "WATZEEHELP" },
                    dialogType.resources.Select(x => x.name).ToArray());
                Assert.IsTrue(dialogType.resources.All(x => neFile.LoadResource(x).Length > 0));

                var iconGroupType = neFile.FindResourceType(Win16.ResourceType.RT_GROUP_ICON.ToString());
                Assert.IsNotNull(iconGroupType);
                CollectionAssert.AreEqual(new[] { "WATZEE" }, iconGroupType.resources.Select(x => x.name).ToArray());

                var iconGroup = Resources.LoadIconOrCursorGroup(neFile.GetResourceStream(iconGroupType.name, "WATZEE"));
                Assert.AreEqual(0, iconGroup.Directory.idReserved);
                Assert.AreEqual(1, iconGroup.Directory.idType);
                Assert.AreEqual(1, iconGroup.Entries.Count);

                var iconEntry = iconGroup.Entries[0];
                Assert.AreEqual(32, iconEntry.bWidth);
                Assert.AreEqual(32, iconEntry.bHeight);
                Assert.AreEqual(4, iconEntry.wBitCount);

                var icon = neFile.LoadResource(Win16.ResourceType.RT_ICON.ToString(), $"#{iconEntry.nId}");
                Assert.IsNotNull(icon);
                Assert.AreEqual((int)iconEntry.dwBytesInRes, icon.Length);
                AssertBitmapHeader(icon, 32, 64, 4);
            }
        }

        static string CreateTempDirectory()
        {
            var path = Path.Combine(Path.GetTempPath(), "win3mu-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return path;
        }

        static string GetRepositoryFile(params string[] relativePath)
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current != null)
            {
                if (File.Exists(Path.Combine(current.FullName, "Win3mu.sln")))
                    return Path.Combine(new[] { current.FullName }.Concat(relativePath).ToArray());

                current = current.Parent;
            }

            throw new DirectoryNotFoundException("Unable to locate repository root from the test output directory.");
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

        static void AssertBitmapHeader(byte[] data, int width, int height, short bitCount)
        {
            Assert.IsTrue(data.Length >= 16);
            Assert.AreEqual(40, BitConverter.ToInt32(data, 0));
            Assert.AreEqual(width, BitConverter.ToInt32(data, 4));
            Assert.AreEqual(height, BitConverter.ToInt32(data, 8));
            Assert.AreEqual(1, BitConverter.ToInt16(data, 12));
            Assert.AreEqual(bitCount, BitConverter.ToInt16(data, 14));
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
