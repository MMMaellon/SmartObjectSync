using System;
using System.Runtime.InteropServices;

namespace PortMidi
{
    public class MidiOutput : MidiStream
    {
        public MidiOutput(IntPtr stream, Int32 outputDevice, int latency)
            : base(stream, outputDevice)
        {
        }

        public void Write(MidiEvent midiEvent)
        {
            if (midiEvent.SysEx != null)
            {
                WriteSysEx(midiEvent.Timestamp, midiEvent.SysEx);
            }
            else
            {
                Write(midiEvent.Timestamp, midiEvent.Message);
            }
        }

        private void Write(Int32 when, MidiMessage msg)
        {
            var ret = PortMidiMarshal.Pm_WriteShort(stream, when, msg);
            if (ret != MidiErrorType.NoError)
            {
                throw new MidiException(ret,
                    $"Failed to write message {msg.Value} : {PortMidiMarshal.Pm_GetErrorText((MidiErrorType) ret)}");
            }
        }

        private void WriteSysEx(Int32 when, byte[] sysEx)
        {
            var ret = PortMidiMarshal.Pm_WriteSysEx(stream, when, sysEx);
            if (ret != MidiErrorType.NoError)
                throw new MidiException(ret,
                    $"Failed to write sysEx message : {PortMidiMarshal.Pm_GetErrorText((MidiErrorType) ret)}");
        }

        public void Write(MidiEvent[] buffer)
        {
            Write(buffer, 0, buffer.Length);
        }

        private void Write(MidiEvent[] buffer, int index, int length)
        {
            var gch = GCHandle.Alloc(buffer);
            try
            {
                var ptr = Marshal.UnsafeAddrOfPinnedArrayElement(buffer, index);
                var ret = PortMidiMarshal.Pm_Write(stream, ptr, length);
                if (ret != MidiErrorType.NoError)
                {
                    throw new MidiException(ret,
                        $"Failed to write messages : {PortMidiMarshal.Pm_GetErrorText((MidiErrorType) ret)}");
                }
            }
            finally
            {
                gch.Free();
            }
        }
    }
}