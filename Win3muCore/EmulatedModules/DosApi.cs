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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Sharp86;

namespace Win3muCore
{
    public class DosError : Exception
    {
        public DosError(ushort errorCode) :
            base(string.Format("DOS Error: {0}", errorCode))
        {
            this.errorCode = errorCode;

            Log.WriteLine($"Dos Error: {errorCode}");
        }

        public DosError(int hresult) :
            base(string.Format("DOS Error: HRESULT = {0}", hresult & 0xFFFF))
        {
            errorCode = (byte)(hresult & 0xFFFF);

            Log.WriteLine($"Dos Error: {errorCode}");
        }

        public ushort errorCode;

        public const ushort FunctionNumberInvalid = 0x01;
        public const ushort FileNotFound = 0x02;
        public const ushort PathNotFound = 0x03;
        public const ushort TooManyOpenFiles = 0x04;
        public const ushort AccessDenied = 0x05;
        public const ushort HandleInvalid = 0x06;
        public const ushort MemoryControlBlocksDestroyed = 0x07;
        public const ushort InsufficientMemory = 0x08;
        public const ushort MemoryBlockAddressInvalid = 0x09;
        public const ushort EnvironmentInvalid = 0x0A;
        public const ushort FormatInvalid = 0x0B;
        public const ushort AccessCodeInvalid = 0x0C;
        public const ushort DataInvalid = 0x0D;
        public const ushort UnknownUnit = 0x0E;
        public const ushort DiskDriveInvalid = 0x0F;
        public const ushort AttemptedToRemoveCurrentDirectory = 0x10;
        public const ushort NotSameDevice = 0x11;
        public const ushort NoMoreFiles = 0x12;
        public const ushort DiskWriteProtected = 0x13;
        public const ushort UnknownUnit2 = 0x14;
        public const ushort DriveNotReady = 0x15;
        public const ushort UnknownCommand = 0x16;
        public const ushort DataError = 0x17;
        public const ushort BadRequestStructureLength = 0x18;
        public const ushort SeekError = 0x19;
        public const ushort UnknownMediaType = 0x1A;
        public const ushort SectorNotFound = 0x1B;
        public const ushort PrinterOutOfPaper = 0x1C;
        public const ushort WriteFault = 0x1D;
        public const ushort ReadFault = 0x1E;
        public const ushort GeneralFailure = 0x1F;
        public const ushort SharingViolation = 0x20;
        public const ushort LockViolation = 0x21;
        public const ushort DiskChangeInvalid = 0x22;
        public const ushort FCBUnavailable = 0x23;
        public const ushort SharingBufferExceeded = 0x24;
        public const ushort UnsupportedNetworkRequest = 0x32;
        public const ushort RemoteMachineNotListening = 0x33;
        public const ushort DuplicateNameOnNetwork = 0x34;
        public const ushort NetworkNameNotFound = 0x35;
        public const ushort NetworkBusy = 0x36;
        public const ushort DeviceNoLongerExistsOnNetwork = 0x37;
        public const ushort NetBIOSCommandLimitExceeded = 0x38;
        public const ushort ErrorInNetworkAdapterHardware = 0x39;
        public const ushort IncorrectResponseFromNetwork = 0x3A;
        public const ushort UnexpectedNetworkError = 0x3B;
        public const ushort RemoteAdapterIncompatible = 0x3C;
        public const ushort PrintQueueFull = 0x3D;
        public const ushort NotEnoughSpaceForPrintFile = 0x3E;
        public const ushort PrintFileCanceled = 0x3F;
        public const ushort NetworkNameDeleted = 0x40;
        public const ushort NetworkAccessDenied = 0x41;
        public const ushort IncorrectNetworkDeviceType = 0x42;
        public const ushort NetworkNameNotFound2 = 0x43;
        public const ushort NetworkNameLimitExceeded = 0x44;
        public const ushort NetBIOSSessionLimitExceeded = 0x45;
        public const ushort FileSharingTemporarilyPaused = 0x46;
        public const ushort NetworkRequestNotAccepted = 0x47;
        public const ushort PrintOrDiskRedirectionPaused = 0x48;
        public const ushort FileAlreadyExists = 0x50;
        public const ushort Reserved = 0x51;
        public const ushort CannotMakeDirectory = 0x52;
        public const ushort FailOnInt24H = 0x53;
        public const ushort TooManyRedirections = 0x54;
        public const ushort DuplicateRedirection = 0x55;
        public const ushort InvalidPassword = 0x56;
        public const ushort InvalidParameter = 0x57;
        public const ushort NetworkDeviceFault = 0x58;
        public const ushort FunctionNotSupportedByNetwork = 0x59;
        public const ushort RequiredSystemComponentNotInstalled = 0x5A;
    }

    public class DosApi
    {
        public DosApi(CPU cpu, ISite site)
        {
            _cpu = cpu;
            _site = site;

            // Set all drives root directory
            for (int i=0; i<26; i++)
            {
                _driveInfo[i] = new DriveInfo()
                {
                    CurrentDirectory = "\\",
                };
            }

            _dtaOfs = 0x80;
            _dtaSeg = 0;
        }

        public interface ISite
        {
            void ExitProcess(short exitCode);
            bool DoesGuestDirectoryExist(string path); 
            string TryMapGuestPathToHost(string path, bool forWrite);
            string TryMapHostPathToGuest(string path, bool forWrite);
            IEnumerable<string> GetVirtualSubFolders(string guestPath);
            uint Alloc(ushort size);
            void Free(uint ptr);
        }

        CPU _cpu;
        ISite _site;
        ushort _psp;
        ushort _dtaOfs;
        ushort _dtaSeg;

        public bool EnableApiLogging;
        public bool EnableFileLogging;

        public CPU CPU
        {
            get { return _cpu; }
        }

        public ushort PSP
        {
            get { return _psp; }
            set
            {
                _psp = value;
                _dtaSeg = 0;
            }
        }

        class DriveInfo
        {
            public string CurrentDirectory;
            public uint DriveParameterBlock;
        }

        int _currentDrive = 0;
        DriveInfo[] _driveInfo = new DriveInfo[26];

        // Matches Windows DRIVE_xxx constants
        public enum DriveType
        {
            None = 0,
            Removable = 2,
            Fixed = 3,
            Remote = 4,
        }

        public DriveType GetDriveType(int drive)
        {
            if (!_site.DoesGuestDirectoryExist(string.Format("{0}:\\", (char)('A' + drive))))
                return DriveType.None;
            else
                return DriveType.Fixed;
        }

        // Check if a drive number is valid
        public void CheckValidDrive(int drive)
        {
            if (drive<0 || drive>=25 || GetDriveType(drive) == DriveType.None)
                throw new DosError(DosError.DiskDriveInvalid);
        }

