#if !COMPILER_UDONSHARP && UNITY_EDITOR
using VRC.SDKBase.Editor.BuildPipeline;
using VRC.SDKBase;
using UnityEditor;
using UnityEngine;
using UdonSharpEditor;
using System.Collections.Immutable;

namespace MMMaellon
{
    [InitializeOnLoad]
    public class SmartObjectSyncMenu : IVRCSDKBuildRequestedCallback
    {
        public int callbackOrder => 0;
        [InitializeOnLoadMethod]
        public static void Initialize()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        public static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.ExitingEditMode) return;
            Setup();
        }
        public bool OnBuildRequested(VRCSDKRequestedBuildType requestedBuildType)
        {
            return Setup();
        }
        public static void SetupSmartObjectSync(SmartObjectSync sync)
        {
            if (!Utilities.IsValid(sync))
            {
                return;
            }
            if (!IsEditable(sync))
            {
                Debug.LogErrorFormat(sync, "<color=red>[SmartObjectSync AutoSetup]: ERROR</color> {0}", "SmartObjectSync is not editable");
            }
            SmartObjectSyncEditor.SetupStates(sync);
        }
        public static bool IsEditable(Component component)
        {
            return !EditorUtility.IsPersistent(component.transform.root.gameObject) && !(component.gameObject.hideFlags == HideFlags.NotEditable || component.gameObject.hideFlags == HideFlags.HideAndDontSave);
        }


        public static bool Setup()
        {
            foreach (SmartObjectSync sync in GameObject.FindObjectsOfType<SmartObjectSync>())
            {
                SetupSmartObjectSync(sync);
            }
            foreach (SmartObjectSyncExtra.JointAttachmentState joint in GameObject.FindObjectsOfType<SmartObjectSyncExtra.JointAttachmentState>())
            {
                SmartObjectSyncExtra.JointAttachmentState.SetupJointAttachmentState(joint);
            }
            return true;
        }
    }
}

#endif
