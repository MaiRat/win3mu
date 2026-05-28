namespace Win3muCore.Validation
{
    public class LoaderValidationSymbol
    {
        public LoaderValidationSymbol(string name, ushort segment, ushort offset, string source)
        {
            Name = name;
            Segment = segment;
            Offset = offset;
            Source = source;
        }

        public string Name
        {
            get;
            private set;
        }

        public ushort Segment
        {
            get;
            private set;
        }

        public ushort Offset
        {
            get;
            private set;
        }

        public string Source
        {
            get;
            private set;
        }
    }
}
