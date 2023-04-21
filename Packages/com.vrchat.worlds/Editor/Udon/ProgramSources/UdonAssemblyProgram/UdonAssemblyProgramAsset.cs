using System;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using VRC.Udon.Editor.ProgramSources;
using VRC.Udon.Editor.ProgramSources.Attributes;

[assembly: UdonProgramSourceNewMenu(typeof(UdonAssemblyProgramAsset), "Udon Assembly Program Asset")]

namespace VRC.Udon.Editor.ProgramSources
{
    [CreateAssetMenu(menuName = "VRChat/Udon/Udon Assembly Program Asset", fileName = "New Udon Assembly Program Asset")]
    public class UdonAssemblyProgramAsset : UdonProgramAsset
    {
        [SerializeField]
        protected string udonAssembly = "";

        [SerializeField]
        protected string assemblyError = null;

        public string AssemblyError => assemblyError;

        public delegate void AssembleDelegate(bool success, string assembly);
        public event AssembleDelegate OnAssemble;

        protected override void DrawProgramSourceGUI(UdonBehaviour udonBehaviour, ref bool dirty)
        {
            DrawAssemblyErrorTextArea();

            DrawPublicVariables(udonBehaviour, ref dirty);
            
            DrawAssemblyTextArea(!Application.isPlaying, ref dirty);
            DrawProgramDisassembly();
        }

        protected override void RefreshProgramImpl()
        {
            AssembleProgram();
        }

        [PublicAPI]
        protected virtual void DrawAssemblyTextArea(bool allowEditing, ref bool dirty)
        {
            EditorGUILayout.LabelField("Assembly Code", EditorStyles.boldLabel);
            if(GUILayout.Button("Copy Assembly To Clipboard"))
            {
                EditorGUIUtility.systemCopyBuffer = udonAssembly;
            }

            EditorGUI.BeginChangeCheck();
            using(new EditorGUI.DisabledScope(!allowEditing))
            {
                string newAssembly = EditorGUILayout.TextArea(udonAssembly);
                if(EditorGUI.EndChangeCheck())
                {
                    dirty = true;
                    Undo.RecordObject(this, "Edit Assembly Program Code");
                    udonAssembly = newAssembly;
                    UdonEditorManager.Instance.QueueAndRefreshProgram(this);
                }
            }
        }

        [PublicAPI]
        protected void DrawAssemblyErrorTextArea()
        {
            if(string.IsNullOrEmpty(assemblyError))
            {
                return;
            }

            EditorGUILayout.LabelField("Assembly Error", EditorStyles.boldLabel);
            using(new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextArea(assemblyError);
            }
        }

        [PublicAPI]
        protected void AssembleProgram()
        {
            try
            {
                program = UdonEditorManager.Instance.Assemble(udonAssembly);
                assemblyError = null;
                OnAssemble?.Invoke(true, udonAssembly);
            }
            catch(Exception e)
            {
                program = null;
                assemblyError = e.Message;
                Debug.LogException(e);
                OnAssemble?.Invoke(false, assemblyError);
            }
        }

    }
}
