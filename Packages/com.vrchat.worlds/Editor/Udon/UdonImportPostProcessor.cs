using System.IO;
using UnityEditor;
using UnityEngine;

namespace VRC.Udon.Editor
{
    public class UdonImportPostProcessor : AssetPostprocessor
    {
        private const string PREFABS_INITIALIZED = "PrefabsInitialized";
        
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            // Get key unique to this project, exit early if we've run the function already
            string key = Path.Combine(Application.dataPath, PREFABS_INITIALIZED);
            if (EditorPrefs.HasKey(key))
                return;
            
            // Function never run for this project - compile and link all prefab programs
            foreach(string importedAsset in importedAssets)
            {
                UdonEditorManager.PopulateAssetDependenciesPrefabSerializedProgramAssetReferences(importedAsset);
            }

            UdonEditorManager.RecompileAllProgramSources();

            EditorPrefs.SetBool(key, true);
        }
    }
}
