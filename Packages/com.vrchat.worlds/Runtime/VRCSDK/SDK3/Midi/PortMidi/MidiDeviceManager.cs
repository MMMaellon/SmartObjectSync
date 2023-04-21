using System;
using System.Collections.Generic;
using PmDeviceID = System.Int32;
using PortMidiStream = System.IntPtr;
using PmError = PortMidi.MidiErrorType;

namespace PortMidi
{
    public static class MidiDeviceManager
    {
        private const int DefaultBufferSize = 1024;

        static MidiDeviceManager()
        {
            PortMidiMarshal.Pm_Initialize();
            AppDomain.CurrentDomain.DomainUnload += delegate { PortMidiMarshal.Pm_Terminate(); };
        }

        public static int DeviceCount => PortMidiMarshal.Pm_CountDevices();

        public static int DefaultInputDeviceId => PortMidiMarshal.Pm_GetDefaultInputDeviceID();

        public static int DefaultOutputDeviceId => PortMidiMarshal.Pm_GetDefaultOutputDeviceID();

        public static IEnumerable<MidiDeviceInfo> AllDevices
        {
            get
            {
                for (var i = 0; i < DeviceCount; i++)
                {
                    yield return GetDeviceInfo(i);
                }
            }
        }

        private static MidiDeviceInfo GetDeviceInfo(PmDeviceID id)
        {
            return new MidiDeviceInfo(id, PortMidiMarshal.Pm_GetDeviceInfo(id));
        }

        public static MidiInput OpenInput(PmDeviceID inputDevice)
        {
            return OpenInput(inputDevice, DefaultBufferSize);
        }

        private static MidiInput OpenInput(PmDeviceID inputDevice, int bufferSize)
        {
            PortMidiStream stream = default;
            var e = PortMidiMarshal.Pm_OpenInput(out stream, inputDevice, IntPtr.Zero, bufferSize, null, IntPtr.Zero);
            if (e != PmError.NoError)
            {
                throw new MidiException(e, $"Failed to open MIDI input device {e}");
            }
            
            return new MidiInput(stream, inputDevice);
        }

        public static MidiOutput OpenOutput(PmDeviceID outputDevice)
        {
            PortMidiStream stream;
            var e = PortMidiMarshal.Pm_OpenOutput(out stream, outputDevice, IntPtr.Zero, 0, null, IntPtr.Zero, 0);
            if (e != PmError.NoError)
            {
                throw new MidiException(e, $"Failed to open MIDI output device {e}");
            }
            return new MidiOutput(stream, outputDevice, 0);
        }
    }
}