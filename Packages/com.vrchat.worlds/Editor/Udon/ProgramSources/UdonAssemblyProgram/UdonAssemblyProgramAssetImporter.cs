using System.IO;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;

namespace VRC.Udon.Editor.ProgramSources
{
    [ScriptedImporter(1, "uasm")]
    [UsedImplicitly]
    public class UdonAssemblyProgramAssetImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
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
