using System;

namespace Win3muCore.Validation
{
    public class LoaderValidationFixup
    {
        public LoaderValidationFixup(string kind, int count)
        {
            Kind = kind;
            Count = count;
        }

        public string Kind
        {
            get;
            private set;
        }

        public int Count
        {
            get;
            private set;
        }
    }
}
