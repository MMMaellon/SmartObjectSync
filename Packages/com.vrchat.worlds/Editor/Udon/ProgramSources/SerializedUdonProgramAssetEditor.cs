using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
using VRC.Udon.Common.Interfaces;
using VRC.Udon.ProgramSources;
using VRC.Udon.Serialization.OdinSerializer;

namespace VRC.Udon.Editor.ProgramSources
{
    [CustomEditor(typeof(SerializedUdonProgramAsset))]
    public class SerializedUdonProgramAssetEditor : UnityEditor.Editor
    {
        private SerializedProperty _serializedProgramBytesStringSerializedProperty;
        private SerializedProperty _serializationDataFormatSerializedProperty;

        private void OnEnable()
        {
            _serializedProgramBytesStringSerializedProperty = serializedObject.FindProperty("serializedProgramBytesString");
            _serializationDataFormatSerializedProperty = serializedObject.FindProperty("serializationDataFormat");
        }

        public override void OnInspectorGUI()
        {
            DrawSerializationDebug();
        }

        [Conditional("UDON_DEBUG")]
        private void DrawSerializationDebug()
        {
            EditorGUILayout.LabelField($"DataFormat: {(DataFormat)_serializationDataFormatSerializedProperty.enumValueIndex}");
            
            if(string.IsNullOrEmpty(_serializedProgramBytesStringSerializedProperty.stringValue))
            {
                return;
            }

            if(_serializationDataFormatSerializedProperty.enumValueIndex == (int)DataFormat.JSON)
            {
                using(new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.TextArea(System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(_serializedProgramBytesStringSerializedProperty.stringValue)));
                }
            }
            else
            {
                using(new EditorGUI.DisabledScope(true))
                {
                    SerializedUdonProgramAsset serializedUdonProgramAsset = (SerializedUdonProgramAsset)target;
                    IUdonProgram udonProgram = serializedUdonProgramAsset.RetrieveProgram();
                    byte[] serializedBytes = SerializationUtility.SerializeValue(udonProgram, DataFormat.JSON, out List<UnityEngine.Object> _);
                    EditorGUILayout.TextArea(System.Text.Encoding.UTF8.GetString(serializedBytes));
                }
            }
        }
    }
}
