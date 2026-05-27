/*
Win3mu - Windows 3 Emulator
Copyright (C) 2017 Topten Software.

Win3mu is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

Win3mu is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with Win3mu.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Sharp86;

namespace Win3muCore
{
    [Module("DDEML", @"C:\WINDOWS\SYSTEM\DDEML.DLL")]
    public class DdeML: Module32
    {
        // Win3.x DDEML (Dynamic Data Exchange Management Library) provided
        // inter-process data exchange. These are stubs returning failure/null
        // since DDE conversations require a full server implementation.

        // DDEML error codes
        const ushort DMLERR_NO_ERROR = 0;
        const ushort DMLERR_NO_CONV_ESTABLISHED = 0x400A;
        const ushort DMLERR_DLL_NOT_INITIALIZED = 0x4000;
        const ushort DMLERR_INVALIDPARAMETER = 0x4006;
        const ushort DMLERR_SYS_ERROR = 0x400F;

        ushort _lastError = DMLERR_NO_ERROR;

        // Ordinal 2 - DdeInitialize
        [EntryPoint(0x0002)]
        public ushort DdeInitialize(uint pidInst, uint pfnCallback, uint afCmd, uint ulRes)
        {
            Log.WriteLine("DdeML.DdeInitialize: pidInst=0x{0:X8}, callback=0x{1:X8}, cmd=0x{2:X8}", pidInst, pfnCallback, afCmd);
            // Write a non-zero instance ID to let the caller think init succeeded
            if (pidInst != 0)
            {
                _machine.WriteWord(pidInst.Hiword(), pidInst.Loword(), 1);
                _machine.WriteWord(pidInst.Hiword(), (ushort)(pidInst.Loword() + 2), 0);
            }
            _lastError = DMLERR_NO_ERROR;
            return DMLERR_NO_ERROR;
        }

        // Ordinal 3 - DdeUninitialize
        [EntryPoint(0x0003)]
        public bool DdeUninitialize(uint idInst)
        {
            Log.WriteLine("DdeML.DdeUninitialize: inst={0}", idInst);
            return true; // success
        }

        // Ordinal 4 - DdeConnectList
        [EntryPoint(0x0004)]
        public ushort DdeConnectList(uint idInst, ushort hszService, ushort hszTopic, ushort hConvList, uint pCC)
        {
            Log.WriteLine("DdeML.DdeConnectList: inst={0}, service={1}, topic={2}", idInst, hszService, hszTopic);
            return 0; // NULL = no connections
        }

        // Ordinal 5 - DdeQueryNextServer
        [EntryPoint(0x0005)]
        public ushort DdeQueryNextServer(ushort hConvList, ushort hConvPrev)
        {
            return 0; // NULL = no more servers
        }

        // Ordinal 6 - DdeDisconnectList
        [EntryPoint(0x0006)]
        public bool DdeDisconnectList(ushort hConvList)
        {
            return true; // success
        }

        // Ordinal 7 - DdeConnect
        [EntryPoint(0x0007)]
        public ushort DdeConnect(uint idInst, ushort hszService, ushort hszTopic, uint pCC)
        {
            Log.WriteLine("DdeML.DdeConnect: inst={0}, service={1}, topic={2}", idInst, hszService, hszTopic);
            _lastError = DMLERR_NO_CONV_ESTABLISHED;
            return 0; // NULL = connection failed
        }

        // Ordinal 8 - DdeDisconnect
        [EntryPoint(0x0008)]
        public bool DdeDisconnect(ushort hConv)
        {
            Log.WriteLine("DdeML.DdeDisconnect: conv={0}", hConv);
            return true; // success
        }

        // Ordinal 9 - DdeReconnect
        [EntryPoint(0x0009)]
        public ushort DdeReconnect(ushort hConv)
        {
            _lastError = DMLERR_NO_CONV_ESTABLISHED;
            return 0; // NULL = reconnection failed
        }

        // Ordinal 10 - DdeQueryConvInfo
        [EntryPoint(0x000A)]
        public ushort DdeQueryConvInfo(ushort hConv, uint idTransaction, uint pConvInfo)
        {
            Log.WriteLine("DdeML.DdeQueryConvInfo: conv={0}, trans={1}", hConv, idTransaction);
            _lastError = DMLERR_INVALIDPARAMETER;
            return 0; // failure
        }

        // Ordinal 11 - DdeSetUserHandle
        [EntryPoint(0x000B)]
        public bool DdeSetUserHandle(ushort hConv, uint id, uint hUser)
        {
            Log.WriteLine("DdeML.DdeSetUserHandle: conv={0}, id={1}", hConv, id);
            return false; // failure - no valid conversation
        }

        // Ordinal 12 - DdeAbandonTransaction
        [EntryPoint(0x000C)]
        public bool DdeAbandonTransaction(uint idInst, ushort hConv, uint idTransaction)
        {
            return true; // success (nothing to abandon)
        }

        // Ordinal 13 - DdePostAdvise
        [EntryPoint(0x000D)]
        public bool DdePostAdvise(uint idInst, ushort hszTopic, ushort hszItem)
        {
            Log.WriteLine("DdeML.DdePostAdvise: inst={0}, topic={1}, item={2}", idInst, hszTopic, hszItem);
            return true; // success (no clients to notify)
        }

        // Ordinal 14 - DdeEnableCallback
        [EntryPoint(0x000E)]
        public bool DdeEnableCallback(uint idInst, ushort hConv, ushort wCmd)
        {
            return true; // success
        }

        // Ordinal 16 - DdeNameService
        [EntryPoint(0x0010)]
        public ushort DdeNameService(uint idInst, ushort hsz1, ushort hsz2, ushort afCmd)
        {
            Log.WriteLine("DdeML.DdeNameService: inst={0}, cmd=0x{1:X4}", idInst, afCmd);
            return 0; // NULL handle
        }

        // Ordinal 17 - DdeClientTransaction
        [EntryPoint(0x0011)]
        public ushort DdeClientTransaction(uint pData, uint cbData, ushort hConv, ushort hszItem, ushort wFmt, ushort wType, uint dwTimeout, uint pdwResult)
        {
            Log.WriteLine("DdeML.DdeClientTransaction: conv={0}, item={1}, fmt={2}, type=0x{3:X4}", hConv, hszItem, wFmt, wType);
            _lastError = DMLERR_NO_CONV_ESTABLISHED;
            return 0; // NULL = failure
        }

        // Ordinal 18 - DdeCreateDataHandle
        [EntryPoint(0x0012)]
        public ushort DdeCreateDataHandle(uint idInst, uint pSrc, uint cb, uint cbOff, ushort hszItem, ushort wFmt, ushort afCmd)
        {
            Log.WriteLine("DdeML.DdeCreateDataHandle: inst={0}, size={1}, fmt={2}", idInst, cb, wFmt);
            _lastError = DMLERR_SYS_ERROR;
            return 0; // NULL = failure
        }

        // Ordinal 19 - DdeAddData
        [EntryPoint(0x0013)]
        public ushort DdeAddData(ushort hData, uint pSrc, uint cb, uint cbOff)
        {
            _lastError = DMLERR_INVALIDPARAMETER;
            return 0; // NULL = failure
        }

        // Ordinal 20 - DdeGetData
        [EntryPoint(0x0014)]
        public uint DdeGetData(ushort hData, uint pDst, uint cbMax, uint cbOff)
        {
            return 0; // 0 bytes copied
        }

        // Ordinal 21 - DdeAccessData
        [EntryPoint(0x0015)]
        public uint DdeAccessData(ushort hData, uint pcbDataSize)
        {
            if (pcbDataSize != 0)
            {
                _machine.WriteWord(pcbDataSize.Hiword(), pcbDataSize.Loword(), 0);
                _machine.WriteWord(pcbDataSize.Hiword(), (ushort)(pcbDataSize.Loword() + 2), 0);
            }
            return 0; // NULL pointer
        }

        // Ordinal 22 - DdeUnaccessData
        [EntryPoint(0x0016)]
        public bool DdeUnaccessData(ushort hData)
        {
            return true; // success
        }

        // Ordinal 23 - DdeFreeDataHandle
        [EntryPoint(0x0017)]
        public bool DdeFreeDataHandle(ushort hData)
        {
            return true; // success
        }

        // Ordinal 24 - DdeGetLastError
        [EntryPoint(0x0018)]
        public ushort DdeGetLastError(uint idInst)
        {
            var err = _lastError;
            _lastError = DMLERR_NO_ERROR;
            return err;
        }

        // Ordinal 25 - DdeCreateStringHandle
        [EntryPoint(0x0019)]
        public ushort DdeCreateStringHandle(uint idInst, string psz, short iCodePage)
        {
            Log.WriteLine("DdeML.DdeCreateStringHandle: inst={0}, str=\"{1}\", cp={2}", idInst, psz, iCodePage);
            // Return a dummy handle based on a simple counter
            return _nextStringHandle++;
        }

        ushort _nextStringHandle = 1;

        // Ordinal 26 - DdeFreeStringHandle
        [EntryPoint(0x001A)]
        public bool DdeFreeStringHandle(uint idInst, ushort hsz)
        {
            return true; // success
        }

        // Ordinal 27 - DdeQueryString
        [EntryPoint(0x001B)]
        public uint DdeQueryString(uint idInst, ushort hsz, uint psz, uint cchMax, short iCodePage)
        {
            // Return 0 = empty string / handle not found
            if (psz != 0 && cchMax > 0)
            {
                _machine.WriteByte(psz.Hiword(), psz.Loword(), 0); // null-terminate
            }
            return 0;
        }

        // Ordinal 28 - DdeKeepStringHandle
        [EntryPoint(0x001C)]
        public bool DdeKeepStringHandle(uint idInst, ushort hsz)
        {
            return true; // success
        }

        // Ordinal 36 - DdeCmpStringHandles
        [EntryPoint(0x0024)]
        public short DdeCmpStringHandles(ushort hsz1, ushort hsz2)
        {
            if (hsz1 == hsz2) return 0;
            return (short)(hsz1 < hsz2 ? -1 : 1);
        }
    }
}
