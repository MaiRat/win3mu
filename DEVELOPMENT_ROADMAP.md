# Win3mu Development Roadmap

This roadmap summarizes the highest-value improvements identified in the current codebase for the Win3.x emulator.

## Current state

- **Sharp86 is intentionally limited to 8086-era execution plus host-provided selector support.** The Sharp86 readme explicitly describes the current model as a pseudo-protected mode and notes that protected-mode instructions are not available (`/home/runner/work/win3mu/win3mu/Sharp86/readme.md`).
- **The runtime's export-forwarding model is broadly the right approach to keep.** Most compatibility gaps are in the thunking, message conversion, and edge-case handling around the 16-bit to host bridge (`/home/runner/work/win3mu/win3mu/Win3muCore/Core/Module32.cs`).

## Priority 1: close compatibility blockers in the runtime layer

These items are likely to unblock more real applications faster than a large CPU rewrite.

1. ~~**Finish hook bridging and default hook processing**~~ ✅ **COMPLETED**
   - `OldHookProcProxy` now supports WH_MSGFILTER, WH_SYSMSGFILTER, WH_GETMESSAGE, WH_KEYBOARD, WH_MOUSE, and WH_CALLWNDPROC with proper message conversion.
   - `DefHookProc` now handles all common hook types with appropriate 16↔32 marshaling.

2. ~~**Return the real window-proc result from message dispatch**~~ ✅ **COMPLETED**
   - `SendMessage` now properly returns the dispatched WNDPROC result via `CallWndProc32from16`.

3. ~~**Support `lpParam` / `CREATESTRUCT.lpCreateParams` during window creation**~~ ✅ **COMPLETED**
   - `CreateWindowEx` now accepts and forwards `lpParam` to the 32-bit API; `WM_NCCREATE` CREATESTRUCT conversion handles `lpCreateParams`.

4. **Broaden the thunking layer before adding more forwarded exports** — _in progress_
    - `Module32` now supports `IntPtr` as a return type and allows non-null `IntPtr` parameters (converting seg:offset to host pointers) instead of requiring the `MustBeNull` attribute.
    - `Module32` now also handles undecorated value-type struct parameters by value, and returns 2-byte/4-byte undecorated structs through AX/DX:AX.
    - **Remaining:** larger or non-register return conventions still need dedicated handling if future forwarded exports require them.

## Priority 2: expand Sharp86 instruction coverage for Win3.x workloads

1. ~~**Implement the missing `TEST` group variants**~~ ✅ **COMPLETED**
   - Opcode groups `F6 /1` and `F7 /1` are now implemented with correct `TEST` semantics. Disassembler also updated.

2. ~~**Add a staged plan for `0x0F` extended opcode decoding**~~ ✅ **COMPLETED**
   - Two-byte opcode decoding is implemented and covers: conditional jumps (Jcc), SETcc, MOVZX, MOVSX, BSF/BSR, BT/BTS/BTR/BTC, XADD, CMPXCHG, IMUL r16/r/m16, SHLD, SHRD, PUSH/POP FS/GS, LFS/LGS/LSS, and segment-override prefixes 64h/65h.

3. ~~**Define a protected-mode support boundary**~~ ✅ **COMPLETED** — _compatibility-first path chosen_
   - Sharp86 now implements the subset of protected-mode instructions needed by Win3.x: SLDT, STR, LLDT, LTR, VERR, VERW, SGDT, SIDT, LGDT, LIDT, SMSW, LMSW, CLTS, LAR, LSL, and ARPL.
   - These are backed by emulated descriptor-table state in Win3mu's global heap and machine objects.

## Priority 3: fill subsystem gaps that affect specific app classes

