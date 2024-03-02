using System.IO;
using JetBrains.Annotations;
using UnityEditor;

using UnityEngine;

namespace VRC.Udon.Editor.ProgramSources
{
    [UnityEditor.AssetImporters.ScriptedImporter(1, "uasm")]
    [UsedImplicitly]
    public class UdonAssemblyProgramAssetImporter : UnityEditor.AssetImporters.ScriptedImporter
    {
        public override void OnImportAsset(UnityEditor.AssetImporters.AssetImportContext ctx)
        {
            UdonAssemblyProgramAsset udonAssemblyProgramAsset = ScriptableObject.CreateInstance<UdonAssemblyProgramAsset>();
            SerializedObject serializedUdonAssemblyProgramAsset = new SerializedObject(udonAssemblyProgramAsset);
            SerializedProperty udonAssemblyProperty = serializedUdonAssemblyProgramAsset.FindProperty("udonAssembly");
            udonAssemblyProperty.stringValue = File.ReadAllText(ctx.assetPath);
            serializedUdonAssemblyProgramAsset.ApplyModifiedProperties();

            udonAssemblyProgramAsset.RefreshProgram();

            ctx.AddObjectToAsset("Imported Udon Assembly Program", udonAssemblyProgramAsset);
            ctx.SetMainObject(udonAssemblyProgramAsset);
        }
    }
}
