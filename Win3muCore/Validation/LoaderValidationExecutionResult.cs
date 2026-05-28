namespace Win3muCore.Validation
{
    public class LoaderValidationExecutionResult
    {
        public bool Attempted
        {
            get;
            set;
        }

        public int InstructionsExecuted
        {
            get;
            set;
        }

        public bool ReachedInstructionLimit
        {
            get;
            set;
        }

        public bool Aborted
        {
            get;
            set;
        }

        public ushort CodeSegment
        {
            get;
            set;
        }

        public ushort InstructionPointer
        {
            get;
            set;
        }

        public string StopReason
        {
            get;
            set;
        }
    }
}