1. ~~**Multimedia**~~ ✅ **COMPLETED**
   - `mciSendCommand` now supports MCI_OPEN, MCI_CLOSE, MCI_PLAY, MCI_STATUS, MCI_STOP, MCI_PAUSE, MCI_SEEK, MCI_SET, MCI_GETDEVCAPS, MCI_INFO, MCI_RECORD, MCI_RESUME, MCI_SAVE, MCI_LOAD, and many more generic MCI commands.
   - `mciSendString` is now implemented, providing the string-based MCI interface used by many multimedia applications.
   - `mciGetErrorString` is fully implemented.
   - `MCI_SYSINFO` now marshals 16↔32-bit parameters (including QUANTITY integer results and string returns).
   - `MCI_WINDOW`, `MCI_PUT`, `MCI_WHERE`, and `MCI_UPDATE` are forwarded as generic commands, allowing the host MCI driver to handle video/animation window management.
   - Unsupported MCI commands now return MCIERR_UNSUPPORTED_FUNCTION instead of throwing.

2. **DOS and multiplex interrupts** — _substantially expanded_
   - `Int 1Ah` services 0–5 (timer count get/set, RTC time get/set, RTC date get/set) are implemented.
   - `Int 21h` now supports additional services: 0x29 (Parse Filename into FCB), 0x33 (Get/Set System Values including Ctrl-C check flag and boot drive), 0x34 (InDOS Flag), 0x36 (Get Disk Free Space), 0x38 (Get/Set Country Info), 0x39 (Create Directory), 0x3A (Remove Directory), 0x48-0x4A (memory allocation stubs), 0x51/0x62 (Get PSP), 0x57 (Get/Set File Date and Time), 0x5B (Create New File), 0x60 (Truename/Fully Qualified Filename), and 0x66 (Get/Set Global Code Page).
   - `Int 21h/44h` IOCTL now supports subfunctions 00h (Get Device Info), 01h (Set Device Info), 02h (Read From Character Device), 03h (Write To Character Device), 04h (Read From Block Device), 05h (Write To Block Device), 06h (Get Input Status), 07h (Get Output Status), 08h (Check Removable Media), 09h (Check Remote Device), 0Ah (Check Remote Handle), 0Bh (Set Sharing Retry Count), 0Eh (Get Logical Drive Map), and 0Fh (Set Logical Drive Map). Unsupported IOCTL subfunctions set carry and log the specific subfunction number.
   - Unsupported `Int 1Ah` services, unsupported DOS interrupt functions (`Int 21h`), and unsupported multiplex interrupt services (`Int 2Fh`) now log a warning and return gracefully (setting carry flag / error codes) instead of throwing `NotImplementedException`.
   - **Remaining:** specific services used by installers can be added as compatibility testing reveals them.

3. **Cross-task window enumeration** — _fully implemented_
   - `EnumTaskWindows` now succeeds without enumeration for non-current-task requests, rather than throwing.
   - `EnumWindows` (ordinal 0x0036) is now implemented, delegating to Win32 `EnumWindows` with proper 16↔32-bit callback marshaling.
   - `EnumChildWindows` (ordinal 0x0037) is now implemented, delegating to Win32 `EnumChildWindows` with proper 16↔32-bit callback marshaling.
   - Both functions push a 16-bit HWND and DWORD lParam onto the VM stack and invoke the 16-bit callback via `CallVM`.

