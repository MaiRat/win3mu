using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Win3muCore;

namespace Win3muCoreUnitTests
{
    [TestClass]
    public class Module32Tests
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct TestPoint
        {
            public short X;
            public short Y;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct OddSizedStruct
        {
            public byte A;
            public byte B;
            public byte C;
        }

        [Module("TEST", "TEST.DLL")]
        class TestModule : Module32
        {
        }

        [TestMethod]
        public void Module32_SizeOfType16_SupportsRawValueStructs()
        {
            var module = new TestModule();

            Assert.AreEqual((ushort)4, InvokeSizeOfType16(module, typeof(TestPoint)));
        }

        [TestMethod]
        public void Module32_SizeOfType16_WordAlignsRawValueStructs()
        {
            var module = new TestModule();

            Assert.AreEqual((ushort)4, InvokeSizeOfType16(module, typeof(OddSizedStruct)));
        }

        [TestMethod]
        public void User_GetUpdateRect_UsesPointerThunkingSignature()
        {
            var method = typeof(User).GetMethod("GetUpdateRect", BindingFlags.Instance | BindingFlags.Public);

            Assert.IsNotNull(method);
            Assert.AreEqual(typeof(IntPtr), method.GetParameters()[1].ParameterType);
        }

        [TestMethod]
        public void Module32_ResolveBufferCapacity_SupportsKnownIntegralTypes()
        {
            Assert.AreEqual(12, InvokeResolveBufferCapacity(12));
            Assert.AreEqual(13, InvokeResolveBufferCapacity((ushort)13));
            Assert.AreEqual(14, InvokeResolveBufferCapacity((nint)14));
            Assert.AreEqual(15, InvokeResolveBufferCapacity((short)15));
            Assert.AreEqual(16, InvokeResolveBufferCapacity((uint)16));
            Assert.AreEqual(17, InvokeResolveBufferCapacity((nuint)17));
        }

        static ushort InvokeSizeOfType16(Module32 module, Type type)
        {
            return (ushort)typeof(Module32)
                .GetMethod("SizeOfType16", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(module, new object[] { type });
        }

        static int InvokeResolveBufferCapacity(object value)
        {
            return (int)typeof(Module32)
                .GetMethod("ResolveBufferCapacity", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new[] { value });
        }
    }
}
