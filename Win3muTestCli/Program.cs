using System;
using System.Linq;
using Win3muCore.Validation;

namespace Win3muTestCli
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.Error.WriteLine("Usage: Win3muTestCli <file-or-directory>");
                return 1;
            }

            try
            {
                var validator = new LoaderValidator();
                var report = validator.Validate(args[0]);

                foreach (var result in report.Results)
                {
                    var status = result.Success ? "OK" : "FAIL";
                    Console.WriteLine("[{0}] {1}", status, result.FilePath);
                    Console.WriteLine("  Module: {0}", string.IsNullOrEmpty(result.ModuleName) ? "<unknown>" : result.ModuleName);
                    Console.WriteLine("  Type: {0}", result.IsDll ? "DLL" : "EXE");
                    Console.WriteLine("  Segments: {0}", result.SegmentCount);
                    Console.WriteLine("  Fixups: {0}", result.FixupCount == 0 ? "none" : string.Join(", ", result.Fixups.Select(x => string.Format("{0}={1}", x.Kind, x.Count))));

                    if (result.ReferencedModules.Count > 0)
                        Console.WriteLine("  References: {0}", string.Join(", ", result.ReferencedModules));

                    if (!result.Success)
                        Console.WriteLine("  Error: {0}", result.Error);

                    if (result.Success && result.Execution != null)
                    {
                        var execution = result.Execution;
                        var executionStatus = execution.ReachedInstructionLimit
                            ? string.Format("instruction limit reached after {0} instructions", execution.InstructionsExecuted)
                            : string.Format("stopped after {0} instructions", execution.InstructionsExecuted);

                        Console.WriteLine("  Startcode: {0} at {1:X4}:{2:X4}", executionStatus, execution.CodeSegment, execution.InstructionPointer);
                        if (!string.IsNullOrEmpty(execution.StopReason))
                            Console.WriteLine("    Reason: {0}", execution.StopReason);
                    }

                    if (result.Success)
                    {
                        Console.WriteLine("  Symbol map:");
                        foreach (var symbol in result.Symbols)
                        {
                            Console.WriteLine("    {0:X4}:{1:X4} {2} ({3})", symbol.Segment, symbol.Offset, symbol.Name, symbol.Source);
                        }
                    }
                }

                Console.WriteLine();
                Console.WriteLine("Summary:");
                Console.WriteLine("  Discovered: {0}", report.FilesDiscovered);
                Console.WriteLine("  Processed: {0}", report.FilesProcessed);
                Console.WriteLine("  Succeeded: {0}", report.SuccessCount);
                Console.WriteLine("  Failed: {0}", report.FailureCount);
                Console.WriteLine("  Limitations: startcode execution is bounded and stops at the first host dependency, missing module, GUI call, or instruction budget.");

                return report.FailureCount == 0 ? 0 : 2;
            }
            catch (Exception x)
            {
                Console.Error.WriteLine(x.Message);
                return 1;
            }
        }
    }
}
