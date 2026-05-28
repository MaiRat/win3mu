using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Win3muCore;
using Win3muCore.NeFile;

namespace Win3muCoreUnitTests
{
    [TestClass]
    public class ModuleManagerTests
    {
        [TestMethod]
        public void GetModule_ZeroHandle_ReturnsLoadedExecutableModule()
        {
            var tempRoot = CreateTempDirectory();
            try
            {
                var filePath = Path.Combine(tempRoot, "sample.exe");
                WriteMinimalNeFile(filePath, "MINAPP", false);
                var machine = new Machine();
                var module = new Module16(filePath);
                module.hModule = 1;
                var loadedModulesField = typeof(ModuleManager).GetField("_loadedModules", BindingFlags.Instance | BindingFlags.NonPublic);

                Assert.IsNotNull(loadedModulesField);

                var loadedModules = (Dictionary<string, ModuleBase>)loadedModulesField.GetValue(machine.ModuleManager);

                loadedModules[module.GetModuleName()] = module;

                var resolved = machine.ModuleManager.GetModule(0);

                Assert.AreSame(module, resolved);
            }
            finally
            {
                if (Directory.Exists(tempRoot))
                    Directory.Delete(tempRoot, true);
            }
        }

        [TestMethod]
        public void LoadModuleInternalForValidation_DoesNotReuseNativeModuleFromDifferentDirectory()
        {
            var tempRoot = CreateTempDirectory();
            try
            {
                var firstDirectory = Path.Combine(tempRoot, "first");
                var secondDirectory = Path.Combine(tempRoot, "second");
                Directory.CreateDirectory(firstDirectory);
                Directory.CreateDirectory(secondDirectory);

                WriteMinimalNeFile(Path.Combine(firstDirectory, "pbrush.dll"), "PBRUSHA", true);
                WriteMinimalNeFile(Path.Combine(secondDirectory, "pbrush.dll"), "PBRUSH", true);

                var machine = new Machine();
                machine.PathMapper.AddMount(@"C:\FIRST", firstDirectory, firstDirectory);
                machine.PathMapper.AddMount(@"C:\SECOND", secondDirectory, secondDirectory);

                var loadModuleInternalForValidation = typeof(ModuleManager).GetMethod("LoadModuleInternalForValidation", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(loadModuleInternalForValidation);

                var firstModule = (Module16)loadModuleInternalForValidation.Invoke(machine.ModuleManager, new object[] { @"C:\FIRST\PBRUSH.DLL", null });
                var secondModule = (Module16)loadModuleInternalForValidation.Invoke(machine.ModuleManager, new object[] { "PBRUSH.DLL", @"C:\SECOND" });

                Assert.AreNotSame(firstModule, secondModule);
                Assert.AreEqual("PBRUSHA", firstModule.GetModuleName());
                Assert.AreEqual("PBRUSH", secondModule.GetModuleName());
                Assert.AreEqual(@"C:\FIRST\PBRUSH.DLL", firstModule.GetModuleFileName());
                Assert.AreEqual(@"C:\SECOND\PBRUSH.DLL", secondModule.GetModuleFileName());
            }
            finally
            {
                if (Directory.Exists(tempRoot))
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

                if (stream.Position > mzHeaderOffset)
                    throw new InvalidOperationException("Minimal NE header exceeded expected MZ header padding.");

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
