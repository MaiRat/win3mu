using Microsoft.VisualStudio.TestTools.UnitTesting;
using Win3muCore;

namespace Win3muCoreUnitTests
{
    [TestClass]
    public class SymbolResolverTests
    {
        [TestMethod]
        public void ResolveSymbol_DuplicateExportName_DoesNotThrow()
        {
            var machine = new Machine();

            var openComm = machine.SymbolResolver.ResolveSymbol("OpenComm");

            Assert.IsNotNull(openComm);
        }

        [TestMethod]
        public void ResolveSymbol_MessageName_ResolvesAfterBuildingSymbolMap()
        {
            var machine = new Machine();

            var wmCommand = machine.SymbolResolver.ResolveSymbol("WM_COMMAND");

            Assert.IsNotNull(wmCommand);
            Assert.AreEqual((ushort)0x0111, wmCommand.GetValue());
        }
    }
}
