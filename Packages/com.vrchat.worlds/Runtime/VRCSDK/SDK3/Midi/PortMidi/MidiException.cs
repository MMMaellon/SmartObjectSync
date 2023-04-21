using System;

namespace PortMidi
{
    public class MidiException : Exception
    {
        MidiErrorType error_type;

        public MidiException(MidiErrorType errorType, string message)
            : this(errorType, message, null)
        {
        }

        public MidiException(MidiErrorType errorType, string message, Exception innerException)
            : base(message, innerException)
        {
            error_type = errorType;
        }

        public MidiErrorType ErrorType => error_type;
    }
}