        public string GetCurrentDirectory(int drive)
        {
            CheckValidDrive(drive);
            return _driveInfo[drive].CurrentDirectory;
        }

        public bool IsDirectory(string path)
        {
            // Qualify
            var qualified = QualifyPath(path);

            // Check if it exists
            return _site.DoesGuestDirectoryExist(qualified);
        }

        public bool IsFile(string path)
        {
            // Qualify
            var qualified = QualifyPath(path);

            // Try to map it.  If it can't be mapped then it must be
            // a parent to a mapped folder and must be a directory
            var hostPath = _site.TryMapGuestPathToHost(qualified, false);
            if (hostPath == null)
                return false;

            // Test if it exists
            return System.IO.File.Exists(hostPath);
        }

        public bool SetCurrentDirectory(int drive, string name)
        {
            CheckValidDrive(drive);

            // Must end with a backslash
            if (!name.EndsWith("\\"))
                name += "\\";

            // Check it's a valid 8.3 path
            var qualified = string.Format("{0}:{1}", (char)('a' + drive), name);
            if (!DosPath.IsValid(qualified))
                throw new DosError(DosError.PathNotFound);

            // Verify it
            if (!_site.DoesGuestDirectoryExist(qualified))
                throw new DosError(DosError.PathNotFound);

            // Store it
            _driveInfo[drive].CurrentDirectory = name.ToUpperInvariant();
            return true;
        }

        public bool SetCurrentDrive(int drive)
        {
            CheckValidDrive(drive);

            _currentDrive = drive;
            return true;
        }

        public int GetCurrentDrive(int drive)
        {
            return _currentDrive;
        }

        public string WorkingDirectory
        {
            get
            {
                CheckValidDrive(_currentDrive);
                return string.Format("{0}:{1}", (char)('A' + _currentDrive), _driveInfo[_currentDrive].CurrentDirectory);
            }
            set
            {
                value = QualifyPath(value);
                var drive = DosPath.DriveFromPath(value);
                CheckValidDrive(drive);
                SetCurrentDirectory(drive, value.Substring(2));
                SetCurrentDrive(drive);
            }
        }

        uint CurrentDay
        {
            get
            {
                return (uint)(Math.Floor(CurrentDateTime.ToOADate()));
            }
        }

        uint _lastDay;
        TimeSpan _clockOffset = TimeSpan.Zero;

        DateTime CurrentDateTime => DateTime.Now + _clockOffset;

        static byte ToBcd(int value)
        {
            return (byte)(((value / 10) << 4) | (value % 10));
        }

        static int FromBcd(byte value)
        {
            return ((value >> 4) * 10) + (value & 0x0F);
        }

        static uint ToClockCount(DateTime now)
        {
            return (uint)(now.Hour * 65520 + now.Minute * 1092 + now.Second * 18.2);
        }

        static TimeSpan ClockCountToTimeOfDay(uint clockCount)
        {
            var hours = (int)(clockCount / 65520);
            clockCount -= (uint)(hours * 65520);

            var minutes = (int)(clockCount / 1092);
            clockCount -= (uint)(minutes * 1092);

            var seconds = (int)Math.Round(clockCount / 18.2);
            if (seconds >= 60)
            {
                seconds -= 60;
                minutes++;
            }
            if (minutes >= 60)
            {
                minutes -= 60;
                hours++;
            }

            return new TimeSpan(hours % 24, minutes, seconds);
        }

