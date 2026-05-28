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
                machine = new Machine();
                machine.logModules = false;
                machine.logRelocations = false;
                machine.logWarnings = false;

                var containingDirectory = Path.GetDirectoryName(filePath);
                machine.PathMapper.AddMount(ValidationGuestFolder, containingDirectory, containingDirectory);

                module = new Module16(filePath);
                module.SetGuestFileName(DosPath.Join(ValidationGuestFolder, BuildGuestFileName(filePath)));

                PopulateMetadata(module.NeFile, result);
                machine.ModuleManager.LoadModuleForValidation(module);
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
                    }
                }
            }

            return result;
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
