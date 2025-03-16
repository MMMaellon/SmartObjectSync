#define COMMUNITY_LABS_SDK

using System;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using VRCSDK2;

namespace VRC.SDKBase.Editor
{
    [InitializeOnLoad]
    internal class VRC_SdkSplashScreen : EditorWindow
    {
        static VRC_SdkSplashScreen()
        {
            EditorApplication.update -= DoSplashScreen;
            EditorApplication.update += DoSplashScreen;
        }

        private static void DoSplashScreen()
        {
            EditorApplication.update -= DoSplashScreen;
            if (EditorApplication.isPlaying)
                return;

            if (EditorPrefs.GetBool("VRCSDK_ShowedSplashScreenFirstTime", false)) return;
            
            OpenSplashScreen();
            EditorPrefs.SetBool("VRCSDK_ShowedSplashScreenFirstTime", true);
        }
        
        [MenuItem("VRChat SDK/Splash Screen", false, 500)]
        public static void OpenSplashScreen()
        {
            var window = GetWindow<VRC_SdkSplashScreen>(true, "Welcome to VRChat SDK");
            window.minSize = new Vector2(400, 350);
            window.maxSize = new Vector2(400, 350);
        }
        
        public static void Open()
        {
            OpenSplashScreen();
        }

        private void CreateGUI()
        {
            var tree = Resources.Load<VisualTreeAsset>("VRCSdkSplashScreen");
            tree.CloneTree(rootVisualElement);
            rootVisualElement.styleSheets.Add(Resources.Load<StyleSheet>("VRCSdkPanelStyles"));
            rootVisualElement.styleSheets.Add(Resources.Load<StyleSheet>("VRCSdkSplashScreenStyles"));
            
#if UDON
            {
                rootVisualElement.Q<Button>("help-center-button").RemoveFromClassList("last");
                var examplesButton = rootVisualElement.Q<Button>("examples-button");
                examplesButton.RemoveFromClassList("d-none");
                examplesButton.clicked += () => EditorApplication.ExecuteMenuItem("VRChat SDK/🏠 Example Central");
                rootVisualElement.Q("bottom-block").style.backgroundImage = Resources.Load<Texture2D>("vrcSdkSplashUdon2");
                var bottomButton = rootVisualElement.Q<Button>("bottom-block-button");
                bottomButton.text = "Join other Creators in our Discord";
                bottomButton.clicked += () => {
                    Application.OpenURL("https://discord.gg/vrchat");
                };
            }
#endif

            rootVisualElement.Q<Button>("splash-block-bottom-button").clicked += () => 
            {
                Application.OpenURL("https://creators.vrchat.com/getting-started/");
            };
            rootVisualElement.Q<Button>("sdk-docs-button").clicked += () =>
            {
                Application.OpenURL("https://creators.vrchat.com/");
            };
            rootVisualElement.Q<Button>("vrc-faq-button").clicked += () =>
            {
                Application.OpenURL("https://vrchat.com/developer-faq");
            };
            rootVisualElement.Q<Button>("help-center-button").clicked += () =>
            {
                Application.OpenURL("https://help.vrchat.com");
            };
            rootVisualElement.Q<Button>("vrc-quest-content-button").clicked += () =>
            {
                Application.OpenURL("https://creators.vrchat.com/platforms/android/");
            };
            
#if !UDON
            {
                var bottomButton = rootVisualElement.Q<Button>("bottom-block-button");
                bottomButton.style.top = 4;
                bottomButton.text = "Click here to see great assets for VRChat creation";    
                bottomButton.clicked += () =>
                {
                    Application.OpenURL("https://assetstore.unity.com/lists/vrchat-picks-125734?aid=1101l7yuQ");
                };
            }
#endif
        }
    }
}