4. **Control message semantics** — _substantially expanded_
   - Button: `BM_GETCHECK`, `BM_SETCHECK`, `BM_GETSTATE`, `BM_SETSTATE`, `BM_SETSTYLE` are now fully mapped with correct parameter semantics.
   - Static: `STM_SETICON` and `STM_GETICON` now use proper GDI object handle mapping.
   - Edit: 27 messages implemented including `EM_GETSEL`, `EM_SETSEL` (with Win16 packed-lParam cracking), `EM_GETRECT` (RECT output), `EM_SETRECT`/`EM_SETRECTNP` (RECT input), `EM_LINESCROLL` (with parameter cracking), `EM_REPLACESEL` (with string conversion), `EM_GETMODIFY`, `EM_SETMODIFY`, `EM_GETLINECOUNT`, `EM_LINEINDEX`, `EM_LINELENGTH`, `EM_GETLINE` (buffer with word-length prefix), `EM_CANUNDO`, `EM_UNDO`, `EM_FMTLINES`, `EM_LINEFROMCHAR`, `EM_SETTABSTOPS` (int array marshalling), `EM_SETPASSWORDCHAR`, `EM_EMPTYUNDOBUFFER`, `EM_GETFIRSTVISIBLELINE`, `EM_SETREADONLY`, `EM_GETPASSWORDCHAR`, `EM_SETLIMITTEXT`, `EM_SETHANDLE` (stub with logging), `EM_GETHANDLE` (stub with logging), `EM_SETWORDBREAKPROC` (stub with logging), and `EM_GETWORDBREAKPROC` (stub with logging).
   - ListBox: 24 messages implemented including `LB_DELETESTRING`, `LB_SETSEL`, `LB_GETSEL`, `LB_GETTEXTLEN`, `LB_GETCOUNT`, `LB_SELECTSTRING`, `LB_GETSELCOUNT`, `LB_GETHORIZONTALEXTENT`, `LB_SETHORIZONTALEXTENT`, `LB_SETCOLUMNWIDTH`, `LB_GETITEMDATA`, `LB_SETITEMDATA`, `LB_SELITEMRANGE`, `LB_SETCARETINDEX`, `LB_SETITEMHEIGHT`, `LB_GETITEMHEIGHT`, `LB_FINDSTRINGEXACT`, `LB_GETITEMRECT` (RECT output), `LB_GETSELITEMS` (int array buffer output), `LB_SETTABSTOPS` (int array marshalling), plus existing `LB_ADDSTRING`, `LB_INSERTSTRING`, `LB_GETTEXT`, `LB_FINDSTRING`, etc.
   - ComboBox: 20 messages implemented including `CB_GETEDITSEL`, `CB_LIMITTEXT`, `CB_SETEDITSEL`, `CB_DELETESTRING`, `CB_DIR`, `CB_GETLBTEXTLEN`, `CB_SELECTSTRING`, `CB_SHOWDROPDOWN`, `CB_SETITEMHEIGHT`, `CB_GETITEMHEIGHT`, `CB_SETEXTENDEDUI`, `CB_GETEXTENDEDUI`, `CB_GETDROPPEDSTATE`, `CB_FINDSTRINGEXACT`, `CB_GETDROPPEDCONTROLRECT` (RECT output), plus existing `CB_ADDSTRING`, `CB_INSERTSTRING`, `CB_GETLBTEXT`, `CB_FINDSTRING`, etc.
   - The old commented-out `notimpl()` block has been replaced entirely with proper semantics.
   - **Remaining:** none — all standard Win3.x edit control messages are now covered. Full cross-bitness callback thunking for `EM_SETWORDBREAKPROC` can be added if applications require functional word-break callbacks.

5. **GDI robustness** — _improved_
   - `CreateBrushIndirect` now handles BS_NULL/BS_HOLLOW brush style and falls back gracefully for unknown brush styles instead of throwing.
   - `GetObject` now logs and returns 0 for unsupported GDI object types instead of throwing.
   - `BS_DIBPATTERN` brush conversion now degrades gracefully to a solid brush instead of throwing.

5. **Kernel process termination** — _implemented_
   - `FatalExit` and `FatalAppExit` now log, display a message box (for FatalAppExit), and cleanly terminate the emulated process instead of throwing `NotImplementedException`.

6. **Module loading robustness** — _fully implemented_
   - NE relocation processing now supports all six relocation address types: `LowByte` (type 0), `Selector` (type 2), `Pointer32` (type 3), `Offset16` (type 5), `Pointer48` (type 11), and `Offset32` (type 13) for `InternalReference`, `ImportedOrdinal`, and `ImportedName` relocations.
   - Unknown relocation address types and unknown relocation types now log a warning and skip instead of crashing module load.
   - Unknown FP OSFixup tribyte combinations log a warning and emit a NOP instead of crashing module load.
   - Unknown FP OSFixup two-byte opcode patterns now emit two NOP bytes and log a warning instead of throwing `NotImplementedException`.

