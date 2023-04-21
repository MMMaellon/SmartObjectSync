namespace PortMidi
{
    public enum MidiErrorType
    {
        NoError = 0,
        NoData = 0,
        GotData = 1,
        HostError = -10000,
        InvalidDeviceId,
        InsufficientMemory,
        BufferTooSmall,
        BufferOverflow,
        BadPointer,
        BadData,
        InternalError,
        BufferMaxSize,
    }
}