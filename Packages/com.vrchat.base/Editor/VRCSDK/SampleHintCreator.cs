using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace VRC.SDKBase.Editor
{
    [InitializeOnLoad]
    public class SampleHintCreator
    {
        private static VRCPackageSettings _settings;
        private const string SamplesMovedString = "You can now find them in the menu under VRChat SDK/Samples";
        private const string FilePath = "Assets/Examples and Samples Have Moved.txt";
        static SampleHintCreator()
        {
            if (_settings == null)
            {
                _settings = VRCPackageSettings.Create();
            }

            if (!_settings.samplesHintCreated)
            {
                try
                {
                    File.WriteAllText(FilePath, SamplesMovedString);
                    AssetDatabase.ImportAsset(FilePath);
                    _settings.samplesHintCreated = true;
                    _settings.Save();
                }
                catch (Exception)
                {
                    Debug.LogWarning($"Could not create Sample hint file. {SamplesMovedString}");
                }
            }
        }
    }
}