using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Win3muCore.Utils;

namespace Win3muCoreUnitTests
{
    [TestClass]
    public class CommandLineTests
    {
        [TestMethod]
        public void DebugSwitch_EnablesDebuggerWithoutConfigJson()
        {
            var commandLine = new CommandLine(new[] { "--", "/debug" });

            Assert.IsTrue(commandLine.HasEnableDebuggerOverride);
            Assert.IsTrue(commandLine.EnableDebugger);
            Assert.IsFalse(commandLine.Break);
        }

        [TestMethod]
        public void BreakSwitch_EnablesDebuggerAndBreaksOnLoad()
        {
            var commandLine = new CommandLine(new[] { "--", "/break" });

            Assert.IsTrue(commandLine.HasEnableDebuggerOverride);
            Assert.IsTrue(commandLine.EnableDebugger);
            Assert.IsTrue(commandLine.Break);
        }

        [TestMethod]
        public void ConfigSwitch_IsRejected()
        {
            try
            {
                new CommandLine(new[] { "--", "/config:debug" });
                Assert.Fail("Expected /config to be rejected.");
            }
            catch (InvalidOperationException ex)
            {
                StringAssert.Contains(ex.Message, "/config");
                StringAssert.Contains(ex.Message, "no longer supported");
            }
        }
    }
}
