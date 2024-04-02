﻿using System;
using UnityEditor;
using UnityEngine;

namespace VRWorldToolkit.Editor
{
    public class About : EditorWindow
    {
        [MenuItem("VRWorld Toolkit/About VRWorld Toolkit", false, 40)]
        public static void ShowWindow()
        {
            var window = (About) GetWindow(typeof(About), true, "VRWorld Toolkit");
            window.minSize = new Vector2(600, 380);
            window.maxSize = new Vector2(600, 380);
            window.Show();
        }

        private static GUIStyle header, text;

        private static Texture iconTwitter, iconDiscord, iconGithub;

        [NonSerialized] private int clickCounter;

        public void OnEnable()
        {
            header = new GUIStyle
            {
                normal =
                {
                    background = Resources.Load("VRWorldToolkit/SplashTextures/VRWTSplashLogo") as Texture2D,
                    textColor = Color.white,
                },
                fixedHeight = 140
            };

            iconTwitter = Resources.Load("VRWorldToolkit/SplashTextures/IconTwitter") as Texture2D;
            iconDiscord = Resources.Load("VRWorldToolkit/SplashTextures/IconDiscord") as Texture2D;
            iconGithub = Resources.Load("VRWorldToolkit/SplashTextures/IconGithub") as Texture2D;
        }

        private void OnGUI()
        {
            // Header Image
            if (GUILayout.Button("", header))
            {
                clickCounter++;
                if (clickCounter >= 10)
                {
                    Debug.Log("Toggled benchmark mode for VRWorld Toolkit");
#if VRWT_BENCHMARK
                    ScriptingDefineManager.RemoveScriptingDefine("VRWT_BENCHMARK");
#else
                    ScriptingDefineManager.AddScriptingDefine("VRWT_BENCHMARK");
#endif
                }
            };

            // Information Texts
            GUILayout.Label("Welcome to VRWorld Toolkit!", EditorStyles.boldLabel);

            GUILayout.Label("VRWorld Toolkit is a project aimed at helping people get into world building faster without spending time combing different documentations for all the smaller mistakes you can make while making your first world. Even for experienced world builders, it helps make tedious steps like setting up post-processing faster and allows you not to forget the dozen little things you need to remember while building worlds.", Styles.RichTextWrap);

            GUILayout.Label("If you have suggestions, found problems with the included tools, or want to check my social channels, you can click on the buttons below. Feedback is always welcome, so I know what to improve!", Styles.RichTextWrap);

            GUILayout.FlexibleSpace();

            // Social Buttons
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(iconTwitter, GUIStyle.none)) Application.OpenURL("https://twitter.com/oneVRdev");
            GUILayout.Space(20);
            if (GUILayout.Button(iconDiscord, GUIStyle.none)) Application.OpenURL("https://discord.gg/8w2Tc6C");
            GUILayout.Space(20);
            if (GUILayout.Button(iconGithub, GUIStyle.none)) Application.OpenURL("https://github.com/oneVR/VRWorldToolkit");

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(20);
        }
    }
}