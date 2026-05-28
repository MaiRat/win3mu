using Microsoft.VisualStudio.TestTools.UnitTesting;
using Win3muCore;

namespace Win3muCoreUnitTests
{
    [TestClass]
    public class SymbolResolverTests
    {
        [TestMethod]
        public void ResolveSymbol_DuplicateExportName_DoesNotThrowAndResolvesMessages()
        {
            var machine = new Machine();

            var openComm = machine.SymbolResolver.ResolveSymbol("OpenComm");
            var wmCommand = machine.SymbolResolver.ResolveSymbol("WM_COMMAND");

            Assert.IsNotNull(openComm);
            Assert.IsNotNull(wmCommand);
            Assert.AreEqual((ushort)0x0111, wmCommand.GetValue());
        }
    }
}
