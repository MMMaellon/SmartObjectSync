using System;
using System.Runtime.InteropServices;

namespace PortMidi
{
    [StructLayout(LayoutKind.Sequential)]
    public struct MidiEvent
    {
        MidiMessage msg;
        Int32 ts;
        [NonSerialized] byte[] sysex;

        public MidiMessage Message
        {
            get => msg;
            set => msg = value;
        }

        public Int32 Timestamp
        {
            get => ts;
            set => ts = value;
        }

        public byte[] SysEx
        {
            get => sysex;
            set => sysex = value;
        }
    }
}