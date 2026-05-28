using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Win3muCore.NeFile;

namespace Win3muCore.Validation
{
    public class LoaderValidator
    {
        const string ValidationGuestFolder = @"C:\INPUT";
        const int ExecutionInstructionLimit = 50000;
        const int ExecutionSliceSize = 256;

        public LoaderValidationReport Validate(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            var fullPath = Path.GetFullPath(path);
            var candidates = EnumerateCandidates(fullPath).OrderBy(x => x, StringComparer.InvariantCultureIgnoreCase).ToList();
            var results = new List<LoaderValidationResult>();

            foreach (var candidate in candidates)
            {
                results.Add(ValidateFile(candidate));
            }

            return new LoaderValidationReport(fullPath, results, candidates.Count);
        }

        IEnumerable<string> EnumerateCandidates(string path)
        {
            if (File.Exists(path))
            {
                yield return path;
                yield break;
            }

            if (!Directory.Exists(path))
                throw new FileNotFoundException(string.Format("The path '{0}' does not exist.", path), path);

            foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            {
                var extension = Path.GetExtension(file);
                if (extension.Equals(".exe", StringComparison.InvariantCultureIgnoreCase) ||
                    extension.Equals(".dll", StringComparison.InvariantCultureIgnoreCase))
                {
                    yield return file;
                }
            }
        }

        LoaderValidationResult ValidateFile(string filePath)
        {
            var result = new LoaderValidationResult();
            result.FilePath = filePath;

            Machine machine = null;
            Module16 module = null;

            try
            {
                machine = CreateMachine(filePath);
                module = CreateModule(filePath);
                PopulateMetadata(module.NeFile, result);
                machine.ModuleManager.LoadModuleForValidation(module);
                PopulateSymbolMap(module, result);

                if (!module.IsDll)
                    result.Execution = ExecuteStartCode(filePath, result);

                result.Success = true;
            }
            catch (Exception x)
            {
                result.Success = false;
                result.Error = UnwrapException(x).Message;
            }
            finally
            {
                if (module != null && module.LoadCount > 0)
                {
                    try
                    {
                        machine.ModuleManager.UnloadModule(module);
                    }
                    catch
                    {
                        // Ignore cleanup failures so the original validation result is preserved.
                    }
                }
            }

            return result;
        }

        static Machine CreateMachine(string filePath)
        {
            var machine = new Machine();
            machine.logModules = false;
            machine.logRelocations = false;
            machine.logWarnings = false;

            var containingDirectory = Path.GetDirectoryName(filePath);
            machine.PathMapper.AddMount(ValidationGuestFolder, containingDirectory, containingDirectory);
            return machine;
        }

        static Module16 CreateModule(string filePath)
        {
            var module = new Module16(filePath);
            module.SetGuestFileName(DosPath.Join(ValidationGuestFolder, BuildGuestFileName(filePath)));
            return module;
        }

        static string BuildGuestFileName(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToUpperInvariant();
            if (string.IsNullOrEmpty(extension))
                extension = ".BIN";

            if (extension.Length > 4)
                extension = extension.Substring(0, 4);

            return "TARGET" + extension;
        }

        static void PopulateMetadata(NeFileReader neFile, LoaderValidationResult result)
        {
            result.ModuleName = neFile.ModuleName;
            result.IsDll = neFile.IsDll;
            result.SegmentCount = neFile.Segments.Count;
            result.ReferencedModules = neFile.ModuleReferenceTable.ToList();

            var fixupCounts = neFile.Segments
                .SelectMany(x => x.relocations)
                .GroupBy(x => string.Format("{0} ({1})", x.TypeString, x.addressType))
                .OrderBy(x => x.Key, StringComparer.InvariantCultureIgnoreCase)
                .Select(x => new LoaderValidationFixup(x.Key, x.Count()))
                .ToList();

            result.Fixups = fixupCounts;
            result.FixupCount = fixupCounts.Sum(x => x.Count);
        }

