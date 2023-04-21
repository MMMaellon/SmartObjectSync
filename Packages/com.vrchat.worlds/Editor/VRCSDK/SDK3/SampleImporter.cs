using UnityEditor;
using UnityEditor.SceneManagement;

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
        
        [MenuItem("VRChat SDK/Samples/UdonExampleScene")]
        private static void OpenSampleUdonExampleScene()
        {
            if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(exampleScenePath);
            }
        }
        
        [MenuItem("VRChat SDK/Samples/ControllerColliderPlayerHit")]
        private static void OpenCCPlayerHitExampleScene()
        {
            if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(ccplayerhitScenePath);
            }
        }
        
        [MenuItem("VRChat SDK/Samples/Minimap")]
        private static void OpenMinimapExampleScene()
        {
            if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(minimapScenePath);
            }
        }
        
        [MenuItem("VRChat SDK/Samples/MidiPlayback")]
        private static void OpenMididPlaybackExampleScene()
        {
            if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(midiPlaybackScenePath);
            }
        }
    }
}