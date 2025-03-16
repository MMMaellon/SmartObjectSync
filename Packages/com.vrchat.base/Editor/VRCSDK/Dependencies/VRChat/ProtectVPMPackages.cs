using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VRC.SDKBase.Editor
{
    public class ProtectVPMPackages : UnityEditor.AssetModificationProcessor
    {
        public static string[] OnWillSaveAssets(string[] paths)
        {
            // skip check if settings allows changes
            if (VRCPackageSettings.Instance.allowVRCPackageChanges) return paths;
            
            List<string> pathsToSave = new List<string>();

            for (int i = 0; i < paths.Length; ++i)
            {
                var path = paths[i];
                var directories = path.Split(Path.DirectorySeparatorChar);
                if (directories.Length >= 2)
                {
                    var filename = Path.GetFileName(path);
                    var extension = Path.GetExtension(path);
                    if (directories[0] == "Packages" && directories[1].StartsWith("com.vrchat") && !(filename == "MirrorReflection.mat") && ! path.Contains("UdonProgramSources") && !path.Contains("SerializedUdonPrograms") && !(extension == ".meta"))
                    {
                        Debug.LogWarning(
                            $"Something tried to change {path}, which is part of a VRC Package, and will not be changed. Use \"Save As...\" instead if you want to make your own version.");
                        continue;
                    }
                }
                pathsToSave.Add(path);
            }
            return pathsToSave.ToArray();
        }
    }
}