7. **Port I/O support** — _implemented_
   - Machine now implements `IPortBus`, enabling `IN`/`OUT` word and byte instructions to execute without crashing.
   - Port reads return 0xFF and port writes are logged and ignored, matching the expected behavior for Win3.x applications that probe hardware ports.

8. **Module and message robustness** — _improved_
   - `Module32.Uninit` is now a no-op instead of throwing `NotImplementedException`.
   - `RegisteredWindowMessages` logs a warning for messages outside the 16-bit range instead of throwing.
   - `copy_zero` message handler passes through non-zero lParam values with logging instead of throwing (fixes dead-code path).
   - Unreachable `NotImplementedException` throws after `ThrowMessageError` in `Messaging.cs` have been removed.
   - `notimpl` message conversion now logs a warning and passes parameters through as a copy instead of throwing `NotImplementedException`.

9. **Memory management robustness** — _improved_
   - `RangeAllocator` shrink operation is now implemented, allowing address space reduction when trailing space is free instead of throwing `NotImplementedException`.

10. **Shell module** — _implemented_
    - `RegOpenKey`, `RegCreateKey`, `RegCloseKey`, `RegDeleteKey`, `RegSetValue`, `RegQueryValue`, `RegEnumKey` are implemented as stubs returning appropriate error codes (Win3.x registry was minimal).
    - `DragAcceptFiles`, `DragQueryFile`, `DragFinish` are implemented as stubs for file drag-drop support.
    - `ExtractIcon` delegates to Win32 `ExtractIcon` with proper GDI handle mapping.
    - `ShellExecute` delegates to Win32 `ShellExecuteW` with guest-to-host path support.
    - `ShellAbout` delegates to Win32 `ShellAboutW`.
    - `FindExecutable`, `DoEnvironmentSubst`, `RegisterShellHook` are implemented as stubs.

11. **Window/class accessor consistency** — _improved_
    - `SetClassWord` now handles `GCW_STYLE` (delegating to `SetClassLong`), consistent with `GetClassWord`.
    - `SetWindowWord` now handles `GWW_HWNDPARENT` (using `SetWindowLongPtr`), consistent with `GetWindowWord`.

