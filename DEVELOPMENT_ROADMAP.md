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

2. **DOS and multiplex interrupts** — _improved_
   - `Int 1Ah` services 0–5 (timer count get/set, RTC time get/set, RTC date get/set) are implemented.
   - `Int 21h` now supports additional services: 0x33 (Get/Set System Values including Ctrl-C check flag and boot drive), 0x36 (Get Disk Free Space), 0x48-0x4A (memory allocation stubs), and 0x57 (Get/Set File Date and Time).
   - `Int 21h/44h` IOCTL now supports subfunctions 00h (Get Device Info), 01h (Set Device Info), 06h (Get Input Status), 07h (Get Output Status), and 08h (Check Removable Media). Unsupported IOCTL subfunctions set carry and log the specific subfunction number.
   - Unsupported `Int 1Ah` services, unsupported DOS interrupt functions (`Int 21h`), and unsupported multiplex interrupt services (`Int 2Fh`) now log a warning and return gracefully (setting carry flag / error codes) instead of throwing `NotImplementedException`.
   - **Remaining:** specific services used by installers can be added as compatibility testing reveals them.

3. **Cross-task window enumeration** — _virtualized_
   - `EnumTaskWindows` now succeeds without enumeration for non-current-task requests, rather than throwing.
   - **Remaining:** full cross-task enumeration is not yet emulated; the current approach virtualizes the result for compatibility.

4. **Control message semantics** — _substantially expanded_
   - Button: `BM_GETCHECK`, `BM_SETCHECK`, `BM_GETSTATE`, `BM_SETSTATE`, `BM_SETSTYLE` are now fully mapped with correct parameter semantics.
   - Static: `STM_SETICON` and `STM_GETICON` now use proper GDI object handle mapping.
   - Edit: 18 messages implemented including `EM_GETSEL`, `EM_SETSEL` (with Win16 packed-lParam cracking), `EM_LINESCROLL` (with parameter cracking), `EM_REPLACESEL` (with string conversion), `EM_GETMODIFY`, `EM_SETMODIFY`, `EM_GETLINECOUNT`, `EM_LINEINDEX`, `EM_LINELENGTH`, `EM_CANUNDO`, `EM_UNDO`, `EM_FMTLINES`, `EM_LINEFROMCHAR`, `EM_SETPASSWORDCHAR`, `EM_EMPTYUNDOBUFFER`, `EM_GETFIRSTVISIBLELINE`, `EM_SETREADONLY`, `EM_GETPASSWORDCHAR`, and `EM_SETLIMITTEXT`.
   - ListBox: 21 messages implemented including `LB_DELETESTRING`, `LB_SETSEL`, `LB_GETSEL`, `LB_GETTEXTLEN`, `LB_GETCOUNT`, `LB_SELECTSTRING`, `LB_GETSELCOUNT`, `LB_GETHORIZONTALEXTENT`, `LB_SETHORIZONTALEXTENT`, `LB_SETCOLUMNWIDTH`, `LB_GETITEMDATA`, `LB_SETITEMDATA`, `LB_SELITEMRANGE`, `LB_SETCARETINDEX`, `LB_SETITEMHEIGHT`, `LB_GETITEMHEIGHT`, `LB_FINDSTRINGEXACT`, plus existing `LB_ADDSTRING`, `LB_INSERTSTRING`, `LB_GETTEXT`, `LB_FINDSTRING`, etc.
   - ComboBox: 19 messages implemented including `CB_GETEDITSEL`, `CB_LIMITTEXT`, `CB_SETEDITSEL`, `CB_DELETESTRING`, `CB_DIR`, `CB_GETLBTEXTLEN`, `CB_SELECTSTRING`, `CB_SHOWDROPDOWN`, `CB_SETITEMHEIGHT`, `CB_GETITEMHEIGHT`, `CB_SETEXTENDEDUI`, `CB_GETEXTENDEDUI`, `CB_GETDROPPEDSTATE`, `CB_FINDSTRINGEXACT`, plus existing `CB_ADDSTRING`, `CB_INSERTSTRING`, `CB_GETLBTEXT`, `CB_FINDSTRING`, etc.
   - The old commented-out `notimpl()` block has been replaced entirely with proper semantics.
   - **Remaining:** complex pointer-based messages (`EM_GETRECT`/`EM_SETRECT`, `EM_GETLINE`, `EM_SETTABSTOPS`, `LB_GETITEMRECT`, `LB_GETSELITEMS`, `LB_SETTABSTOPS`, `CB_GETDROPPEDCONTROLRECT`, `EM_SETHANDLE`/`EM_GETHANDLE`, `EM_SETWORDBREAKPROC`/`EM_GETWORDBREAKPROC`) require custom Callable implementations and can be added as applications exercise them.

