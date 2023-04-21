using System;

namespace PortMidi
{
    public abstract class MidiStream : IDisposable
    {
        internal IntPtr stream;
        internal Int32 device;

        protected MidiStream(IntPtr stream, Int32 deviceID)
        {
            this.stream = stream;
            this.device = deviceID;
        }

        public void Abort()
        {
            PortMidiMarshal.Pm_Abort(stream);
        }

        public void Close()
        {
            Dispose();
        }

        public void Dispose()
        {
            PortMidiMarshal.Pm_Close(stream);
        }

        public void SetFilter(MidiFilter filters)
        {
            PortMidiMarshal.Pm_SetFilter(stream, filters);
        }

        public void SetChannelMask(int mask)
        {
            PortMidiMarshal.Pm_SetChannelMask(stream, mask);
        }
    }
}