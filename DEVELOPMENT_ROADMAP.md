# Win3mu Development Roadmap

This roadmap summarizes the highest-value improvements identified in the current codebase for the Win3.x emulator.

## Current state

- **Sharp86 is intentionally limited to 8086-era execution plus host-provided selector support.** The Sharp86 readme explicitly describes the current model as a pseudo-protected mode and notes that protected-mode instructions are not available (`/home/runner/work/win3mu/win3mu/Sharp86/readme.md`).
- **The runtime's export-forwarding model is broadly the right approach to keep.** Most compatibility gaps are in the thunking, message conversion, and edge-case handling around the 16-bit to host bridge (`/home/runner/work/win3mu/win3mu/Win3muCore/Core/Module32.cs`).

## Priority 1: close compatibility blockers in the runtime layer

These items are likely to unblock more real applications faster than a large CPU rewrite.

1. **Finish hook bridging and default hook processing**
   - `OldHookProcProxy` still throws `NotImplementedException("Hook Proxy")` for unsupported paths (`/home/runner/work/win3mu/win3mu/Win3muCore/EmulatedModules/User.cs`).
   - `DefHookProc` currently only handles `WH_MSGFILTER` and throws for other hook types.
   - **Plan:** add safe fallbacks for unsupported hook message conversions, then extend support for common hook types such as keyboard, mouse, and call-window-proc hooks.

2. **Return the real window-proc result from message dispatch**
   - `SendMessage` currently returns `0` after dispatch with a TODO noting that the WNDPROC return value should be propagated (`/home/runner/work/win3mu/win3mu/Win3muCore/EmulatedModules/User.cs`).
   - **Plan:** preserve and marshal the dispatched result back to 16-bit callers before expanding message semantics further.

3. **Support `lpParam` / `CREATESTRUCT.lpCreateParams` during window creation**
   - `CreateWindowEx` and `WM_NCCREATE` still throw when `lpCreateParams` is present for non-registered classes.
   - **Plan:** define a supported conversion path for app-supplied creation data so installers and custom controls can initialize correctly.

4. **Broaden the thunking layer before adding more forwarded exports**
   - `Module32` still throws for unsupported parameter types, unsupported return types, and non-null `IntPtr` parameters.
   - **Plan:** add targeted marshaling support for the parameter/return shapes seen in real Win3.x DLL entry points, and keep the current forwarding architecture intact.

## Priority 2: expand Sharp86 instruction coverage for Win3.x workloads

1. **Implement the missing `TEST` group variants**
   - Opcode groups `F6 /1` and `F7 /1` still throw `NotImplementedException` in `CPU.cs`.
   - **Plan:** implement these first because they are discrete, testable, and low-risk compared with a larger decoder change.

2. **Add a staged plan for `0x0F` extended opcode decoding**
   - The `0x0F` opcode prefix currently throws `InvalidOpCodeException` immediately (`/home/runner/work/win3mu/win3mu/Sharp86/Sharp86/CPU.cs`).
   - **Plan:** introduce two-byte opcode decoding in stages, starting with instructions that matter to 16-bit Windows applications and protected-mode-aware runtimes, rather than attempting full 386 coverage at once.

3. **Define a protected-mode support boundary**
   - Sharp86 documents that it can emulate selector-aware software but not software that expects real protected-mode instructions.
   - **Plan:** decide whether the goal is:
     - **A compatibility-first path:** only implement the subset of protected-mode behavior needed by Win3.x applications and Win32s-style support code, or
     - **A broader CPU path:** add descriptor-table, selector-validation, and privilege-related instructions in a deliberate 286/386 compatibility project.

## Priority 3: fill subsystem gaps that affect specific app classes

1. **Multimedia**
   - `mciSendCommand` only supports a small subset of commands, and `mciGetErrorString` is not implemented (`/home/runner/work/win3mu/win3mu/Win3muCore/EmulatedModules/MMSystem.cs`).
   - **Plan:** add `STOP`, `PAUSE`, `SEEK`, and error-string support before expanding to less common commands.

2. **DOS and multiplex interrupts**
   - `Int 1Ah` service coverage is partial, and unsupported DOS / multiplex interrupt cases still throw in `DosApi.cs`.
   - **Plan:** prioritize services used by installers, setup programs, and launchers before attempting broader DOS completeness.

3. **Cross-task window enumeration**
   - `EnumTaskWindows` rejects enumeration for tasks other than the current process module.
   - **Plan:** decide whether cross-task enumeration should be emulated fully or explicitly virtualized for compatibility.

## Recommended execution order

1. Add focused unit tests around the currently known `NotImplementedException` and invalid-opcode paths.
2. Fix runtime blockers first: hook bridging, WNDPROC return values, `lpCreateParams`, and thunking support.
3. Implement the missing `TEST` instruction variants.
4. Introduce staged `0x0F` decoding and document which extended/protected-mode instructions are intentionally still unsupported.
5. Expand subsystem coverage for MCI, DOS interrupts, and cross-task window APIs based on app compatibility testing.

## Expected outcome

Following this order keeps the existing host-forwarding design, improves compatibility where the current bridge is already close to working, and avoids taking on a full protected-mode CPU project before the runtime layer can benefit from it.