        public void DispatchInt1A()
        {
            // http://www.ousob.com/ng/peter_norton/ng8edc8.php
            switch (_cpu.ah)
            {
                case 0:
                    if (_lastDay == 0)
                        _lastDay = CurrentDay;
                    var thisDay = CurrentDay;

                    // Get current clock count
                    var now = CurrentDateTime;
                    var clockCount = ToClockCount(now);

                    _cpu.cx = clockCount.Hiword();
                    _cpu.dx = clockCount.Loword();
                    _cpu.al = (byte)((thisDay != _lastDay) ? 1 : 0);
                    _lastDay = thisDay;
                    break;

                case 1:
                {
                    var targetClockCount = ((uint)_cpu.cx << 16) | _cpu.dx;
                    var target = DateTime.Now.Date + ClockCountToTimeOfDay(targetClockCount);
                    _clockOffset = target - DateTime.Now;
                    _cpu.FlagC = false;
                    break;
                }

                case 2:
                {
                    var rtcNow = CurrentDateTime;
                    _cpu.ch = ToBcd(rtcNow.Hour);
                    _cpu.cl = ToBcd(rtcNow.Minute);
                    _cpu.dh = ToBcd(rtcNow.Second);
                    _cpu.dl = (byte)(TimeZoneInfo.Local.IsDaylightSavingTime(rtcNow) ? 1 : 0);
                    _cpu.FlagC = false;
                    break;
                }

                case 3:
                {
                    var current = CurrentDateTime;
                    var target = new DateTime(
                        current.Year,
                        current.Month,
                        current.Day,
                        FromBcd(_cpu.ch),
                        FromBcd(_cpu.cl),
                        FromBcd(_cpu.dh));
                    _clockOffset = target - DateTime.Now;
                    _cpu.FlagC = false;
                    break;
                }

                case 4:
                {
                    var rtcNow = CurrentDateTime;
                    _cpu.ch = ToBcd(rtcNow.Year / 100);
                    _cpu.cl = ToBcd(rtcNow.Year % 100);
                    _cpu.dh = ToBcd(rtcNow.Month);
                    _cpu.dl = ToBcd(rtcNow.Day);
                    _cpu.FlagC = false;
                    break;
                }

                case 5:
                {
                    var current = CurrentDateTime;
                    var target = new DateTime(
                        (FromBcd(_cpu.ch) * 100) + FromBcd(_cpu.cl),
                        FromBcd(_cpu.dh),
                        FromBcd(_cpu.dl),
                        current.Hour,
                        current.Minute,
                        current.Second);
                    _clockOffset = target - DateTime.Now;
                    _lastDay = CurrentDay;
                    _cpu.FlagC = false;
                    break;
                }

                default:
                    Log.WriteLine("Int 1Ah, service {0} not implemented - returning with carry flag set", _cpu.ah);
                    _cpu.FlagC = true;
                    break;

                    /*
                case 1:
                    // Set current clock count
                    break;

                case 2:
                    // Get real-time clock time
                    break;

                case 3:
                    // Set realtime clock time
                    break;

                case 4:
                    // Get real-time clock date
                    break;

                case 5:
                    // Set real-time clock date
                    break;

                case 6:
                    // Set real-time clock alarm
                    break;

                case 7:
                    // Get real-time clock alarm
                    break;
                    */
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct DRIVEPARAMETERBLOCK
        {
            public byte driveNumber;
            public byte unitNumberInDriver;
            public ushort bytesPerSector;
            public byte sectorsPerCluster;
            public byte clusterSizeLog2;
            public ushort reservedSectors;
            public byte fatTables;
            public ushort rootDirEntries;
            public ushort sectorDataSpace;
            public ushort maxClusterNumber;
            public ushort sectorsPerFAT;
            public ushort sectorRootDir;
            public uint deviceHeader;
            public byte mediaDescriptorByte;
            public byte accessIndicatorByte;
            public uint nextParameterBlock;
            public ushort startClusterFreeSpaceSearch;
            public ushort unknown;
        };

        public void DispatchInt21()
        {
            if (EnableApiLogging)
                Log.WriteLine("DOS Int 21h: AH = 0x{0:X2}", _cpu.ah);

            if (_cpu.ah != 0x59)
                _lastError = 0;

            try
            {
                _cpu.FlagC = false;
                switch (_cpu.ah)
                {
                    case 0x0E:
                        // Select disk
                        // Docs are vague about what happens if invalid drive specified
                        // Setup return value in al first
                        _cpu.al = (byte)(Enumerable.Range(0, 26).Count(x => GetDriveType(x) != DriveType.None));

                        // Set drive (may throw exception)
                        SetCurrentDrive(_cpu.dl);
                        break;



                    case 0x11:
                    {
                        TryFindFirstFileFCB();
                        break;
                    }

                    case 0x12:
                    {
                        TryFindNextFileFCB();
                        break;
                    }

                    case 0x19:
                        // Get current drive
                        _cpu.al = (byte)_currentDrive;
                        break;

                    case 0x1A:
                        // Set DTA
                        _dtaSeg = _cpu.ds;
                        _dtaOfs = _cpu.dx;
                        break;

                    case 0x25:
                        // Set Interrupt Vector
                        _cpu.MemoryBus.WriteWord(_cpu.idt, (ushort)(_cpu.al * 4), _cpu.dx);
                        _cpu.MemoryBus.WriteWord(_cpu.idt, (ushort)(_cpu.al * 4 + 2), _cpu.ds);
                        break;

                    case 0x2A:
                    {
                        // Get Date
                        var now = DateTime.Now;
                        _cpu.cx = (ushort)now.Year;
                        _cpu.dh = (byte)now.Month;
                        _cpu.dl = (byte)now.Day;
                        _cpu.al = (byte)now.DayOfWeek;
                        break;
                    }

                    case 0x2C:
                    {                                      
                        // Get Time
                        var now = DateTime.Now;
                        _cpu.ch = (byte)now.Hour;
                        _cpu.cl = (byte)now.Minute;
                        _cpu.dh = (byte)now.Second;
                        _cpu.dl = (byte)(now.Millisecond / 10);     // Hundredths
                        break;
                    }

                    case 0x2F:
                        // Get Disk Transfer Area
                        _cpu.es = _dtaSeg;
                        _cpu.bx = _dtaOfs;
                        break;

                    case 0x30:
                        // Get MSDOS Version
                        _cpu.ax = 0x0006;
                        break;

                    case 0x33:
                    {
                        // Get/Set System Values (Ctrl-C check flag)
                        switch (_cpu.al)
                        {
                            case 0: // Get Ctrl-C check flag
                                _cpu.dl = 0; // Ctrl-C checking off
                                break;
                            case 1: // Set Ctrl-C check flag
                                // Silently accept - no effect in emulated environment
                                break;
                            case 5: // Get boot drive
                                _cpu.dl = 3; // C: drive
                                break;
                            case 6: // Get true DOS version
                                _cpu.bx = 0x0006; // Major
                                _cpu.dx = 0x0000; // Minor, revision
                                break;
                            default:
                                Log.WriteLine("Unsupported Int 21h AH=33h subfunction AL=0x{0:X2}", _cpu.al);
                                break;
                        }
                        break;
                    }

                    case 0x32:
                    {
                        // Get Drive Parameter Block
                        // https://books.google.com.au/books?id=YxFTezF9-sMC&pg=PT405&lpg=PT405&dq=Get+disk+parameter+block+for+specified+drive&source=bl&ots=1JzFWO1B2G&sig=Wwc7OQ7Jlw_FHJGzL6BtYtvm2Wk&hl=en&sa=X&ved=0ahUKEwjzwai28v_PAhXFkZQKHai-CAsQ6AEIMTAD#v=onepage&q=Get%20disk%20parameter%20block%20for%20specified%20drive&f=false

                        // Resolve drive
                        int drive = _cpu.dl == 0 ? _currentDrive : _cpu.dl - 1;

                        // Validate drive
                        _cpu.al = 0xff;
                        CheckValidDrive(drive);

                        if (_driveInfo[drive].DriveParameterBlock == 0)
                        {
                            var dpb = new DRIVEPARAMETERBLOCK()
                            {
                                // Copied from WinXP/VirtualBox CDrive
                                driveNumber = (byte)drive,
                                unitNumberInDriver = (byte)drive,
                                bytesPerSector = 512,
                                sectorsPerCluster = 126,
                                clusterSizeLog2 = 0,
                                reservedSectors = 1,
                                fatTables = 2,
                                rootDirEntries = 63,
                                sectorDataSpace = 0,
                                maxClusterNumber = 15748,
                                sectorsPerFAT = 512,
                                deviceHeader = 0,
                                mediaDescriptorByte = 248,
                                accessIndicatorByte = 10,
                                nextParameterBlock = 0,
                                startClusterFreeSpaceSearch = 0,
                            };
                            uint ptr = _site.Alloc((ushort)Marshal.SizeOf<DRIVEPARAMETERBLOCK>());
                             _driveInfo[drive].DriveParameterBlock = ptr;
                            _cpu.MemoryBus.WriteStruct(ptr, ref dpb);
                        }

                        _cpu.ds = _driveInfo[drive].DriveParameterBlock.Hiword();
                        _cpu.bx = _driveInfo[drive].DriveParameterBlock.Loword();
                        _cpu.al = 0;
                        break;
                    }

                    case 0x35:
                        // Get Interrupt Vector
                        _cpu.bx = _cpu.MemoryBus.ReadWord(_cpu.idt, (ushort)(_cpu.al * 4));
                        _cpu.es = _cpu.MemoryBus.ReadWord(_cpu.idt, (ushort)(_cpu.al * 4 + 2));
                        break;

                    case 0x36:
                    {
                        // Get Disk Free Space
                        int drive36 = _cpu.dl == 0 ? _currentDrive : _cpu.dl - 1;
                        try
                        {
                            CheckValidDrive(drive36);
                            // Return synthetic values for a large drive:
                            // AX = sectors per cluster, BX = available clusters, CX = bytes per sector, DX = total clusters
                            _cpu.ax = 8;        // sectors per cluster
                            _cpu.cx = 512;      // bytes per sector
                            _cpu.dx = 65535;    // total clusters (max 16-bit = ~256MB)
                            _cpu.bx = 32768;    // available clusters (~128MB free)
                        }
                        catch (DosError)
                        {
                            _cpu.ax = 0xFFFF;   // invalid drive
                        }
                        break;
                    }

                    case 0x3B:
                        // Set current directory
                        var dirName = _cpu.MemoryBus.ReadString(_cpu.ds, _cpu.dx);
                        WorkingDirectory = dirName;
                        break;

                    case 0x3C:
                        _cpu.ax = CreateFile(_cpu.MemoryBus.ReadString(_cpu.ds, _cpu.dx), _cpu.cx);
                        break;

                    case 0x3D:
                        _cpu.ax = OpenFile(_cpu.MemoryBus.ReadString(_cpu.ds, _cpu.dx), _cpu.al);
                        break;

                    case 0x3E:
                        CloseFile(_cpu.bx);
                        break;

                    case 0x3F:
                        _cpu.ax = ReadFile(_cpu.bx, _cpu.cx, _cpu.ds, _cpu.dx);
                        break;

                    case 0x40:
                        _cpu.ax = WriteFile(_cpu.bx, _cpu.cx, _cpu.ds, _cpu.dx);
                        break;

                    case 0x41:
                    {
                        var guestFile = _cpu.MemoryBus.ReadString(_cpu.ds, _cpu.dx);
                        DeleteFile(guestFile);
                        break;
                    }

                    case 0x42:
                     {
                        int retv = SeekFile(_cpu.bx, _cpu.cx << 16 | _cpu.dx, _cpu.al);
                        _cpu.ax = retv.Loword();
                        _cpu.dx = retv.Hiword();
                        break;
                    }

                    case 0x43:
                    {
                        string filename = _cpu.MemoryBus.ReadString(_cpu.ds, _cpu.dx);
                        if (_cpu.al != 0)
                            _cpu.cx = SetFileAttributes(filename, _cpu.cx);
                        else
                            _cpu.cx = GetFileAttributes(filename);
                        break;
                    }

                    case 0x44:
                        switch (_cpu.al)
                        {
                            case 0:
                                // Get device information
                                var file = FileFromHandle(_cpu.bx);
                                if (file == null)
                                    break;
                                _cpu.dx = (ushort)DosPath.DriveFromPath(file.guestFilename);
                                return;

                        }
                        Log.WriteLine("Failing call to DOS Int 21h ah = 0x44 (IOCTL)");
                        _cpu.FlagC = true;
                        break;

                    case 0x47:
                    {
                        // Get current directory
                        int drive = _cpu.dl == 0 ? _currentDrive : _cpu.dl-1;
                        CheckValidDrive(drive);
                        var dir = _driveInfo[drive].CurrentDirectory.Substring(1);
                        if (dir.EndsWith("\\"))
                            dir = dir.Substring(0, dir.Length - 1);
                        _cpu.MemoryBus.WriteString(_cpu.ds, _cpu.si, dir, 64);
                        break;
                    }

                    case 0x48:
                    {
                        // Allocate Memory - in Windows 3.x, memory is managed by Windows
                        // Return error: insufficient memory (apps should use Windows API instead)
                        Log.WriteLine("DOS Int 21h/48h: Allocate Memory request (unsupported, returning error)");
                        _cpu.FlagC = true;
                        _cpu.ax = 0x08; // Insufficient memory
                        _cpu.bx = 0;    // Largest available block = 0
                        break;
                    }

                    case 0x49:
                    {
                        // Free Memory - silently succeed
                        Log.WriteLine("DOS Int 21h/49h: Free Memory request (no-op in emulated environment)");
                        break;
                    }

                    case 0x4A:
                    {
                        // Resize Memory Block - silently succeed
                        Log.WriteLine("DOS Int 21h/4Ah: Resize Memory Block request (no-op in emulated environment)");
                        break;
                    }

                    case 0x4c:
                        _site.ExitProcess(_cpu.al);
                        break;

                    case 0x4e:
                    {
                        // Find first file
                        var spec = _cpu.MemoryBus.ReadString(_cpu.ds, _cpu.dx);
                        FindFiles(spec, _cpu.cl);

                        // Find next file
                        if (_foundFiles != null && _foundFiles.Count > 0)
                        {
                            var ffs = _foundFiles[0];
                            _foundFiles.RemoveAt(0);
                            _cpu.MemoryBus.WriteStruct(BitUtils.MakeDWord(_dtaOfs, _dtaSeg), ref ffs);
                        }
                        else
                        {
                            _cpu.FlagC = true;
                            _cpu.ax = DosError.NoMoreFiles;
                        }
                        break;
                    }

                    case 0x4f:
                    {
                        // Find next file
                        if (_foundFiles != null && _foundFiles.Count>0)
                        {
                            var ffs = _foundFiles[0];
                            _foundFiles.RemoveAt(0);
                            _cpu.MemoryBus.WriteStruct(BitUtils.MakeDWord(_dtaOfs, _dtaSeg), ref ffs);
                        }
                        else
                        {
                            _cpu.FlagC = true;
                            _cpu.ax = DosError.NoMoreFiles;
                        }
                        break;
                    }

                    case 0x56:
                    {
                        string oldFile = _cpu.MemoryBus.ReadString(_cpu.ds, _cpu.dx);
                        string newFile = _cpu.MemoryBus.ReadString(_cpu.es, _cpu.di);
                        RenameFile(oldFile, newFile);
                        break;
                    }

                    case 0x57:
                    {
                        // Get/Set File Date and Time
                        var file57 = FileFromHandle(_cpu.bx);
                        switch (_cpu.al)
                        {
                            case 0:
                            {
                                // Get file date and time
                                DateTime lastWrite;
                                if (file57 != null && file57.hostFilename != null && System.IO.File.Exists(file57.hostFilename))
                                    lastWrite = System.IO.File.GetLastWriteTime(file57.hostFilename);
                                else
                                    lastWrite = DateTime.Now;

                                // DOS time format: bits 15-11=hours, 10-5=minutes, 4-0=seconds/2
                                _cpu.cx = (ushort)((lastWrite.Hour << 11) | (lastWrite.Minute << 5) | (lastWrite.Second / 2));
                                // DOS date format: bits 15-9=year-1980, 8-5=month, 4-0=day
                                _cpu.dx = (ushort)(((lastWrite.Year - 1980) << 9) | (lastWrite.Month << 5) | lastWrite.Day);
                                break;
                            }
                            case 1:
                            {
                                // Set file date and time
                                if (file57 != null && file57.hostFilename != null && System.IO.File.Exists(file57.hostFilename))
                                {
                                    int hour = (_cpu.cx >> 11) & 0x1F;
                                    int minute = (_cpu.cx >> 5) & 0x3F;
                                    int second = (_cpu.cx & 0x1F) * 2;
                                    int year = ((_cpu.dx >> 9) & 0x7F) + 1980;
                                    int month = (_cpu.dx >> 5) & 0x0F;
                                    int day = _cpu.dx & 0x1F;

                                    try
                                    {
                                        var dt = new DateTime(year, month, day, hour, minute, second);
                                        System.IO.File.SetLastWriteTime(file57.hostFilename, dt);
                                    }
                                    catch (Exception)
                                    {
                                        // Silently ignore invalid date/time values
                                    }
                                }
                                break;
                            }
                            default:
                                Log.WriteLine("Unsupported Int 21h AH=57h subfunction AL=0x{0:X2}", _cpu.al);
                                _cpu.FlagC = true;
                                _cpu.ax = DosError.FunctionNumberInvalid;
                                break;
                        }
                        break;
                    }

                    case 0x59:
                        _cpu.ax = _lastError;
                        _cpu.bh = 0x07;
                        _cpu.bl = 0x04;
                        _cpu.ch = 0;
                        break;

                    case 0x5A:
                    {
                        // Create unique name file

                        // Get the folder and map to host path
                        var guestFolder = _cpu.MemoryBus.ReadString(_cpu.ds, _cpu.dx);
                        var hostFolder = _site.TryMapGuestPathToHost(guestFolder, true);
                        if (hostFolder == null)
                            throw new DosError(DosError.PathNotFound);

                        var r = new Random();
                        while (true)
                        {
                            // Generate random file name
                            var sb = new System.Text.StringBuilder();
                            for (int i=0; i<8; i++)
                            {
                                var ch = r.Next(36);
                                sb.Append(ch < 26 ? (char)('A' + ch) : (char)('0' + ch - 26));
                            }
                            sb.Append(".tmp");

                            // Does it exist?
                            var hostFile = DosPath.Join(hostFolder, sb.ToString());
                            if (System.IO.File.Exists(hostFile))
                                continue;

                            // No, create it
                            var guestFile = DosPath.Join(guestFolder, sb.ToString());
                            _cpu.ax = CreateFile(guestFile, 0);

                            // Return the created path
                            _cpu.MemoryBus.WriteString(_cpu.ds, _cpu.dx, guestFile, 0xFFFF);
                            break;
                        }

                        break;
                    }

                    case 0x71:
                        // Long Filename vector - not supported
                        // http://www.fysnet.net/longfile.htm
                        throw new DosError(DosError.FunctionNumberInvalid);

                    default:
                        Log.WriteLine("Unsupported DOS Interrupt - ah=0x{0:X2} - returning error", _cpu.ah);
                        _cpu.FlagC = true;
                        _cpu.ax = DosError.FunctionNumberInvalid;
                        _lastError = DosError.FunctionNumberInvalid;
                        break;
                }
            }
            catch (DosError x)
            {
                _cpu.FlagC = true;
                _cpu.ax = x.errorCode;
                _lastError = x.errorCode;
            }
        }

        public void DispatchInt2f()
        {
            // Multiplex services
            // http://www.techhelpmanual.com/681-int_2fh__multiplex_interrupt.html
            switch (_cpu.ax)
            {
                case 0x1600:
                case 0x1601:
                case 0x1602:
                case 0x1606:
                case 0x160A:
                case 0x4680:
                    _cpu.ax = 0;
                    return;
            }

            switch (_cpu.ah)
            {
                case 0x15:
                    // MSCDEX services
                    switch (_cpu.al)
                    {
                        case 0:
                            _cpu.al = 0;        // 0xFF if installed
                            return;
                    }
                    break;
            }

            Log.WriteLine("Unsupported Multiplex (0x2f) Interrupt - ax=0x{0:X4} - returning zero", _cpu.ax);
            _cpu.ax = 0;
        }



        ushort _lastError = 0;

        // Get the file info from a handle
        File FileFromHandle(ushort handle)
        {
            File file;
            if (!_openFiles.TryGetValue(handle, out file))
            {
                throw new DosError(DosError.HandleInvalid);
            }
            return file;
        }

        public File OpenFileHandle(string guestFilename, FileMode mode, FileAccess access, FileShare share)
        {
            if (EnableFileLogging)
            {
                Log.WriteLine($"Opening '{guestFilename}' mode {mode} access: {access} share:{share}"); 
            }
            // Qualify the guest filename
            guestFilename = QualifyPath(guestFilename);

            // Check the path exists
            var dir = System.IO.Path.GetDirectoryName(guestFilename);
            if (!_site.DoesGuestDirectoryExist(dir))
                throw new DosError(DosError.PathNotFound);

            // Convert to host name
            var hostFilename = _site.TryMapGuestPathToHost(guestFilename, access.HasFlag(FileAccess.Write));
            if (hostFilename == null)
                throw new DosError(DosError.AccessDenied);

            // Open the file
            try
            {
                var fs = new FileStream(hostFilename, mode, access, share);

                // Add to map
                File file = new File()
                {
                    fs = fs,
                    mode = mode,
                    access = access,
                    share = share,
                    handle = AllocateHandle(),
                    hostFilename = hostFilename,
                    guestFilename = guestFilename,
                };
                _openFiles.Add(file.handle, file);

                if (EnableFileLogging)
                    Log.WriteLine($"File opened {file.handle:X4}");

                return file;
            }
            catch (IOException x)
            {
                throw new DosError(x.HResult);
            }
        }

        public string QualifyPath(string filename)
        {
            if (filename == null)
                return null;

            // Non root drive letter? eg: B:BLAH
            if (filename.Length > 2 && filename[1]==':' && filename[2]!='\\')
            {
                var drive = DosPath.DriveFromPath(filename);
                CheckValidDrive(drive);

                filename = string.Format("{0}:{1}{2}", (char)('A' + drive), _driveInfo[drive].CurrentDirectory, filename.Substring(2));
            }

            if (!DosPath.IsFullyQualified(filename))
            {
                if (filename.StartsWith("\\"))
                {
                    filename = string.Format("{0}:{1}", (char)('A' + _currentDrive), filename);
                }
                else
                    filename = DosPath.Join(WorkingDirectory, filename);
            }

            return DosPath.CanonicalizePath(filename);
        }

        // Check if a guest filename exists
        public bool DoesFileExist(string guestFile)
        {
            if (guestFile == null)
                return false;

            // Qualify to current directory
            guestFile = QualifyPath(guestFile);

            var hostFile = _site.TryMapGuestPathToHost(guestFile, false);
            if (hostFile == null)
                return false;

            return System.IO.File.Exists(hostFile);
        }

        string QualifyVerifyAndMapFileName(string filename, bool checkExists)
        {
            // Qualify 
            filename = QualifyPath(filename);

            if (!DosPath.IsValid(filename))
                throw new DosError(DosError.PathNotFound);

            // Check path is valid
            var dir = System.IO.Path.GetDirectoryName(filename);
            if (!_site.DoesGuestDirectoryExist(dir))
                throw new DosError(DosError.PathNotFound);

            // Map to guest path
            var hostFileName = _site.TryMapGuestPathToHost(filename, true);
            if (hostFileName == null)
                throw new DosError(DosError.AccessDenied);

            if (checkExists && !System.IO.File.Exists(hostFileName))
                throw new DosError(DosError.FileNotFound);

            return hostFileName;
        }

        public void RenameFile(string oldFile, string newFile)
        {
            try
            {
                if (EnableFileLogging)
                    Log.WriteLine($"RenameFile '{oldFile}' to '{newFile}'");

                var oldHostPath = QualifyVerifyAndMapFileName(oldFile, false);   // Dont verify because could be directory name
                var newHostPath = QualifyVerifyAndMapFileName(newFile, false);

                System.IO.File.Move(oldHostPath, newHostPath);
            }
            catch (IOException x)
            {
                throw new DosError(x.HResult);
            }

        }

        public void DeleteFile(string filename)
        {
            try
            {
                if (EnableFileLogging)
                    Log.WriteLine($"DeleteFile: '{filename}'");

                // Qualify 
                var hostPath = QualifyVerifyAndMapFileName(filename, true);

                // Zap
                System.IO.File.Delete(hostPath);
            }
            catch (IOException x)
            {
                throw new DosError(x.HResult);
            }
        }

        // Create a file
        public ushort CreateFile(string filename, ushort attributes)
        {
            _lastError = 0;
            try
            {
                ushort handle = 0;
                try
                {
                    var file = OpenFileHandle(filename, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);

                    // Set its attributes
                    System.IO.File.SetAttributes(file.hostFilename, MapAttributes((byte)attributes));

                    return file.handle;
                }
                catch (IOException x)
                {
                    if (handle != 0)
                        CloseFile(handle);
                    throw new DosError(x.HResult);        // not sure about this
                }
            }
            catch (DosError x)
            {
                _lastError = x.errorCode;
                throw;
            }
        }

        // Open a file
        public ushort OpenFile(string filename, byte mode)
        {
            _lastError = 0;
            try
            {
                try
                {
                    var fam = (FileAccessMode)mode;

                    FileAccess access;
                    switch (fam & FileAccessMode.ReadWriteMask)
                    {
                        case FileAccessMode.Read: access = FileAccess.Read; break;
                        case FileAccessMode.Write: access = FileAccess.Write; break;
                        case FileAccessMode.ReadWrite: access = FileAccess.ReadWrite; break;
                        default:
                            return 0;
                    }

                    FileShare share;
                    switch (fam & FileAccessMode.ShareMask)
                    {
                        case FileAccessMode.ShareDenyAll: share = FileShare.None; break;
                        case FileAccessMode.ShareDenyWrite: share = FileShare.Read; break;
                        case FileAccessMode.ShareDenyRead: share = FileShare.Write; break;
                        case FileAccessMode.ShareDenyNone: share = FileShare.ReadWrite; break;

                        case FileAccessMode.ShareCompatiblity:
                        default:
                            share = FileShare.ReadWrite;
                            break;
                    }


                    return OpenFileHandle(filename, FileMode.Open, access, share).handle;
                }
                catch (IOException x)
                {
                    throw new DosError(x.HResult);        // not sure about this
                }
            }
            catch (DosError x)
            {
                _lastError = x.errorCode;
                throw;
            }
        }

        // Close file
        public bool CloseFile(ushort handle)
        {
            _lastError = 0;
            try
            {
                var file = FileFromHandle(handle);

                if (EnableFileLogging)
                {
                    Log.WriteLine($"Closing file: {handle:X4} '{file.guestFilename}'");
                }

                _openFiles.Remove(handle);

                try
                {
                    // Close the file
                    file.fs.Close();
                }
                catch
                {
                    // Don't care
                }
                return true;
            }
            catch (DosError x)
            {
                _lastError = x.errorCode;
                throw;
            }
        }

        public int SeekFile(ushort handle, int offset, short origin)
        {
            _lastError = 0;
            try
            {
                var file = FileFromHandle(handle);

                return (int)file.fs.Seek(offset, (SeekOrigin)origin);
            }
            catch (DosError x)
            {
                _lastError = x.errorCode;
                throw;
            }
        }

        const ushort HANDLE_STDIN = 0;

        // Read from a file
        public ushort ReadFile(ushort handle, ushort bytes, ushort seg, ushort offset)
        {
            _lastError = 0;
            try
            {
                /*
                if (handle == HANDLE_STDIN)
                {
                    ushort bytesRead = 0;
                    while (bytesRead < bytes)
                    {
                        int ch = Console.In.Read();
                        _cpu.Bus.WriteByte(seg, offset, (byte)ch);
                        bytesRead++;
                    }
                    return bytesRead;
                }
                */

                var file = FileFromHandle(handle);

                // Read data
                byte[] data;
                try
                {
                    data = file.fs.ReadBytes(bytes);
                }
                catch (IOException x)
                {
                    throw new DosError(x.HResult);
                }

                // Write back to machine
                _cpu.MemoryBus.WriteBytes(seg, offset, data);

                // Return length
                return (ushort)data.Length;
            }
            catch (DosError x)
            {
                _lastError = x.errorCode;
                throw;
            }
        }

        // Write to a file
        public ushort WriteFile(ushort handle, ushort bytes, ushort seg, ushort offset)
        {
            _lastError = 0;
            try
            {
                var file = FileFromHandle(handle);

                var buf = _cpu.MemoryBus.ReadBytes(seg, offset, bytes);

                // Read data
                try
                {
                    file.fs.Write(buf, 0, buf.Length);
                }
                catch (IOException x)
                {
                    throw new DosError(x.HResult);
                }

                //((Machine)_cpu).Debugger.Break();

                // Return length
                return bytes;
            }
            catch (DosError x)
            {
                _lastError = x.errorCode;
                throw;
            }
        }

        string MapGuestFileName(string guestFilename, bool forWrite)
        {
            // Qualify the guest filename
            guestFilename = QualifyPath(guestFilename);

            // Check the path exists
            var dir = System.IO.Path.GetDirectoryName(guestFilename);
            if (!_site.DoesGuestDirectoryExist(dir))
                throw new DosError(DosError.PathNotFound);

            // Convert to host name
            var hostFilename = _site.TryMapGuestPathToHost(guestFilename, forWrite);
            if (hostFilename == null)
                throw new DosError(DosError.AccessDenied);

            return hostFilename;
        }

        public ushort SetFileAttributes(string filename, ushort attributes)
        {
            try
            {
                var hostFile = MapGuestFileName(filename, true);
                System.IO.File.SetAttributes(hostFile, MapAttributes((byte)attributes));
                return GetFileAttributes(filename);
            }
            catch (IOException x)
            {
                throw new DosError(x.HResult);
            }
        }

        public ushort GetFileAttributes(string filename)
        {
            try
            {
                var hostFile = MapGuestFileName(filename, true);
                return (ushort)MapAttributes(System.IO.File.GetAttributes(hostFile));
            }
            catch (IOException x)
            {
                throw new DosError(x.HResult);
            }
        }

        ushort AllocateHandle()
        {
            ushort handle = 5;
            while (_openFiles.ContainsKey(handle))
                handle++;
            return handle;
        }

        Dictionary<ushort, File> _openFiles = new Dictionary<ushort, File>();

        public class File
        {
            public FileStream fs;
            public FileMode mode;
            public FileAccess access;
            public FileShare share;
            public ushort handle;
            public string hostFilename;
            public string guestFilename;
        }

        public IEnumerable<File> OpenFiles
        {
            get
            {
                foreach (var f in _openFiles.Values.OrderBy(x=>x.handle))
                {
                    yield return f;
                }
            }
        }

        [Flags]
        public enum FileAccessMode
        {
            Read = 0,
            Write = 1,
            ReadWrite = 2,
            ReadWriteMask = 3,

            ShareCompatiblity = 0,
            ShareDenyAll = 0x10,
            ShareDenyWrite = 0x20,
            ShareDenyRead = 0x30,
            ShareDenyNone = 0x40,
            ShareMask = 0x70,

            Inherit = 0x80,
        }

        public static class DosFileAttributes
        {
            public const byte Normal = 0;
            public const byte ReadOnly = 0x01;
            public const byte Hidden = 0x02;
            public const byte System = 0x04;
            public const byte VolumeLabel = 0x08;
            public const byte Directory = 0x10;
            public const byte Archive = 0x20;
        }

        static System.IO.FileAttributes MapAttributes(byte attr)
        {
            var fa = System.IO.FileAttributes.Normal;
            if ((attr & DosFileAttributes.ReadOnly)!=0)
                fa |= System.IO.FileAttributes.ReadOnly;
            if ((attr & DosFileAttributes.Hidden)!=0)
                fa |= System.IO.FileAttributes.Hidden;
            if ((attr & DosFileAttributes.System)!=0)
                fa |= System.IO.FileAttributes.System;
            if ((attr & DosFileAttributes.VolumeLabel)!=0)
                throw new IOException("Unsupported file attribute", unchecked((int)(0x80070005)));
            if ((attr & DosFileAttributes.Archive)!=0)
                fa |= System.IO.FileAttributes.Archive;

            return fa;
        }

        static byte MapAttributes(System.IO.FileAttributes attr)
        {
            var fa = 0;
            if (attr.HasFlag(System.IO.FileAttributes.Directory))
                fa |= DosFileAttributes.Directory;
            if (attr.HasFlag(System.IO.FileAttributes.ReadOnly))
                fa |= DosFileAttributes.ReadOnly;
            if (attr.HasFlag(System.IO.FileAttributes.Hidden))
                fa |= DosFileAttributes.Hidden;
            if (attr.HasFlag(System.IO.FileAttributes.System))
                fa |= DosFileAttributes.System;
            if (attr.HasFlag(System.IO.FileAttributes.Archive))
                fa |= DosFileAttributes.Archive;

            return (byte)fa;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        public struct FINDFILESTRUCT
        {
            public FINDFILESTRUCT(string name, FileInfo fi) : this(fi)
            {
                this.name = name;
            }
            public FINDFILESTRUCT(FileInfo fi) : this()
            {
                name = fi.Name.ToUpperInvariant();
                fileTime = (ushort)((fi.LastWriteTime.Second / 2) | (fi.LastWriteTime.Minute << 5) | (fi.LastWriteTime.Hour << 11));
                fileDate = (ushort)((fi.LastWriteTime.Day) | (fi.LastWriteTime.Month << 5) | ((fi.LastWriteTime.Year - 1980) << 9));
                attribute = (byte)MapAttributes(fi.Attributes);
                if ((attribute & (ushort)DosFileAttributes.Directory)==0)
                    fileSize = (uint)fi.Length;
            }
            public uint reserved1;
            public uint reserved2;
            public uint reserved3;
            public uint reserved4;
            public uint reserved5;
            public byte reserved6;
            public byte attribute;
            public ushort fileTime;
            public ushort fileDate;
            public uint fileSize;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 13)]
            public string name;

            public override string ToString()
            {
                return $"file:{name} attr:{attribute:X4} size: {fileSize}";
            }
        };

        Dictionary<string, FINDFILESTRUCT> _findFiles;
        List<FINDFILESTRUCT> _foundFiles;

        void EnumFolder(string hostPath, string spec)
        {
            if (hostPath == null)
                return;

            foreach (var f in System.IO.Directory.EnumerateFiles(hostPath))
            {
                var fi = new FileInfo(f);
                if (!DosPath.IsValidElement(fi.Name) || !DosPath.GlobMatch(spec, fi.Name))
                    continue;

                var ffs = new FINDFILESTRUCT(fi);
                _findFiles[ffs.name] = ffs;
            }
            foreach (var d in System.IO.Directory.EnumerateDirectories(hostPath))
            {
                var fi = new FileInfo(d);
                if (!DosPath.IsValidElement(fi.Name) || !DosPath.GlobMatch(spec, fi.Name))
                    continue;

                var ffs = new FINDFILESTRUCT(fi);
                _findFiles[ffs.name] = ffs;
            }
        }

        public static string EnsureEndsWithSlash(string str)
        {
            if (str.EndsWith("\\"))
                return str;
            else
                return str + "\\";
        }

        static string GetGuestDirectoryName(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            int index = path.LastIndexOf('\\');
            if (index < 0)
                return string.Empty;

            return path.Substring(0, index + 1);
        }

        static string GetGuestFileName(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            int index = path.LastIndexOf('\\');
            return index >= 0 ? path.Substring(index + 1) : path;
        }

        public void FindFiles(string spec, byte attributes)
        {
            int size = Marshal.SizeOf<FINDFILESTRUCT>();
            System.Diagnostics.Debug.Assert(size == 43);

            _findFiles = null;
            _foundFiles = null;

            // Get the fully qualified spec
            var fullspec = QualifyPath(spec);

            // Just querying single folder?  Remove trailing backslash
            if (fullspec.EndsWith("\\") && fullspec.Length > 3)
            {
                fullspec = fullspec.Substring(fullspec.Length - 1);
                if (!_site.DoesGuestDirectoryExist(fullspec))
                {
                    throw new DosError(DosError.PathNotFound);
                }
            }

            // Just drive letter? eg: "C:\"
            if (fullspec.Length == 3)
                return;

            _findFiles = new Dictionary<string, FINDFILESTRUCT>();

            string folder = EnsureEndsWithSlash(GetGuestDirectoryName(fullspec).TrimEnd('\\'));
            string glob = GetGuestFileName(fullspec);

            // If not root folder, also include the parent folder
            if (folder.Length > 3)
            {
                var parentFolder = GetGuestDirectoryName(folder.TrimEnd('\\'));
                var readOnlyParentFolder = _site.TryMapGuestPathToHost(parentFolder, false);
                if (readOnlyParentFolder != null)
                {
                    _findFiles[".."] = new FINDFILESTRUCT("..", new FileInfo(readOnlyParentFolder));
                }

                var writeParentFolder = _site.TryMapGuestPathToHost(parentFolder, true);
                if (writeParentFolder != null)
                {
                    _findFiles[".."] = new FINDFILESTRUCT("..", new FileInfo(writeParentFolder));
                }
            }

            // Scan read-only folder
            var readOnlyFolder = _site.TryMapGuestPathToHost(folder, false);

            if (readOnlyFolder!= null)
                _findFiles["."] = new FINDFILESTRUCT(".", new FileInfo(readOnlyFolder));

            EnumFolder(readOnlyFolder, glob);

            // Scan write folder
            var writeFolder = _site.TryMapGuestPathToHost(folder, true);
            if (writeFolder != null && writeFolder != readOnlyFolder)
            {
                _findFiles["."] = new FINDFILESTRUCT(".", new FileInfo(writeFolder));
                EnumFolder(writeFolder, glob);
            }

            // Enumerate virtual sub folders
            foreach (var vsf in _site.GetVirtualSubFolders(folder))
            {
                var e = new FINDFILESTRUCT()
                {
                    name = vsf,
                    attribute = (byte)DosFileAttributes.Directory,
                };
                _findFiles[e.name] = e;
            }

            // Setup list
            _foundFiles = _findFiles.Values.Where(x=>DoesFileMatchAttribute(x.attribute, attributes)).OrderBy(x=>x.name).ToList();
            _findFiles = null;

            if (EnableFileLogging)
            {
                Log.WriteLine($"FindFiles: {spec} attributes: {attributes:X2} dir: {WorkingDirectory}");
                foreach (var ffs in _foundFiles)
                {
                    Log.WriteLine($"    - {ffs}");
                }
                Log.WriteLine($"Found {_foundFiles.Count} files");
            }
        }

        public bool FindNextFile(out FINDFILESTRUCT ffs)
        {
            if (_foundFiles == null || _foundFiles.Count == 0)
            {
                ffs = new FINDFILESTRUCT();
                return false;
            }

            ffs = _foundFiles[0];
            _foundFiles.RemoveAt(0);

            return true;
        }

        public bool DoesFileMatchAttribute(byte fileAttributes, byte filterAttributes)
        {
            // Volume label?
            if ((filterAttributes & DosFileAttributes.VolumeLabel)!=0)
                return (fileAttributes & DosFileAttributes.VolumeLabel)!= 0;

            //if ((filterAttributes & DosFileAttributes.Directory) != 0)
            //    return (fileAttributes & DosFileAttributes.Directory) != 0;

            // Ignore archive bit
            fileAttributes = (byte)(fileAttributes & ~DosFileAttributes.Archive);
            filterAttributes = (byte)(filterAttributes & ~DosFileAttributes.Archive);

            // Normal or special 
            return fileAttributes == 0 || (fileAttributes & filterAttributes) != 0;
        }

        void TryFindFirstFileFCB()
        {
            var fcbRequest = _cpu.MemoryBus.ReadBytes(_cpu.ds, _cpu.dx, 37);
            byte attributes;
            string spec = GetFcbSearchSpec(fcbRequest, out attributes);

            try
            {
                FindFiles(spec, attributes);
                TryFindNextFileFCB();
            }
            catch (DosError x) when (x.errorCode == DosError.FileNotFound || x.errorCode == DosError.PathNotFound || x.errorCode == DosError.NoMoreFiles)
            {
                _cpu.al = 0xFF;
                _cpu.FlagC = false;
            }
        }

        void TryFindNextFileFCB()
        {
            var fcb = _cpu.MemoryBus.ReadBytes(_cpu.ds, _cpu.dx, 37);
            try
            {
                FindNextFileFCB(fcb, _cpu.ds, _cpu.dx);
                _cpu.al = 0x00;
                _cpu.FlagC = false;
            }
            catch (DosError x) when (x.errorCode == DosError.NoMoreFiles)
            {
                _cpu.al = 0xFF;
                _cpu.FlagC = false;
            }
        }

        string GetFcbSearchSpec(byte[] fcbRequest, out byte attributes)
        {
            bool isExtended = fcbRequest[0] == 0xFF;
            int driveOffset = isExtended ? 7 : 0;
            int nameOffset = isExtended ? 8 : 1;
            int extOffset = isExtended ? 16 : 9;

            attributes = isExtended ? fcbRequest[6] : (byte)0;

            byte drive = fcbRequest[driveOffset];
            if (drive == 0)
                drive = (byte)_currentDrive;
            else
                drive--;

            var ansi = System.Text.Encoding.GetEncoding(1252);
            var filename = ansi.GetString(fcbRequest, nameOffset, 8).TrimEnd();
            var ext = ansi.GetString(fcbRequest, extOffset, 3).TrimEnd();
            return $"{(char)('A' + drive)}:{filename}.{ext}";
        }

        void FindNextFileFCB(byte[] fcbRequest, ushort seg, ushort offset)
        {
            FINDFILESTRUCT ffs;
            if (FindNextFile(out ffs))
            {
                bool isExtended = fcbRequest[0] == 0xFF;
                int nameOffset = isExtended ? 8 : 1;
                int extOffset = isExtended ? 16 : 9;

                if (isExtended)
                    fcbRequest[6] = ffs.attribute;

                var ansi = System.Text.Encoding.GetEncoding(1252);
                var name = ansi.GetBytes((System.IO.Path.GetFileNameWithoutExtension(ffs.name).ToUpperInvariant() + "        ").Substring(0, 8));
                var ext = ansi.GetBytes((System.IO.Path.GetExtension(ffs.name).TrimStart('.').ToUpperInvariant() + "   ").Substring(0, 3));

                Array.Copy(name, 0, fcbRequest, nameOffset, 8);
                Array.Copy(ext, 0, fcbRequest, extOffset, 3);
                _cpu.MemoryBus.WriteBytes(seg, offset, fcbRequest, fcbRequest.Length);
                return;
            }

            throw new DosError(DosError.NoMoreFiles);
        }


        public IEnumerable<int> EnumDrives()
        {
            for (int i=0; i<26; i++)
            {
                if (GetDriveType(i) != DriveType.None)
                    yield return i;
            }
        }
    }
}
