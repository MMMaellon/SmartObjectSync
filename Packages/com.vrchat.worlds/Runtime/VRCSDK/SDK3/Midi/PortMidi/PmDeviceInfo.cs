using System;
using System.Runtime.InteropServices;

namespace PortMidi
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct PmDeviceInfo
    {
        public int StructVersion;
        public IntPtr Interface;
        public IntPtr Name;
        public int Input;
        public int Output;
        public int Opened;

        public override string ToString()
        {
            return $"{StructVersion}, {Interface}, {Name}, {Input}, {Output}, {Opened}";
        }
    }
}