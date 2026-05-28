using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Win3muCore;

namespace Win3muCoreUnitTests
{
    [TestClass]
    public class ModuleManagerTests
    {
        [TestMethod]
        public void GetModule_ZeroHandle_ReturnsLoadedExecutableModule()
        {
            var machine = new Machine();
            var module = new Module16(GetRepositoryFile("Samples", "alarm.exe"));
            module.hModule = 1;

            var loadedModules = (Dictionary<string, ModuleBase>)typeof(ModuleManager)
                .GetField("_loadedModules", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(machine.ModuleManager);

            loadedModules[module.GetModuleName()] = module;

            var resolved = machine.ModuleManager.GetModule(0);

            Assert.AreSame(module, resolved);
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
    }
}
