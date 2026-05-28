using System.Collections.Generic;

namespace Win3muCore.Validation
{
    public class LoaderValidationResult
    {
        public string FilePath
        {
            get;
            set;
        }

        public string ModuleName
        {
            get;
            set;
        }

        public bool IsDll
        {
            get;
            set;
        }

        public int SegmentCount
        {
            get;
            set;
        }

        public int FixupCount
        {
            get;
            set;
        }

        public List<LoaderValidationFixup> Fixups
        {
            get;
            set;
        } = new List<LoaderValidationFixup>();

        public List<string> ReferencedModules
        {
            get;
            set;
        } = new List<string>();

        public List<LoaderValidationSymbol> Symbols
        {
            get;
            set;
        } = new List<LoaderValidationSymbol>();

        public LoaderValidationExecutionResult Execution
        {
            get;
            set;
        }

        public bool Success
        {
            get;
            set;
        }

        public string Error
        {
            get;
            set;
        }
    }
}
