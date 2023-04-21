using System;

namespace PortMidi
{
    [Flags]
    public enum MidiFilter : int
    {
        Active = 1 << 0x0E,
        SysEx = 1,
        Clock = 1 << 0x08,
        Play = ((1 << 0x0A) | (1 << 0x0C) | (1 << 0x0B)),
        Tick = (1 << 0x09),
        FD = (1 << 0x0D),
        Undefined = FD,
        Reset = (1 << 0x0F),
        RealTime = (Active | SysEx | Clock | Play | Undefined | Reset | Tick),
        Note = ((1 << 0x19) | (1 << 0x18)),
        CAF = (1 << 0x1D),
        PAF = (1 << 0x1A),
        AF = (CAF | PAF),
        Program = (1 << 0x1C),
        Control = (1 << 0x1B),
        PitchBend = (1 << 0x1E),
        MTC = (1 << 0x01),
        SongPosition = (1 << 0x02),
        SongSelect = (1 << 0x03),
        Tune = (1 << 0x06),
        SystemCommon = (MTC | SongPosition | SongSelect | Tune)
    }
}