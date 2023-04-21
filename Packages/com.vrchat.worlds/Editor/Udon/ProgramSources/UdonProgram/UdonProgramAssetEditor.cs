using UnityEditor;

namespace VRC.Udon.Editor.ProgramSources
{
    [CustomEditor(typeof(UdonProgramAsset))]
    public class UdonProgramAssetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            bool dirty = false;
            UdonProgramAsset programAsset = (UdonProgramAsset)target;
            programAsset.RunEditorUpdate(null, ref dirty);
            if(dirty)
            {
                EditorUtility.SetDirty(target);
            }
        }
    }
}
