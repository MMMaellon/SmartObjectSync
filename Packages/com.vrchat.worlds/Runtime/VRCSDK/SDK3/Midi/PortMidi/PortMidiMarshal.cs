using System;
using System.Runtime.InteropServices;

namespace PortMidi
{
    internal class PortMidiMarshal
    {
        private const int Hdrlength = 50;
        private const uint PmHostErrorMsgLen = 256;
        const int PmNoDevice = -1;

        [DllImport("portmidi")]
        public static extern MidiErrorType Pm_Initialize();

        [DllImport("portmidi")]
        public static extern IntPtr Pm_GetDeviceInfo(Int32 id);

        [DllImport("portmidi")]
        public static extern int Pm_CountDevices();

        [DllImport("portmidi")]
        public static extern Int32 Pm_GetDefaultInputDeviceID();

        [DllImport("portmidi")]
        public static extern Int32 Pm_GetDefaultOutputDeviceID();

        [DllImport("portmidi")]
        public static extern MidiErrorType Pm_OpenInput(
            out IntPtr stream,
            Int32 inputDevice,
            IntPtr inputDriverInfo,
            int bufferSize,
            MidiTimeProcDelegate timeProc,
            IntPtr timeInfo);

        [DllImport("portmidi")]
        public static extern MidiErrorType Pm_OpenOutput(
            out IntPtr stream,
            Int32 outputDevice,
            IntPtr outputDriverInfo,
            int bufferSize,
            MidiTimeProcDelegate time_proc,
            IntPtr time_info,
            int latency);

        [DllImport("portmidi")]
        public static extern MidiErrorType Pm_SetFilter(IntPtr stream, MidiFilter filters);

        [DllImport("portmidi")]
        public static extern MidiErrorType Pm_SetChannelMask(IntPtr stream, int mask);

        [DllImport("portmidi")]
        public static extern MidiErrorType Pm_Poll(IntPtr stream);

        [DllImport("portmidi")]
        public static extern int Pm_Read(IntPtr stream, IntPtr buffer, int length);

        [DllImport("portmidi")]
        public static extern MidiErrorType Pm_Write(IntPtr stream, IntPtr buffer, int length);

        [DllImport("portmidi")]
        public static extern MidiErrorType Pm_WriteSysEx(IntPtr stream, Int32 when, byte[] msg);

        [DllImport("portmidi")]
        public static extern MidiErrorType Pm_WriteShort(IntPtr stream, Int32 when, MidiMessage msg);
        
        [DllImport("portmidi")]
        public static extern MidiErrorType Pm_Abort(IntPtr stream);

        [DllImport("portmidi")]
        public static extern MidiErrorType Pm_Close(IntPtr stream);

        [DllImport("portmidi")]
        public static extern MidiErrorType Pm_Terminate();

        [DllImport("portmidi")]
        public static extern int Pm_HasHostError(IntPtr stream);

        [DllImport("portmidi")]
        public static extern string Pm_GetErrorText(MidiErrorType errnum);

        [DllImport("portmidi")]
        public static extern void Pm_GetHostErrorText(IntPtr msg, uint len);

        public static int Pm_Channel(int channel)
        {
            return 1 << channel;
        }

        public static int Pm_MessageStatus(int msg)
        {
            return msg & 0xFF;
        }

        public static int Pm_MessageData1(int msg)
        {
            return (msg >> 8) & 0xFF;
        }

        public static int Pm_MessageData2(int msg)
        {
            return (msg >> 16) & 0xFF;
        }
    }
}