using System.Collections.Generic;
using Sharp86;

namespace Win3muCore
{
    [Module("WINSPOOL", @"C:\WINDOWS\SYSTEM\WINSPOOL.DRV")]
    public class Winspool : Module32
    {
        class PrinterHandleState
        {
            public string Name;
            public string DocumentName;
            public bool DocumentOpen;
            public bool PageOpen;
        }

        readonly Dictionary<ushort, PrinterHandleState> _printers = new Dictionary<ushort, PrinterHandleState>();
        ushort _nextPrinterHandle = 1;
        ushort _nextJobId = 1;

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 2)]
        struct DOCINFO
        {
            public uint lpszDocName;
            public uint lpszOutput;
            public uint lpszDatatype;
        }

        public override void Load(Machine machine)
        {
            base.Load(machine);
            _printers.Clear();
            _nextPrinterHandle = 1;
            _nextJobId = 1;
        }

        ushort AllocatePrinterHandle()
        {
            while (_nextPrinterHandle == 0 || _printers.ContainsKey(_nextPrinterHandle))
                _nextPrinterHandle++;

            return _nextPrinterHandle++;
        }

        static void WriteCount(Machine machine, uint ptr, ushort value)
        {
            if (ptr == 0)
                return;

            machine.WriteWord(ptr.Hiword(), ptr.Loword(), value);
            machine.WriteWord(ptr.Hiword(), (ushort)(ptr.Loword() + 2), 0);
        }

        string ReadDocumentName(short level, uint pDocInfo)
        {
            if (level != 1 || pDocInfo == 0)
                return null;

            var docInfo = _machine.ReadStruct<DOCINFO>(pDocInfo);
            if (docInfo.lpszDocName == 0)
                return null;

            return _machine.ReadString(docInfo.lpszDocName);
        }

        bool TryGetPrinter(ushort hPrinter, out PrinterHandleState printer)
        {
            return _printers.TryGetValue(hPrinter, out printer);
        }

        [EntryPoint(41)]
        public bool OpenPrinter(string pPrinterName, uint phPrinter, uint pDefault)
        {
            if (phPrinter == 0)
                return false;

            ushort hPrinter = AllocatePrinterHandle();
            _printers[hPrinter] = new PrinterHandleState()
            {
                Name = string.IsNullOrEmpty(pPrinterName) ? "WIN3MU" : pPrinterName,
            };

            _machine.WriteWord(phPrinter.Hiword(), phPrinter.Loword(), hPrinter);
            return true;
        }

        [EntryPoint(66)]
        public bool ClosePrinter(ushort hPrinter)
        {
            return _printers.Remove(hPrinter);
        }

        ushort StartDocCore(ushort hPrinter, short level, uint pDocInfo)
        {
            if (!TryGetPrinter(hPrinter, out var printer) || printer.DocumentOpen)
                return 0;

            printer.DocumentOpen = true;
            printer.PageOpen = false;
            printer.DocumentName = ReadDocumentName(level, pDocInfo) ?? printer.Name;

            while (_nextJobId == 0)
                _nextJobId++;

            return _nextJobId++;
        }

        [EntryPoint(71)]
        public ushort StartDocPrinter(ushort hPrinter, short level, uint pDocInfo)
        {
            return StartDocCore(hPrinter, level, pDocInfo);
        }

        [EntryPoint(0x0100)]
        public ushort StartDoc(ushort hPrinter, uint pDocInfo)
        {
            return StartDocCore(hPrinter, 1, pDocInfo);
        }

        bool EndDocCore(ushort hPrinter)
        {
            if (!TryGetPrinter(hPrinter, out var printer) || !printer.DocumentOpen)
                return false;

            printer.DocumentOpen = false;
            printer.PageOpen = false;
            printer.DocumentName = null;
            return true;
        }

        [EntryPoint(70)]
        public bool EndDocPrinter(ushort hPrinter)
        {
            return EndDocCore(hPrinter);
        }

        [EntryPoint(0x0101)]
        public bool EndDoc(ushort hPrinter)
        {
            return EndDocCore(hPrinter);
        }

        bool StartPageCore(ushort hPrinter)
        {
            if (!TryGetPrinter(hPrinter, out var printer) || !printer.DocumentOpen || printer.PageOpen)
                return false;

            printer.PageOpen = true;
            return true;
        }

        [EntryPoint(72)]
        public bool StartPagePrinter(ushort hPrinter)
        {
            return StartPageCore(hPrinter);
        }

        [EntryPoint(0x0102)]
        public bool StartPage(ushort hPrinter)
        {
            return StartPageCore(hPrinter);
        }

        bool EndPageCore(ushort hPrinter)
        {
            if (!TryGetPrinter(hPrinter, out var printer) || !printer.PageOpen)
                return false;

            printer.PageOpen = false;
            return true;
        }

        [EntryPoint(73)]
        public bool EndPagePrinter(ushort hPrinter)
        {
            return EndPageCore(hPrinter);
        }

        [EntryPoint(0x0103)]
        public bool EndPage(ushort hPrinter)
        {
            return EndPageCore(hPrinter);
        }

        [EntryPoint(74)]
        public bool WritePrinter(ushort hPrinter, uint pBuf, ushort cbBuf, uint pcWritten)
        {
            if (!TryGetPrinter(hPrinter, out var printer) || !printer.DocumentOpen || (cbBuf != 0 && pBuf == 0))
            {
                WriteCount(_machine, pcWritten, 0);
                return false;
            }

            WriteCount(_machine, pcWritten, cbBuf);
            return true;
        }

        [EntryPoint(75)]
        public bool AbortPrinter(ushort hPrinter)
        {
            if (!TryGetPrinter(hPrinter, out var printer) || !printer.DocumentOpen)
                return false;

            printer.DocumentOpen = false;
            printer.PageOpen = false;
            printer.DocumentName = null;
            return true;
        }
    }
}
