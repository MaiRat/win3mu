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
        public void AppFolderVariable_IsEmptyBeforeProgramHostPathIsAssigned()
        {
            var machine = new Machine();

            Assert.AreEqual(string.Empty, machine.VariableResolver.Resolve("$(AppFolder)"));
            Assert.AreEqual(string.Empty, machine.VariableResolver.Resolve("$(AppName)"));
        }

        [TestMethod]
        public void AppFolderVariable_UsesProgramHostPathAssignedAfterConstruction()
        {
            var tempRoot = CreateTempDirectory();
            try
            {
                var machine = new Machine();
                var programHostPath = Path.Combine(tempRoot, "PROGRAM.EXE");
                machine.ProgramHostPath = programHostPath;

                Assert.AreEqual(Path.GetDirectoryName(programHostPath), machine.VariableResolver.Resolve("$(AppFolder)"));
                Assert.AreEqual("PROGRAM", machine.VariableResolver.Resolve("$(AppName)"));
            }
            finally
            {
                if (Directory.Exists(tempRoot))
                    Directory.Delete(tempRoot, true);
            }
        }

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

                StringAssert.Matches(machine.Dos.WorkingDirectory, new System.Text.RegularExpressions.Regex(@"^[C-Y]:\\$"));
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
        public void ConfigureMandatoryCDriveRoot_RequiresRootPath()
        {
            var machine = new Machine();
            try
            {
                InvokeConfigureMandatoryCDriveRoot(machine);
                Assert.Fail("Expected ConfigureMandatoryCDriveRoot to throw.");
            }
            catch (TargetInvocationException ex)
            {
                Assert.IsInstanceOfType(ex.InnerException, typeof(InvalidOperationException));
                StringAssert.Contains(ex.InnerException.Message, "/root:<path>");
                StringAssert.DoesNotContain(ex.InnerException.Message, "config setting");
            }
        }

        [TestMethod]
        public void ConfigureMandatoryCDriveRoot_CreatesDefaultStructureForMissingRoot()
        {
            var tempRoot = CreateTempDirectory();
            var cDriveRoot = Path.Combine(tempRoot, "drive-c");
            try
            {
                var machine = new Machine();
                machine.CDriveRoot = cDriveRoot;

                InvokeConfigureMandatoryCDriveRoot(machine);

                Assert.IsTrue(Directory.Exists(cDriveRoot));
                Assert.IsTrue(Directory.Exists(Path.Combine(cDriveRoot, "WINDOWS")));
                Assert.IsTrue(Directory.Exists(Path.Combine(cDriveRoot, "WINDOWS", "SYSTEM")));
                Assert.IsTrue(Directory.Exists(Path.Combine(cDriveRoot, "DOS")));
                Assert.IsTrue(Directory.Exists(Path.Combine(cDriveRoot, "TEMP")));
                StringAssert.Contains(File.ReadAllText(Path.Combine(cDriveRoot, "WINDOWS", "WIN.INI")), "[windows]");
                StringAssert.Contains(File.ReadAllText(Path.Combine(cDriveRoot, "WINDOWS", "SYSTEM.INI")), "[boot]");
                StringAssert.Contains(File.ReadAllText(Path.Combine(cDriveRoot, "AUTOEXEC.BAT")), "SET TEMP=C:\\TEMP");
                Assert.AreEqual(cDriveRoot, machine.mountPoints[@"C:\"].host);
                Assert.AreEqual(Path.Combine(cDriveRoot, "WINDOWS"), machine.mountPoints[@"C:\WINDOWS"].host);
            }
            finally
            {
                if (Directory.Exists(tempRoot))
                    Directory.Delete(tempRoot, true);
            }
        }

        [TestMethod]
        public void ConfigureMandatoryCDriveRoot_DoesNotInitializeExistingRoot()
        {
            var tempRoot = CreateTempDirectory();
            var cDriveRoot = Path.Combine(tempRoot, "drive-c");
            try
            {
                Directory.CreateDirectory(cDriveRoot);

                var machine = new Machine();
                machine.CDriveRoot = cDriveRoot;

                InvokeConfigureMandatoryCDriveRoot(machine);

                Assert.IsFalse(Directory.Exists(Path.Combine(cDriveRoot, "WINDOWS")));
                Assert.AreEqual(cDriveRoot, machine.mountPoints[@"C:\"].host);
            }
            finally
            {
                if (Directory.Exists(tempRoot))
                    Directory.Delete(tempRoot, true);
            }
        }

        [TestMethod]
        public void RootGuestMount_MapsChildDirectoriesUnderMountedRoot()
        {
            var tempRoot = CreateTempDirectory();
            try
            {
                var cDriveRoot = Path.Combine(tempRoot, "drive-c");
                Directory.CreateDirectory(Path.Combine(cDriveRoot, "WINDOWS"));

                var machine = new Machine();
                machine.PathMapper.AddMount(@"C:\", cDriveRoot, cDriveRoot);

                Assert.AreEqual(Path.Combine(cDriveRoot, "WINDOWS"), machine.PathMapper.TryMapGuestToHost(@"C:\WINDOWS", false));
            }
            finally
            {
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

        static void InvokeConfigureMandatoryCDriveRoot(Machine machine)
        {
            var method = typeof(Machine).GetMethod("ConfigureMandatoryCDriveRoot", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method);
            method.Invoke(machine, Array.Empty<object>());
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