12. **Disassembler robustness** — _improved_
    - Register/opcode format methods now include descriptive error messages in `NotImplementedException` to aid debugging.
    - `Group2Name` now handles subcode 6 (undocumented SHL alias) instead of throwing.
    - `ConDos` port I/O stubs now return 0xFF for reads and ignore writes (matching Machine's `IPortBus` pattern) instead of throwing `NotImplementedException`.

## Recommended execution order

1. ~~Add focused unit tests around the currently known `NotImplementedException` and invalid-opcode paths.~~ ✅
2. ~~Fix runtime blockers first: hook bridging, WNDPROC return values, `lpCreateParams`, and thunking support.~~ ✅
3. ~~Implement the missing `TEST` instruction variants.~~ ✅
4. ~~Introduce staged `0x0F` decoding and document which extended/protected-mode instructions are intentionally still unsupported.~~ ✅
5. ~~Expand subsystem coverage for MCI, DOS interrupts, and cross-task window APIs based on app compatibility testing.~~ ✅
6. ~~Implement control message semantics for standard Windows controls (Button, Static, Edit, ListBox, ComboBox).~~ ✅
7. ~~Implement complex pointer-based control message Callables (RECT, buffer, array marshalling).~~ ✅
8. ~~Eliminate crash-causing `NotImplementedException` paths in port I/O, module lifecycle, message conversion, and GDI brush handling.~~ ✅
9. ~~Expand DOS Int 21h coverage with filesystem, directory, FCB parsing, country info, PSP, and code page services.~~ ✅
10. ~~Complete NE relocation address type coverage (`Pointer48`, `Offset32`) and harden FP OSFixup fallback.~~ ✅
11. ~~Expand IOCTL subfunction coverage and harden message conversion fallback paths.~~ ✅
12. ~~Implement handle/callback control messages (`EM_SETHANDLE`/`EM_GETHANDLE`, `EM_SETWORDBREAKPROC`/`EM_GETWORDBREAKPROC`) and harden remaining relocation/memory crash paths.~~ ✅

## Next steps

The following items represent the current frontier for further work:

1. ~~**Implement complex control message Callables**~~ ✅ **COMPLETED**
   - Custom `Callable` implementations added for all pointer-based control messages: `EM_GETRECT`/`EM_SETRECT`/`EM_SETRECTNP` (RECT pointer), `EM_GETLINE` (buffer with word-length prefix), `EM_SETTABSTOPS`/`LB_SETTABSTOPS` (int array marshalling), `LB_GETITEMRECT` (RECT pointer), `LB_GETSELITEMS` (int array buffer), `CB_GETDROPPEDCONTROLRECT` (RECT pointer).
2. ~~**Harden crash paths and port I/O**~~ ✅ **COMPLETED**
   - Port I/O: Machine implements `IPortBus` with logging stubs for IN/OUT instructions.
   - Module32.Uninit, RegisteredWindowMessages, copy_zero, BS_DIBPATTERN conversion, and Messaging.cs error paths all hardened against crashes.
3. ~~**Expand DOS service coverage**~~ ✅ **COMPLETED**
   - Added Int 21h services: 0x29 (Parse Filename into FCB), 0x34 (InDOS Flag), 0x38 (Get/Set Country Info), 0x39 (Create Directory), 0x3A (Remove Directory), 0x51/0x62 (Get PSP), 0x5B (Create New File), 0x60 (Truename), 0x66 (Get/Set Code Page).
4. ~~**Add remaining relocation types**~~ ✅ **COMPLETED**
   - `Pointer48` (48-bit far pointer: 32-bit offset + 16-bit selector) and `Offset32` (32-bit offset) relocation address types are now implemented for `InternalReference`, `ImportedOrdinal`, and `ImportedName` relocations.
   - All six NE relocation address types are now covered.
5. ~~**Add remaining IOCTL subfunctions**~~ ✅ **COMPLETED**
   - Added Int 21h/44h subfunctions: 02h (Read From Character Device), 03h (Write To Character Device), 04h (Read From Block Device), 05h (Write To Block Device), 09h (Check Remote Device), 0Ah (Check Remote Handle), 0Bh (Set Sharing Retry Count), 0Eh (Get Logical Drive Map), 0Fh (Set Logical Drive Map).
6. ~~**Harden message conversion crash paths**~~ ✅ **COMPLETED**
   - `notimpl` message Postable now logs a warning and passes parameters through as a copy instead of throwing `NotImplementedException`.
   - Unknown FP OSFixup two-byte opcode patterns now emit NOP bytes and log a warning instead of throwing.
7. ~~**Add handle/callback control messages**~~ ✅ **COMPLETED**
   - `EM_SETHANDLE`/`EM_GETHANDLE` now have stub Callable implementations that log and gracefully degrade (the 32-bit edit control manages its own buffer).
   - `EM_SETWORDBREAKPROC`/`EM_GETWORDBREAKPROC` now have stub Callable implementations that log and return NULL (cross-bitness callback thunking deferred).
8. ~~**Harden relocation and memory crash paths**~~ ✅ **COMPLETED**
   - Unknown relocation address types for `InternalReference`, `ImportedOrdinal`, and `ImportedName` now log a warning and skip instead of throwing `NotImplementedException`.
   - Unknown relocation types now log a warning and skip instead of throwing.
   - `RangeAllocator` shrink operation is now implemented with proper free-tail validation instead of throwing `NotImplementedException`.
9. ~~**Implement Shell module and window enumeration**~~ ✅ **COMPLETED**
   - Shell module now implements registry stubs (RegOpenKey, RegCreateKey, RegCloseKey, RegDeleteKey, RegSetValue, RegQueryValue, RegEnumKey), drag-drop stubs (DragAcceptFiles, DragQueryFile, DragFinish), ExtractIcon, ShellExecute, ShellAbout, FindExecutable, DoEnvironmentSubst, and RegisterShellHook.
   - `EnumWindows` and `EnumChildWindows` are now implemented with proper 16↔32-bit callback marshaling.
   - Window/class accessor consistency: `SetClassWord` handles `GCW_STYLE`; `SetWindowWord` handles `GWW_HWNDPARENT`.
   - Disassembler `Group2Name` subcode 6 (undocumented SHL alias) and ConDos port I/O stubs hardened.
10. ~~**Continue broadening thunking support**~~ ✅ **COMPLETED**
    - Module32 thunking layer now supports enum parameter types, enum return types, and enum-to-underlying-type conversion throughout `SizeOfType16`, `ReadParamFromStack`, and `SetReturnValue`.
    - `StringBuilder` buffer size parameter resolution now supports `short`, `uint`, and `nuint` types in addition to `int`, `ushort`, and `nint`.
11. **Extend MCI device-specific support** — _expanded_
    - `MCI_OPEN` now detects digital-video open flags and marshals `MCI_DGV_OPEN_PARMS`, including `dwStyle` and `hWndParent`, instead of always using the generic open structure.
    - `MCI_WINDOW`, `MCI_PUT`, `MCI_WHERE`, and `MCI_UPDATE` now marshal device-specific window/RECT/HDC structures instead of passing them through as generic callbacks.
    - **Remaining:** additional device-specific MCI commands can be added as multimedia applications reveal new gaps.
12. ~~**Implement Sound and DdeML modules**~~ ✅ **COMPLETED**
    - Sound module now implements all 16 standard Win3.x SOUND.DLL exports: `OpenSound` (1), `CloseSound` (2), `SetVoiceQueueSize` (3), `SetVoiceNote` (4), `SetVoiceAccent` (5), `SetVoiceEnvelope` (6), `SetSoundNoise` (7), `SetVoiceSound` (8), `StartSound` (9), `StopSound` (10), `WaitSoundState` (11), `SyncAllVoices` (12), `CountVoiceNotes` (13), `GetThresholdEvent` (14), `GetThresholdStatus` (15), `SetVoiceThreshold` (16). All return success/empty values since the voice-queue synthesizer has no modern equivalent.
    - DdeML module now implements 27 DDEML.DLL exports: `DdeInitialize` (2), `DdeUninitialize` (3), `DdeConnectList` (4), `DdeQueryNextServer` (5), `DdeDisconnectList` (6), `DdeConnect` (7), `DdeDisconnect` (8), `DdeReconnect` (9), `DdeQueryConvInfo` (10), `DdeSetUserHandle` (11), `DdeAbandonTransaction` (12), `DdePostAdvise` (13), `DdeEnableCallback` (14), `DdeNameService` (16), `DdeClientTransaction` (17), `DdeCreateDataHandle` (18), `DdeAddData` (19), `DdeGetData` (20), `DdeAccessData` (21), `DdeUnaccessData` (22), `DdeFreeDataHandle` (23), `DdeGetLastError` (24), `DdeCreateStringHandle` (25), `DdeFreeStringHandle` (26), `DdeQueryString` (27), `DdeKeepStringHandle` (28), `DdeCmpStringHandles` (36). Stubs return appropriate error/null values; `DdeGetLastError` tracks per-instance errors; `DdeCreateStringHandle` assigns incrementing handles.

## Next steps

The following items represent the current frontier for further work:

1. **Extend MCI device-specific support** — implement device-specific MCI command extensions as multimedia applications reveal gaps.
2. ~~**Implement Comm module**~~ ✅ **COMPLETED**
   - `COMM.DRV` now implements the standard 16 serial communication exports (`OpenComm`, `SetCommState`, `GetCommState`, `GetCommError`, `ReadComm`, `WriteComm`, `TransmitCommChar`, `CloseComm`, `SetCommEventMask`, `GetCommEventMask`, `SetCommBreak`, `ClearCommBreak`, `UngetCommChar`, `BuildCommDCB`, `EscapeCommFunction`, `FlushComm`) with compatibility-first in-memory behavior.
   - Matching USER exports (ordinals `00C8`-`00D7` and `00F5`) now forward to the same stubbed comm state, avoiding unsupported-ordinal crashes for applications that import the older USER entry points directly.
   - `BuildCommDCB` now parses classic serial specs such as `COM1:96,n,8,1` into a Win16-compatible DCB structure, and `Get/SetCommState` round-trip that state.
3. **Expand GDI coverage** — _expanded_
   - Added classic mapping/state exports for `OffsetWindowOrg`, `ScaleWindowExt`, `OffsetViewportOrg`, `ScaleViewportExt`, `GetPolyFillMode`, `GetTextCharacterExtra`, `GetTextFace`, `GetViewportExt`, `GetViewportOrg`, `GetWindowExt`, `GetWindowOrg`, `GetBrushOrg`, `GetBitmapDimension`, `SetBitmapDimension`, and `GetAspectRatioFilter`.
   - Added additional classic drawing/bitmap exports for `SetPolyFillMode`, `SetTextCharacterExtra`, `SetTextJustification`, `Pie`, `Chord`, `CreateBitmapIndirect`, `SetBitmapBits`, `SetDIBits`, `GetDIBits`, and `PolyPolygon`.
   - Added region/drawing exports for `OffsetClipRgn`, `CreateEllipticRgn`, `CreateEllipticRgnIndirect`, `CreatePolygonRgn`, `CreateRectRgnIndirect`, `CreatePolyPolygonRgn`, `EqualRgn`, `OffsetRgn`, `SelectVisRgn`, `GetRgnBox`, `PtInRegion`, `GetClipRgn`, `RectInRegion`, `ExtFloodFill`, and `CreateRoundRectRgn`, plus text-width export `GetCharWidth`.
   - Added classic metafile exports for `GetMetaFile`, `CreateMetaFile`, `CloseMetaFile`, `CopyMetaFile`, `GetMetaFileBits`, `SetMetaFileBitsBetter`, `EnumMetaFile`, `PlayMetaFileRecord`, and `IsValidMetaFile`, plus print-abort helper `QueryAbort`.
   - Added palette exports for `SelectPalette`, `RealizePalette`, `GetPaletteEntries`, `SetPaletteEntries`, `RealizeDefaultPalette`, `UpdateColors`, `AnimatePalette`, `ResizePalette`, `GetNearestPaletteIndex`, `SetSystemPaletteUse`, and `GetSystemPaletteUse`.
   - Added printer/path exports for `Escape`, `StartDoc`, `EndDoc`, `StartPage`, `EndPage`, `SetAbortProc`, `AbortDoc`, `SetBoundsRect`, `GetBoundsRect`, and `SelectBitmap`, with 16-bit `DOCINFO` marshaling and abort-proc callback bridging.
   - **Remaining:** additional printer-driver-specific GDI exports can be added as application compatibility testing reveals specific gaps.
4. ~~**Implement functional DDE string handles**~~ ✅ **COMPLETED**
   - `DdeCreateStringHandle` now maintains a real `(string, codepage)`→handle table with reference counting, allowing duplicate creates to reuse the same HSZ.
   - `DdeQueryString`, `DdeKeepStringHandle`, `DdeFreeStringHandle`, and `DdeCmpStringHandles` now operate on the stored string values instead of dummy incrementing handles.

## Expected outcome

Following this order keeps the existing host-forwarding design, improves compatibility where the current bridge is already close to working, and avoids taking on a full protected-mode CPU project before the runtime layer can benefit from it.
