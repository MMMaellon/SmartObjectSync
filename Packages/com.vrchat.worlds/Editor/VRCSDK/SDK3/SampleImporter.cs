using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace VRC.SDK3.Editor
{
    public class SampleImporter
    {
        private const string exampleScenePath =
            "Packages/com.vrchat.worlds/Samples/UdonExampleScene/UdonExampleScene.unity";
        private const string ccplayerhitScenePath = 
            "Packages/com.vrchat.worlds/Samples/OnControllerColliderHitExampleScene/OnControllerColliderHitSampleScene.unity";
        private const string minimapScenePath = 
            "Packages/com.vrchat.worlds/Samples/GraphicsBlitExampleScene/Minimap Sample Scene.unity";
        private const string midiPlaybackScenePath = 
            "Packages/com.vrchat.worlds/Samples/MidiPlaybackExampleScene/MidiPlaybackScene.unity";
        private const string aiNavigationScenePath = 
            "Packages/com.vrchat.worlds/Samples/AINavmeshScene/AINavmeshSceneExample.unity";
        
        [MenuItem("VRChat SDK/Samples/UdonExampleScene", false, 981)]
        private static void OpenSampleUdonExampleScene()
        {
            if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(exampleScenePath);
            }
        }
        
        [MenuItem("VRChat SDK/Samples/ControllerColliderPlayerHit", false, 982)]
        private static void OpenCCPlayerHitExampleScene()
        {
            if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(ccplayerhitScenePath);
            }
        }
        
        [MenuItem("VRChat SDK/Samples/Minimap", false, 983)]
        private static void OpenMinimapExampleScene()
        {
            if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(minimapScenePath);
            }
        }
        
        [MenuItem("VRChat SDK/Samples/MidiPlayback", false, 984)]
        private static void OpenMidiPlaybackExampleScene()
        {
            if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(midiPlaybackScenePath);
            }
        }

        [MenuItem("VRChat SDK/Samples/Show Sample Documentation", false, 985)]
        private static void ShowSampleDocumentation()
        {
            Application.OpenURL("https://creators.vrchat.com/worlds/examples/");
        }
        
        [MenuItem("VRChat SDK/Samples/AI Navigation")]
        private static void OpenAINavigationScene()
        {
            if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(aiNavigationScenePath);
            }
        }
    }
}
