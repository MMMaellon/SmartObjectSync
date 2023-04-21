using System;
using System.Runtime.InteropServices;

namespace PortMidi
{
    public struct MidiDeviceInfo
    {
        PmDeviceInfo info;

        internal MidiDeviceInfo(int id, IntPtr ptr)
        {
            ID = id;
            this.info = (PmDeviceInfo) Marshal.PtrToStructure(ptr, typeof(PmDeviceInfo));
        }

        public int ID { get; set; }

        public string Interface => Marshal.PtrToStringAnsi(info.Interface);

        public string Name => Marshal.PtrToStringAnsi(info.Name);

        public bool IsInput => info.Input != 0;

        public bool IsOutput => info.Output != 0;

        public bool IsOpened => info.Opened != 0;

        public override string ToString()
        {
            return
                $"{Interface} - {Name} ({(IsInput ? (IsOutput ? "I/O" : "Input") : (IsOutput ? "Output" : "N/A"))} {(IsOpened ? "open" : String.Empty)})";
        }
    }
}