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
   - `mciSendCommand` now supports MCI_OPEN, MCI_CLOSE, MCI_PLAY, MCI_STATUS, MCI_STOP, MCI_PAUSE, and MCI_SEEK.
   - `mciGetErrorString` is fully implemented.
   - Other unsupported MCI commands still throw `NotImplementedException`.

2. **DOS and multiplex interrupts** — _improved_
   - `Int 1Ah` services 0–5 (timer count get/set, RTC time get/set, RTC date get/set) are implemented.
   - Unsupported `Int 1Ah` services, unsupported DOS interrupt functions (`Int 21h`), and unsupported multiplex interrupt services (`Int 2Fh`) now log a warning and return gracefully (setting carry flag / error codes) instead of throwing `NotImplementedException`.
   - **Remaining:** specific services used by installers can be added as compatibility testing reveals them.

3. **Cross-task window enumeration** — _virtualized_
   - `EnumTaskWindows` now succeeds without enumeration for non-current-task requests, rather than throwing.
   - **Remaining:** full cross-task enumeration is not yet emulated; the current approach virtualizes the result for compatibility.

## Recommended execution order

1. ~~Add focused unit tests around the currently known `NotImplementedException` and invalid-opcode paths.~~ ✅
2. ~~Fix runtime blockers first: hook bridging, WNDPROC return values, `lpCreateParams`, and thunking support.~~ ✅
3. ~~Implement the missing `TEST` instruction variants.~~ ✅
4. ~~Introduce staged `0x0F` decoding and document which extended/protected-mode instructions are intentionally still unsupported.~~ ✅
5. Expand subsystem coverage for MCI, DOS interrupts, and cross-task window APIs based on app compatibility testing. — _in progress_

## Next steps

The following items represent the current frontier for further work:

1. **Continue broadening thunking support** — add marshaling for parameter/return shapes encountered when testing real Win3.x applications against additional forwarded DLL exports.
2. **Extend MCI command coverage** — implement less common MCI commands as multimedia applications reveal gaps.
3. **Add app-driven DOS service coverage** — implement additional `Int 21h` and `Int 2Fh` subfunction handlers as installer and launcher testing identifies required services.
4. **Evaluate cross-task window enumeration** — determine from application testing whether full enumeration is needed or the virtualized approach is sufficient.

## Expected outcome

Following this order keeps the existing host-forwarding design, improves compatibility where the current bridge is already close to working, and avoids taking on a full protected-mode CPU project before the runtime layer can benefit from it.
