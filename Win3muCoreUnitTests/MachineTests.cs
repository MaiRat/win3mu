using System;
using System.IO;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Win3muCore;

namespace Win3muCoreUnitTests
{
    [TestClass]
    public class MachineTests
    {
        [TestMethod]
        public void ConfigureLaunchWorkingDirectory_UsesMappedCurrentDirectoryForRelativeFileAccess()
        {
            var tempRoot = CreateTempDirectory();
            var originalCurrentDirectory = Environment.CurrentDirectory;
            try
            {
                var exeDirectory = Path.Combine(tempRoot, "exe");
                var launchDirectory = Path.Combine(tempRoot, "launch");
                Directory.CreateDirectory(exeDirectory);
                Directory.CreateDirectory(launchDirectory);

                var machine = new Machine();
                machine.PathMapper.AddMount(@"C:\TEST", exeDirectory, exeDirectory);
                machine.PathMapper.AddMount(@"D:\START", launchDirectory, launchDirectory);

                Environment.CurrentDirectory = launchDirectory;
                InvokeConfigureLaunchWorkingDirectory(machine, @"C:\TEST\TEST.EXE");

                Assert.AreEqual(NormalizeDirectoryPath(launchDirectory), NormalizeDirectoryPath(machine.PathMapper.TryMapGuestToHost(machine.Dos.WorkingDirectory, false)));
                Assert.AreEqual(Path.Combine(launchDirectory, "DATA.DAT"), machine.PathMapper.TryMapGuestToHost(machine.Dos.QualifyPath("DATA.DAT"), false));
            }
            finally
            {
                Environment.CurrentDirectory = originalCurrentDirectory;
                if (Directory.Exists(tempRoot))
                    Directory.Delete(tempRoot, true);
            }
        }

        [TestMethod]
        public void ConfigureLaunchWorkingDirectory_AddsSyntheticGuestDriveForUnmappedCurrentDirectory()
        {
            var tempRoot = CreateTempDirectory();
            var originalCurrentDirectory = Environment.CurrentDirectory;
            try
            {
                var exeDirectory = Path.Combine(tempRoot, "exe");
                var launchDirectory = Path.Combine(tempRoot, "launch");
                Directory.CreateDirectory(exeDirectory);
                Directory.CreateDirectory(launchDirectory);

                var machine = new Machine();
                machine.PathMapper.AddMount(@"C:\TEST", exeDirectory, exeDirectory);

                Environment.CurrentDirectory = launchDirectory;
                InvokeConfigureLaunchWorkingDirectory(machine, @"C:\TEST\TEST.EXE");

                StringAssert.Matches(machine.Dos.WorkingDirectory, new System.Text.RegularExpressions.Regex(@"^[A-Y]:\\$"));
                Assert.AreEqual(NormalizeDirectoryPath(launchDirectory), NormalizeDirectoryPath(machine.PathMapper.TryMapGuestToHost(machine.Dos.WorkingDirectory, false)));
                Assert.AreEqual(Path.Combine(launchDirectory, "DATA.DAT"), machine.PathMapper.TryMapGuestToHost(machine.Dos.QualifyPath("DATA.DAT"), false));
            }
            finally
            {
                Environment.CurrentDirectory = originalCurrentDirectory;
                if (Directory.Exists(tempRoot))
                    Directory.Delete(tempRoot, true);
            }
        }

        static void InvokeConfigureLaunchWorkingDirectory(Machine machine, string programName16)
        {
            var method = typeof(Machine).GetMethod("ConfigureLaunchWorkingDirectory", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method);
            method.Invoke(machine, new object[] { programName16 });
        }

        static string CreateTempDirectory()
        {
            var path = Path.Combine(Path.GetTempPath(), "win3mu-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return path;
        }

        static string NormalizeDirectoryPath(string path)
        {
            return path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
    }
}