5. **GDI robustness** — _improved_
   - `CreateBrushIndirect` now handles BS_NULL/BS_HOLLOW brush style and falls back gracefully for unknown brush styles instead of throwing.
   - `GetObject` now logs and returns 0 for unsupported GDI object types instead of throwing.

5. **Kernel process termination** — _implemented_
   - `FatalExit` and `FatalAppExit` now log, display a message box (for FatalAppExit), and cleanly terminate the emulated process instead of throwing `NotImplementedException`.

6. **Module loading robustness** — _improved_
   - NE relocation processing now supports the `LowByte` (type 0) relocation address type for `InternalReference`, `ImportedOrdinal`, and `ImportedName` relocations, allowing modules with byte-level fixups to load correctly.
   - Unknown FP OSFixup tribyte combinations now log a warning and emit a NOP instead of crashing module load.
   - **Remaining:** `Pointer48` and `Offset32` relocation address types are not yet implemented (rare in Win3.x modules).

## Recommended execution order

1. ~~Add focused unit tests around the currently known `NotImplementedException` and invalid-opcode paths.~~ ✅
2. ~~Fix runtime blockers first: hook bridging, WNDPROC return values, `lpCreateParams`, and thunking support.~~ ✅
3. ~~Implement the missing `TEST` instruction variants.~~ ✅
4. ~~Introduce staged `0x0F` decoding and document which extended/protected-mode instructions are intentionally still unsupported.~~ ✅
5. ~~Expand subsystem coverage for MCI, DOS interrupts, and cross-task window APIs based on app compatibility testing.~~ ✅
6. ~~Implement control message semantics for standard Windows controls (Button, Static, Edit, ListBox, ComboBox).~~ ✅

## Next steps

The following items represent the current frontier for further work:

1. **Implement complex control message Callables** — add custom `Callable` implementations for pointer-based control messages: `EM_GETRECT`/`EM_SETRECT`/`EM_SETRECTNP` (RECT pointer), `EM_GETLINE` (buffer with word-length prefix), `EM_SETTABSTOPS`/`LB_SETTABSTOPS` (array pointer), `LB_GETITEMRECT` (RECT pointer), `LB_GETSELITEMS` (buffer), `CB_GETDROPPEDCONTROLRECT` (RECT pointer).
2. **Continue broadening thunking support** — add marshaling for parameter/return shapes encountered when testing real Win3.x applications against additional forwarded DLL exports.
3. **Add app-driven DOS service coverage** — implement additional `Int 21h` subfunction handlers (e.g. remaining IOCTL subfunctions at 0x44, memory services) as installer and launcher testing identifies required services.
4. **Evaluate cross-task window enumeration** — determine from application testing whether full enumeration is needed or the virtualized approach is sufficient.
5. **Extend MCI device-specific support** — implement device-specific MCI command extensions as multimedia applications reveal gaps.
6. **Add remaining relocation types** — implement `Pointer48` and `Offset32` relocation address types if encountered by real NE executables.

## Expected outcome

Following this order keeps the existing host-forwarding design, improves compatibility where the current bridge is already close to working, and avoids taking on a full protected-mode CPU project before the runtime layer can benefit from it.
