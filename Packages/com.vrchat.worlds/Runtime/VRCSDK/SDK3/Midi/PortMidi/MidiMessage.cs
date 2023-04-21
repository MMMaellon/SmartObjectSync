using System;

namespace PortMidi
{
    public struct MidiMessage
    {
        private int v;
        public MidiMessage(int value)
        {
            v = value;
        }

        public MidiMessage(int status, int data1, int data2)
        {
            v = ((data2 << 16) & 0xFF0000) | ((data1 << 8) & 0xFF00) | (status & 0xFF);
        }

        public Int32 Value => v;
    }
}