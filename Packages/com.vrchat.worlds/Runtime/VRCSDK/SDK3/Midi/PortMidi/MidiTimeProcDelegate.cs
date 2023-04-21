using System;
using PmTimestamp = System.Int32;

namespace PortMidi
{
    public delegate PmTimestamp MidiTimeProcDelegate(IntPtr timeInfo);
}