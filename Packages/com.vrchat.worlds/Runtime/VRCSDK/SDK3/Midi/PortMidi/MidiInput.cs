using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace PortMidi
{
    public class MidiInput : MidiStream
    {
        public MidiInput(IntPtr stream, Int32 inputDevice)
            : base(stream, inputDevice)
        {
        }

        public bool HasData => PortMidiMarshal.Pm_Poll(stream) == MidiErrorType.GotData;

        public int Read(byte[] buffer, int index, int length)
        {
            var gch = GCHandle.Alloc(buffer);
            try
            {
                var ptr = Marshal.UnsafeAddrOfPinnedArrayElement(buffer, index);
                int size = PortMidiMarshal.Pm_Read(stream, ptr, length);
                if (size < 0)
                {
                    throw new MidiException((MidiErrorType) size,
                        PortMidiMarshal.Pm_GetErrorText((MidiErrorType) size));
                }
                return size * 4;
            }
            finally
            {
                gch.Free();
            }
        }

        public Event ReadEvent(byte[] buffer, int index, int length)
        {
            var gch = GCHandle.Alloc(buffer);
            try
            {
                var ptr = Marshal.UnsafeAddrOfPinnedArrayElement(buffer, index);
                int size = PortMidiMarshal.Pm_Read(stream, ptr, length);
                if (size < 0)
                {
                    throw new MidiException((MidiErrorType) size,
                        PortMidiMarshal.Pm_GetErrorText((MidiErrorType) size));
                }

                return new Event(Marshal.PtrToStructure<PmEvent>(ptr));
            }
            finally
            {
                gch.Free();
            }
        }
    }
}