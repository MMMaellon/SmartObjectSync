#if (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN) && !UNITY_ANDROID
using System;
using System.Collections.Generic;
using System.Linq;
#if VRC_CLIENT
using VRC.Core;
#endif
using PortMidi;
using UnityEngine;
using VRC.SDK3.Midi;


namespace VRC.SDKBase.Midi
{
    public class VRCPortMidiInput: IVRCMidiInput
    {

        private MidiInput _input;
        private byte[] _data;
        private MidiDeviceInfo _info;
        
        public bool OpenDevice(string name = null)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(name))
                {
                    _info = MidiDeviceManager.AllDevices.FirstOrDefault(device =>
                        device.IsInput && device.Name.ToLower().Contains(name.ToLower()));
                }
                else
                {
                    _info = MidiDeviceManager.AllDevices.FirstOrDefault(device =>
                        device.ID == MidiDeviceManager.DefaultInputDeviceId);
                }
                
                _input = MidiDeviceManager.OpenInput(_info.ID);
                _input.SetFilter(MidiFilter.Active | MidiFilter.SysEx | MidiFilter.Clock | MidiFilter.Play | MidiFilter.Tick | MidiFilter.Undefined | MidiFilter.Reset | MidiFilter.RealTime | MidiFilter.AF | MidiFilter.Program | MidiFilter.PitchBend | MidiFilter.SystemCommon);
                _data = new byte[1024];
                return true;
            }
            catch (Exception e)
            {
                #if VRC_CLIENT
                VRC.Core.Logger.LogError($"Error opening Default Device: {e.Message}");
                #else
                Debug.Log($"Error opening Default Device: {e.Message}");
                #endif
                return false;
            }
        }

        public void Close()
        {
            if (_input != null)
            {
                _input.Close();
            }
        }

        public void Update()
        {
            if (_input == null) return;
            
            if (_input.HasData)
            {
                // Portmidi reports 4 bytes per event but the buffer has 8 bytes so we multiply count by 2
                int count = (_input.Read(_data, 0, _data.Length)) * 2;
                for (int i = 0; i < count; i+=8)
                {
                    ConvertAndSend(_data[i], _data[i + 1], _data[i + 2]);
                }
            }
        }

        public IEnumerable<string> GetDeviceNames()
        {
            return MidiDeviceManager.AllDevices.Select(d => d.Name);
        }

        private void ConvertAndSend(byte status, byte data1, byte data2)
        {
            var command = status & 0xF0;  // mask off all but top 4 bits

            if (command >= 0x80 && command <= 0xE0) {
                // it's a voice message
                // find the channel by masking off all but the low 4 bits
                var channel = status & 0x0F;
                
                if (command == VRCMidiHandler.STATUS_NOTE_ON || command == VRCMidiHandler.STATUS_NOTE_OFF || command == VRCMidiHandler.STATUS_CONTROL_CHANGE)
                {
                    OnMidiVoiceMessage?.Invoke(this, new MidiVoiceEventArgs(command, channel, data1, data2));
                }
                else
                {
#if VRC_CLIENT
                VRC.Core.Logger.Log($"command:{command} channel:{channel} data1:{data1} data2:{data2}");
#else
                Debug.Log($"command:{command} channel:{channel} data1:{data1} data2:{data2}");
#endif
                }

            }
            else
            {
                // it's a system message, ignore for now
                OnMidiRawMessage?.Invoke(this, new MidiRawEventArgs(status, data1, data2));
            }
        }

        public string Name => _info.Name;
        public event MidiVoiceMessageDelegate OnMidiVoiceMessage;
        public event MidiRawMessageDelegate OnMidiRawMessage;
    }
}
#endif