        // Build a lightweight symbol map from the loaded module so CLI output can be
        // correlated with Sharp86 CS:IP addresses during follow-up debugging.
        static void PopulateSymbolMap(Module16 module, LoaderValidationResult result)
        {
            result.Symbols.Clear();

            var entryPoint = module.NeFile.Header.EntryPoint;
            if (entryPoint != 0)
            {
                var entrySegmentIndex = (int)(entryPoint >> 16) - 1;
                if (entrySegmentIndex >= 0 && entrySegmentIndex < module.NeFile.Segments.Count)
                {
                    result.Symbols.Add(new LoaderValidationSymbol(
                        "start",
                        module.NeFile.Segments[entrySegmentIndex].globalHandle,
                        (ushort)(entryPoint & 0xFFFF),
                        "module entry point"));
                }
            }

            for (int i = 0; i < module.NeFile.Segments.Count; i++)
            {
                var segment = module.NeFile.Segments[i];
                result.Symbols.Add(new LoaderValidationSymbol(
                    string.Format("seg{0}", i + 1),
                    segment.globalHandle,
                    0,
                    string.Format("segment {0}", i + 1)));
            }

            foreach (var ordinal in module.GetExports().OrderBy(x => x))
            {
                var address = module.GetProcAddress(ordinal);
                if (address == 0 || address.Hiword() == 0xFFFF)
                    continue;

                var exportName = module.GetNameFromOrdinal(ordinal);

                result.Symbols.Add(new LoaderValidationSymbol(
                    string.IsNullOrEmpty(exportName) ? string.Format("ord_{0:X4}", ordinal) : exportName,
                    address.Hiword(),
                    address.Loword(),
                    string.Format("ordinal {0:X4}", ordinal)));
            }
        }

        // Start-code execution is intentionally bounded: the goal is to get through the
        // NE entry sequence until the first Windows-host dependency or similar blocker.
        LoaderValidationExecutionResult ExecuteStartCode(string filePath, LoaderValidationResult result)
        {
            var execution = new LoaderValidationExecutionResult();
            execution.Attempted = true;

            Machine machine = null;
            Module16 module = null;
            ulong initialCpuTime = 0;

            try
            {
                machine = CreateMachine(filePath);
                module = CreateModule(filePath);
                machine.ModuleManager.LoadModule(module);
                // Refresh the symbol map with addresses from the execution-ready machine so
                // the reported CS:IP values line up with the Sharp86 run attempt below.
                PopulateSymbolMap(module, result);

                module.PrepareRun(machine, null, 1);

                initialCpuTime = machine.CpuTime;
                var remainingInstructions = ExecutionInstructionLimit;
                while (remainingInstructions > 0)
                {
                    var slice = Math.Min(remainingInstructions, ExecutionSliceSize);
                    var aborted = machine.Run(slice);
                    remainingInstructions = Math.Max(0, ExecutionInstructionLimit - (int)(machine.CpuTime - initialCpuTime));

                    if (aborted || machine.Halted)
                    {
                        execution.Aborted = true;
                        break;
                    }
                }

                execution.InstructionsExecuted = (int)(machine.CpuTime - initialCpuTime);
                execution.ReachedInstructionLimit = !execution.Aborted && execution.InstructionsExecuted >= ExecutionInstructionLimit;
                if (execution.Aborted)
                    execution.StopReason = "Execution stopped by the emulated program before the instruction budget was exhausted.";
            }
            catch (Exception x)
            {
                execution.StopReason = FormatExecutionStopReason(UnwrapException(x).Message);
                if (machine != null)
                    execution.InstructionsExecuted = (int)(machine.CpuTime - initialCpuTime);
            }
            finally
            {
                if (machine != null)
                {
                    execution.CodeSegment = machine.cs;
                    execution.InstructionPointer = machine.ip;
                }

                if (module != null && module.LoadCount > 0)
                {
                    try
                    {
                        machine.ModuleManager.UnloadModule(module);
                    }
                    catch
                    {
                        // Ignore cleanup failures so the original execution result is preserved.
                    }
                }
            }

            return execution;
        }

        static string FormatExecutionStopReason(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return message;

            using (var reader = new StringReader(message))
            {
                var firstLine = reader.ReadLine();
                return string.IsNullOrWhiteSpace(firstLine) ? message.Trim() : firstLine.Trim();
            }
        }

        static Exception UnwrapException(Exception x)
        {
            while (x is System.Reflection.TargetInvocationException && x.InnerException != null)
            {
                x = x.InnerException;
            }

            return x;
        }
    }
}
