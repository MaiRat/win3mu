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
   - **Remaining:** unsupported parameter/return types still throw `NotImplementedException` when novel type shapes appear (e.g. custom structs not decorated with `MappedTypeAttribute`). These should be addressed as real Win3.x DLL entry points expose them.

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

3. **Cross-task window enumeration** — _virtualized_
   - `EnumTaskWindows` now succeeds without enumeration for non-current-task requests, rather than throwing.
   - **Remaining:** full cross-task enumeration is not yet emulated; the current approach virtualizes the result for compatibility.

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
9. **Continue broadening thunking support** — add marshaling for parameter/return shapes encountered when testing real Win3.x applications against additional forwarded DLL exports.
10. **Evaluate cross-task window enumeration** — determine from application testing whether full enumeration is needed or the virtualized approach is sufficient.
11. **Extend MCI device-specific support** — implement device-specific MCI command extensions as multimedia applications reveal gaps.

## Expected outcome

Following this order keeps the existing host-forwarding design, improves compatibility where the current bridge is already close to working, and avoids taking on a full protected-mode CPU project before the runtime layer can benefit from it.
