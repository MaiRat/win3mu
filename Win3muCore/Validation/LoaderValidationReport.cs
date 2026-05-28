using System.Collections.Generic;
using System.Linq;

namespace Win3muCore.Validation
{
    public class LoaderValidationReport
    {
        public LoaderValidationReport(string requestedPath, List<LoaderValidationResult> results, int filesDiscovered)
        {
            RequestedPath = requestedPath;
            Results = results;
            FilesDiscovered = filesDiscovered;
        }

        public string RequestedPath
        {
            get;
            private set;
        }

        public int FilesDiscovered
        {
            get;
            private set;
        }

        public List<LoaderValidationResult> Results
        {
            get;
            private set;
        }

        public int FilesProcessed
        {
            get { return Results.Count; }
        }

        public int SuccessCount
        {
            get { return Results.Count(x => x.Success); }
        }

        public int FailureCount
        {
            get { return Results.Count(x => !x.Success); }
        }
    }
}
