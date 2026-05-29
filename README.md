About
=====

Win3mu fork from Topten Software

Compilation
===========

Build the solution with:

```bash
dotnet build Win3mu.sln
```

Run the core unit tests with:

```bash
dotnet test Win3muCoreUnitTests/Win3muCoreUnitTests.csproj
```

Development Roadmap
===================

See [DEVELOPMENT_ROADMAP.md](DEVELOPMENT_ROADMAP.md) for a prioritized plan covering Sharp86 instruction/protected-mode gaps and runtime compatibility work.

Usage
=====

Right-click on your .16-bit exe file, then choose "Convert with Win3mu". If everything is good, your original file will be renamed and the new exe will appear with the original icon.

You can then run the new executable with a mandatory C-drive root, for example `win3mu C:\APPS\MYAPP\MYAPP.EXE /root:C:\temp\win3mu-c`. The executable and its companion files should live inside that emulated C: drive tree. If the supplied root folder does not already exist, Win3mu initializes it with a minimal DOS/Windows layout including `C:\WINDOWS`, `C:\WINDOWS\SYSTEM`, `C:\DOS`, `C:\TEMP`, `C:\AUTOEXEC.BAT`, `C:\CONFIG.SYS`, `C:\WINDOWS\WIN.INI`, and `C:\WINDOWS\SYSTEM.INI`.

If it complains about some modules (SHELL, COMMDLG, OLECLI...), copy the corresponding DLL file from the original WINDOWS\SYSTEM to the current folder, and try again.

Finally, if you get some error like "Unsupported ordinal #**** in module **** invoked", then sorry, this particular function hasn't been implemented yet.

Relative file access follows the real host current directory from which the NE executable is launched, even if the executable itself is mapped to a different guest module path.
If that launch directory is outside the configured guest mounts, Win3mu exposes it through a temporary guest drive for working-directory based file I/O.
Module loading still uses the executable's mapped guest path, and a warning is logged when the two directories differ.

Validation CLI
==============

The repository now includes a cross-platform loader validation CLI in `Win3muTestCli`.

Build just the validator with:

```bash
dotnet build Win3muTestCli/Win3muTestCli.csproj
```

Run it against a single file or a directory tree:

```bash
dotnet run --project Win3muTestCli/Win3muTestCli.csproj -- <file-or-directory>
```

The tool recursively scans directories for `.exe` and `.dll` files, attempts to load and link each candidate with the Win3mu loader, prints per-file fixup details, emits a simple symbol map, and for NE executables runs the startup code under Sharp86 until it hits a blocker or an instruction budget.

Current limitations:

- The CLI only drives the executable start code far enough to expose missing dependencies and early control flow.
- GUI calls and other host Windows services still depend on platform DLLs such as `user32.dll`/`gdi32.dll`, so execution will usually stop once the sample reaches those boundaries.

Original links and source code
==============================

- About the project: https://www.toptensoftware.com/win3mu/
- Technical details: https://hackernoon.com/win3mu-part-1-why-im-writing-a-16-bit-windows-emulator-2eae946c935d
- Win3mu: https://bitbucket.org/toptensoftware/win3mu
- Sharp86: https://bitbucket.org/toptensoftware/sharp86
- ConFrames: https://bitbucket.org/toptensoftware/conframes
- PetaJson: https://github.com/toptensoftware/PetaJson
