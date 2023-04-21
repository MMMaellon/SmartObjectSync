using UnityEditor;

namespace VRC.Udon.Editor.ProgramSources.UdonGraphProgram
{
    [CustomEditor(typeof(UdonGraphProgramAsset))]
    public class UdonGraphProgramAssetEditor : UdonAssemblyProgramAssetEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var asset = (UdonGraphProgramAsset) target;
            EditorGUI.BeginChangeCheck();
            asset.graphData.updateOrder = EditorGUILayout.IntField("UpdateOrder", asset.graphData.updateOrder);
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(target);
                AssetDatabase.SaveAssets();
            }
        }
    }
}
