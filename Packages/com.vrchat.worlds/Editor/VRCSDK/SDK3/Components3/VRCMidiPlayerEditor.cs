using UnityEditor;
using UnityEngine;
using VRC.SDK3.Midi;

namespace VRC.SDK3.Editor
{
    [CustomEditor(typeof(VRCMidiPlayer))]
    public class VRCMidiPlayerEditor : UnityEditor.Editor
    {
        private VRCMidiPlayer _player;
        public bool displayDebugBlocks;
        
        // Serialized Properties
        private SerializedProperty _midiFileProp;
        private SerializedProperty _audioSourceProp;
        private SerializedProperty _targetBehavioursProp;
        
        private const string MidiAssetReloadKey = "MidiAssetReloaded";
        
        void OnEnable()
        {
            // Fetch the objects from the GameObject script to display in the inspector
            _midiFileProp = serializedObject.FindProperty(nameof(VRCMidiPlayer.midiFile));
            _audioSourceProp = serializedObject.FindProperty(nameof(VRCMidiPlayer.audioSource));
            _targetBehavioursProp = serializedObject.FindProperty(nameof(VRCMidiPlayer.targetBehaviours));
            
            _player = (VRCMidiPlayer)target;
        }

        private void Awake()
        {
            displayDebugBlocks = EditorPrefs.GetBool(GetPrefsNameFor(nameof(displayDebugBlocks)), false);
        }

        private static string GetPrefsNameFor(string value)
        {
            return $"VRCMidiPlayerEditor.{value}";
        }

        public override void OnInspectorGUI()
        {
            bool _isReady = _player.midiFile != null && 
                            _player.audioSource != null &&
                            _player.audioSource.clip != null &&
                            _player.targetBehaviours.Length > 0 &&
                            _player.targetBehaviours[0] != null ;

            if (_isReady)
            {
                EditorGUILayout.HelpBox("✔ Midi Player is Ready!", MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox("Not Ready - see messages below.", MessageType.Warning);
            }
            
            // Display Midi File Field
            EditorGUILayout.PropertyField(
                _midiFileProp,
                new GUIContent(
                    "Midi File", null, 
                    "The MIDI file in SMF format whose data you want to trigger."
                    ));
            
            // Ensure a Midi File is set before continuing
            if (!_player.midiFile)
            {
                EditorGUILayout.HelpBox("Choose a Midi File to continue setting up the Player.", MessageType.Info);
                serializedObject.ApplyModifiedProperties();
                return;
            }
            
            // Display Audio Source Field
            EditorGUILayout.PropertyField(
                _audioSourceProp,
                new GUIContent(
                    "Audio Source", null, 
                    "The AudioSource component with the audio clip corresponding to your MIDI data."
                ));
            
            // Add audio source if it's not set
            if (!_player.audioSource)
            {
                EditorGUILayout.HelpBox("Set or Create an AudioSource to continue.", MessageType.Info);
                if (GUILayout.Button("Create One Here"))
                {
                    _player.audioSource = _player.gameObject.AddComponent<AudioSource>();
                }
                serializedObject.ApplyModifiedProperties();
                return;
            }
            
            // Force reimport midi assets one time per session
            if (_player.midiFile != null && _player.midiFile.audioClip == null)
            {
                string key = $"{MidiAssetReloadKey}-{_player.midiFile.GetInstanceID()}";
                if (!SessionState.GetBool(key, false))
                {
                    AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(_player.midiFile), ImportAssetOptions.ForceUpdate);
                    SessionState.SetBool(key, true);
                }
            }
            
            // Automatically set AudioSource clip from MidiAsset if possible
            else if (_player.midiFile.audioClip != null && _player.audioSource.clip == null || _player.audioSource.clip != _player.midiFile.audioClip )
            {
                _player.audioSource.clip = _player.midiFile.audioClip;
            }

            if (_player.audioSource.clip == null)
            {
                EditorGUILayout.HelpBox("You need to set the AudioClip in the AudioSource.", MessageType.Warning);
                serializedObject.ApplyModifiedProperties();
                return;
            }

            // Display target UdonBehaviours field
            EditorGUILayout.PropertyField(
                _targetBehavioursProp,
                new GUIContent(
                    "Target Behaviours", null, 
                    "An array of UdonBehaviours which will have MIDI Note On and Off events sent to them"
                ));
            
            // Exit Early if there are not target behaviours set
            if (_player.targetBehaviours.Length == 0 || _player.targetBehaviours[0] == null)
            {
                EditorGUILayout.HelpBox("Set some target UdonBehaviours above to continue.", MessageType.Info);
                serializedObject.ApplyModifiedProperties();
                return;
            }

            bool openVisualizer = GUILayout.Button("Open Visualizer");
            if (openVisualizer)
            {
                VRCMidiEditorVisualizer.Init(_player.midiFile);
            }
            
            // Apply changes to the serializedProperty - always do this at the end of OnInspectorGUI.
            serializedObject.ApplyModifiedProperties();
        }
        
    }
}