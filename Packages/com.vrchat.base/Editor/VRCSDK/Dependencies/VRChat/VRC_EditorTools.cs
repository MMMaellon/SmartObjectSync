#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using UnityEditorInternal;
using System.Reflection;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine.Rendering;
#if !VRC_CLIENT
using VRC.Core;
using VRC.SDKBase.Editor.Api;
#endif
using File = UnityEngine.Windows.File;
using Object = UnityEngine.Object;
#if POST_PROCESSING_INCLUDED
using UnityEngine.Rendering.PostProcessing;
#endif

[assembly: InternalsVisibleTo("VRC.SDK3A.Editor")]
[assembly: InternalsVisibleTo("VRC.SDK3.Editor")]
namespace VRC.SDKBase
{
    public static class VRC_EditorTools
    {
        private static LayerMask LayerMaskPopupInternal(LayerMask selectedValue, System.Func<int, string[], int> showMask)
        {
            string[] layerNames = InternalEditorUtility.layers;
            List<int> layerNumbers = new List<int>();

            foreach (string layer in layerNames)
                layerNumbers.Add(LayerMask.NameToLayer(layer));

            int mask = 0;
            for (int idx = 0; idx < layerNumbers.Count; ++idx)
                if (((1 << layerNumbers[idx]) & selectedValue.value) > 0)
                    mask |= (1 << idx);

            mask = showMask(mask, layerNames);

            selectedValue.value = 0;
            for (int idx = 0; idx < layerNumbers.Count; ++idx)
                if (((1 << idx) & mask) > 0)
                    selectedValue.value |= (1 << layerNumbers[idx]);

            return selectedValue;
        }

        public static LayerMask LayerMaskPopup(LayerMask selectedValue, params GUILayoutOption[] options)
        {
            return LayerMaskPopupInternal(selectedValue, (mask, layerNames) => EditorGUILayout.MaskField(mask, layerNames, options));
        }

        public static LayerMask LayerMaskPopup(string label, LayerMask selectedValue, params GUILayoutOption[] options)
        {
            return LayerMaskPopupInternal(selectedValue, (mask, layerNames) => EditorGUILayout.MaskField(label, mask, layerNames, options));
        }

        public static LayerMask LayerMaskPopup(Rect rect, LayerMask selectedValue, GUIStyle style = null)
        {
            System.Func<int, string[], int> show = (mask, layerNames) =>
            {
                if (style == null)
                    return EditorGUI.MaskField(rect, mask, layerNames);
                else
                    return EditorGUI.MaskField(rect, mask, layerNames, style);
            };
            return LayerMaskPopupInternal(selectedValue, show);
        }

        public static LayerMask LayerMaskPopup(Rect rect, string label, LayerMask selectedValue, GUIStyle style = null)
        {
            System.Func<int, string[], int> show = (mask, layerNames) =>
            {
                if (style == null)
                    return EditorGUI.MaskField(rect, label, mask, layerNames);
                else
                    return EditorGUI.MaskField(rect, label, mask, layerNames, style);
            };
            return LayerMaskPopupInternal(selectedValue, show);
        }

        private static T FilteredEnumPopupInternal<T>(T selectedValue, System.Func<T, bool> predicate, System.Func<int, string[], int> showPopup, System.Func<string, string> rename) where T : struct, System.IConvertible
        {
            if (!typeof(T).IsEnum)
                throw new System.ArgumentException(typeof(T).Name + " is not an Enum", "T");

            T[] ary = System.Enum.GetValues(typeof(T)).Cast<T>().Where(v => predicate(v)).ToArray();
            string[] names = ary.Select(e => System.Enum.GetName(typeof(T), e)).ToArray();

            int selectedIdx = 0;
            for (; selectedIdx < ary.Length; ++selectedIdx)
                if (ary[selectedIdx].Equals(selectedValue))
                    break;
            if (selectedIdx == ary.Length)
                selectedIdx = 0;

            if (ary.Length == 0)
                throw new System.ArgumentException("Predicate filtered out all options", "predicate");

            if (rename != null)
                return ary[showPopup(selectedIdx, names.Select(rename).ToArray())];
            else
                return ary[showPopup(selectedIdx, names)];
        }

        private static void FilteredEnumPopupInternal<T>(SerializedProperty enumProperty, System.Func<T, bool> predicate, System.Func<int, string[], int> showPopup, System.Func<string, string> rename) where T : struct, System.IConvertible
        {
            string selectedName = enumProperty.enumNames[enumProperty.enumValueIndex];
            T selectedValue = FilteredEnumPopupInternal<T>((T)System.Enum.Parse(typeof(T), selectedName), predicate, showPopup, rename);
            selectedName = selectedValue.ToString();
            for (int idx = 0; idx < enumProperty.enumNames.Length; ++idx)
                if (enumProperty.enumNames[idx] == selectedName)
                {
                    enumProperty.enumValueIndex = idx;
                    break;
                }
        }

        public static T FilteredEnumPopup<T>(string label, T selectedValue, System.Func<T, bool> predicate, System.Func<string, string> rename = null, params GUILayoutOption[] options) where T : struct, System.IConvertible
        {
            return FilteredEnumPopupInternal(selectedValue, predicate, (selectedIdx, names) => EditorGUILayout.Popup(label, selectedIdx, names, options), rename);
        }

        public static T FilteredEnumPopup<T>(T selectedValue, System.Func<T, bool> predicate, System.Func<string, string> rename = null, params GUILayoutOption[] options) where T : struct, System.IConvertible
        {
            return FilteredEnumPopupInternal(selectedValue, predicate, (selectedIdx, names) => EditorGUILayout.Popup(selectedIdx, names, options), rename);
        }

        public static T FilteredEnumPopup<T>(Rect rect, string label, T selectedValue, System.Func<T, bool> predicate, System.Func<string, string> rename = null, GUIStyle style = null) where T : struct, System.IConvertible
        {
            System.Func<int, string[], int> show = (selectedIdx, names) =>
            {
                if (style != null)
                    return EditorGUI.Popup(rect, label, selectedIdx, names, style);
                else
                    return EditorGUI.Popup(rect, label, selectedIdx, names);
            };
            return FilteredEnumPopupInternal(selectedValue, predicate, show, rename);
        }

        public static T FilteredEnumPopup<T>(Rect rect, T selectedValue, System.Func<T, bool> predicate, System.Func<string, string> rename = null, GUIStyle style = null) where T : struct, System.IConvertible
        {
            System.Func<int, string[], int> show = (selectedIdx, names) =>
            {
                if (style != null)
                    return EditorGUI.Popup(rect, selectedIdx, names, style);
                else
                    return EditorGUI.Popup(rect, selectedIdx, names);
            };
            return FilteredEnumPopupInternal(selectedValue, predicate, show, rename);
        }

        public static void FilteredEnumPopup<T>(string label, SerializedProperty selectedValue, System.Func<T, bool> predicate, System.Func<string, string> rename = null, params GUILayoutOption[] options) where T : struct, System.IConvertible
        {
            FilteredEnumPopupInternal(selectedValue, predicate, (selectedIdx, names) => EditorGUILayout.Popup(label, selectedIdx, names, options), rename);
        }

        public static void FilteredEnumPopup<T>(SerializedProperty selectedValue, System.Func<T, bool> predicate, System.Func<string, string> rename = null, params GUILayoutOption[] options) where T : struct, System.IConvertible
        {
            FilteredEnumPopupInternal(selectedValue, predicate, (selectedIdx, names) => EditorGUILayout.Popup(selectedIdx, names, options), rename);
        }

        public static void FilteredEnumPopup<T>(Rect rect, string label, SerializedProperty selectedValue, System.Func<T, bool> predicate, System.Func<string, string> rename = null, GUIStyle style = null) where T : struct, System.IConvertible
        {
            System.Func<int, string[], int> show = (selectedIdx, names) =>
            {
                if (style != null)
                    return EditorGUI.Popup(rect, label, selectedIdx, names, style);
                else
                    return EditorGUI.Popup(rect, label, selectedIdx, names);
            };
            FilteredEnumPopupInternal(selectedValue, predicate, show, rename);
        }

        public static void FilteredEnumPopup<T>(Rect rect, SerializedProperty selectedValue, System.Func<T, bool> predicate, System.Func<string, string> rename = null, GUIStyle style = null) where T : struct, System.IConvertible
        {
            System.Func<int, string[], int> show = (selectedIdx, names) =>
            {
                if (style != null)
                    return EditorGUI.Popup(rect, selectedIdx, names, style);
                else
                    return EditorGUI.Popup(rect, selectedIdx, names);
            };
            FilteredEnumPopupInternal(selectedValue, predicate, show, rename);
        }

        private static VRC.SDKBase.VRC_Trigger.TriggerEvent CustomTriggerPopupInternal(VRC.SDKBase.VRC_Trigger sourceTrigger, VRC.SDKBase.VRC_Trigger.TriggerEvent selectedValue, System.Func<int, string[], int> show)
        {
            if (sourceTrigger == null)
                return null;

            VRC.SDKBase.VRC_Trigger.TriggerEvent[] actionsAry = sourceTrigger.Triggers.Where(t => t.TriggerType == VRC.SDKBase.VRC_Trigger.TriggerType.Custom).ToArray();
            string[] names = actionsAry.Select(t => t.Name).ToArray();

            int selectedIdx = Math.Max(0, names.Length - 1);
            if (selectedValue != null)
                for (; selectedIdx > 0; --selectedIdx)
                    if (names[selectedIdx] == selectedValue.Name)
                        break;
            if (actionsAry.Length == 0)
                return null;

            return actionsAry[show(selectedIdx, names)];
        }

        public static VRC.SDKBase.VRC_Trigger.TriggerEvent CustomTriggerPopup(Rect rect, VRC.SDKBase.VRC_Trigger sourceTrigger, VRC.SDKBase.VRC_Trigger.TriggerEvent selectedValue, GUIStyle style = null)
        {
            System.Func<int, string[], int> show = (selectedIdx, names) =>
            {
                if (style != null)
                    return EditorGUI.Popup(rect, selectedIdx, names, style);
                else
                    return EditorGUI.Popup(rect, selectedIdx, names);
            };

            return CustomTriggerPopupInternal(sourceTrigger, selectedValue, show);
        }

        public static VRC.SDKBase.VRC_Trigger.TriggerEvent CustomTriggerPopup(Rect rect, string label, VRC.SDKBase.VRC_Trigger sourceTrigger, VRC.SDKBase.VRC_Trigger.TriggerEvent selectedValue, GUIStyle style = null)
        {
            System.Func<int, string[], int> show = (selectedIdx, names) =>
            {
                if (style != null)
                    return EditorGUI.Popup(rect, label, selectedIdx, names, style);
                else
                    return EditorGUI.Popup(rect, label, selectedIdx, names);
            };

            return CustomTriggerPopupInternal(sourceTrigger, selectedValue, show);
        }

        public static VRC.SDKBase.VRC_Trigger.TriggerEvent CustomTriggerPopup(VRC.SDKBase.VRC_Trigger sourceTrigger, VRC.SDKBase.VRC_Trigger.TriggerEvent selectedValue, params GUILayoutOption[] options)
        {
            return CustomTriggerPopupInternal(sourceTrigger, selectedValue, (selectedIdx, names) => EditorGUILayout.Popup(selectedIdx, names, options));
        }

        public static VRC.SDKBase.VRC_Trigger.TriggerEvent CustomTriggerPopup(string label, VRC.SDKBase.VRC_Trigger sourceTrigger, VRC.SDKBase.VRC_Trigger.TriggerEvent selectedValue, params GUILayoutOption[] options)
        {
            return CustomTriggerPopupInternal(sourceTrigger, selectedValue, (selectedIdx, names) => EditorGUILayout.Popup(label, selectedIdx, names, options));
        }

        public static VRC.SDKBase.VRC_Trigger.TriggerEvent CustomTriggerPopup(string label, VRC.SDKBase.VRC_Trigger sourceTrigger, string selectedValue, params GUILayoutOption[] options)
        {
            if (sourceTrigger == null)
                return null;

            return CustomTriggerPopup(label, sourceTrigger, sourceTrigger.Triggers.FirstOrDefault(t => t.TriggerType == VRC.SDKBase.VRC_Trigger.TriggerType.Custom && t.Name == selectedValue), options);
        }

        public static VRC.SDKBase.VRC_Trigger.TriggerEvent CustomTriggerPopup(VRC.SDKBase.VRC_Trigger sourceTrigger, string selectedValue, params GUILayoutOption[] options)
        {
            if (sourceTrigger == null)
                return null;

            return CustomTriggerPopup(sourceTrigger, sourceTrigger.Triggers.FirstOrDefault(t => t.TriggerType == VRC.SDKBase.VRC_Trigger.TriggerType.Custom && t.Name == selectedValue), options);
        }

        public static VRC.SDKBase.VRC_Trigger.TriggerEvent CustomTriggerPopup(Rect rect, VRC.SDKBase.VRC_Trigger sourceTrigger, string selectedValue, GUIStyle style = null)
        {
            if (sourceTrigger == null)
                return null;

            return CustomTriggerPopup(rect, sourceTrigger, sourceTrigger.Triggers.FirstOrDefault(t => t.TriggerType == VRC.SDKBase.VRC_Trigger.TriggerType.Custom && t.Name == selectedValue), style);
        }

        public static VRC.SDKBase.VRC_Trigger.TriggerEvent CustomTriggerPopup(Rect rect, string label, VRC.SDKBase.VRC_Trigger sourceTrigger, string selectedValue, GUIStyle style = null)
        {
            if (sourceTrigger == null)
                return null;

            return CustomTriggerPopup(rect, label, sourceTrigger, sourceTrigger.Triggers.FirstOrDefault(t => t.TriggerType == VRC.SDKBase.VRC_Trigger.TriggerType.Custom && t.Name == selectedValue), style);
        }

        private static void InternalSerializedCustomTriggerPopup(SerializedProperty triggersProperty, SerializedProperty customProperty, System.Func<int, string[], int> show, System.Action fail)
        {
            if (customProperty == null || (customProperty.propertyType != SerializedPropertyType.String))
                throw new ArgumentException("Expected a string for customProperty");
            if (triggersProperty == null || (!triggersProperty.isArray && triggersProperty.propertyType != SerializedPropertyType.ObjectReference))
                throw new ArgumentException("Expected an object or array for triggersProperty");

            List<String> customNames = new List<string>();
            bool allNull = true;

            if (triggersProperty.isArray)
            {
                int idx;
                for (idx = 0; idx < triggersProperty.arraySize; ++idx)
                {
                    GameObject obj = triggersProperty.GetArrayElementAtIndex(idx).objectReferenceValue as GameObject;
                    if (obj != null)
                    {
                        customNames = obj.GetComponent<VRC.SDKBase.VRC_Trigger>().Triggers.Where(t => t.TriggerType == VRC.SDKBase.VRC_Trigger.TriggerType.Custom).Select(t => t.Name).ToList();
                        allNull = false;
                        break;
                    }
                }
                for (; idx < triggersProperty.arraySize; ++idx)
                {
                    GameObject obj = triggersProperty.GetArrayElementAtIndex(idx).objectReferenceValue as GameObject;
                    if (obj != null)
                    {
                        List<string> thisCustomNames = obj.GetComponent<VRC.SDKBase.VRC_Trigger>().Triggers.Where(t => t.TriggerType == VRC.SDKBase.VRC_Trigger.TriggerType.Custom).Select(t => t.Name).ToList();
                        customNames.RemoveAll(s => thisCustomNames.Contains(s) == false);
                    }
                }
            }
            else
            {
                GameObject obj = triggersProperty.objectReferenceValue as GameObject;
                if (obj != null)
                {
                    allNull = false;
                    customNames = obj.GetComponent<VRC.SDKBase.VRC_Trigger>().Triggers.Where(t => t.TriggerType == VRC.SDKBase.VRC_Trigger.TriggerType.Custom).Select(t => t.Name).ToList();
                }
            }

            if (customNames.Count == 0 && !allNull && triggersProperty.isArray)
            {
                fail();
                customProperty.stringValue = "";
            }
            else
            {
                if (customNames.Count == 0)
                    customNames.Add("");

                int selectedIdx = Math.Max(0, customNames.Count - 1);
                if (!string.IsNullOrEmpty(customProperty.stringValue))
                    for (; selectedIdx > 0; --selectedIdx)
                        if (customNames[selectedIdx] == customProperty.stringValue)
                            break;

                selectedIdx = show(selectedIdx, customNames.ToArray());
                customProperty.stringValue = customNames[selectedIdx];
            }
        }

        public static void CustomTriggerPopup(string label, SerializedProperty triggersProperty, SerializedProperty customProperty, params GUILayoutOption[] options)
        {
            InternalSerializedCustomTriggerPopup(triggersProperty, customProperty, (idx, names) => EditorGUILayout.Popup(label, idx, names, options), () => EditorGUILayout.HelpBox("Receivers do not have Custom Triggers which share names.", MessageType.Warning));
        }

        public static void CustomTriggerPopup(SerializedProperty triggersProperty, SerializedProperty customProperty, params GUILayoutOption[] options)
        {
            InternalSerializedCustomTriggerPopup(triggersProperty, customProperty, (idx, names) => EditorGUILayout.Popup(idx, names, options), () => EditorGUILayout.HelpBox("Receivers do not have Custom Triggers which share names.", MessageType.Warning));
        }

        public static void CustomTriggerPopup(Rect rect, SerializedProperty triggersProperty, SerializedProperty customProperty, GUIStyle style = null)
        {
            InternalSerializedCustomTriggerPopup(triggersProperty, customProperty, (idx, names) => style == null ? EditorGUI.Popup(rect, idx, names) : EditorGUI.Popup(rect, idx, names, style), () => EditorGUI.HelpBox(rect, "Receivers do not have Custom Triggers which share names.", MessageType.Warning));
        }

        public static void CustomTriggerPopup(Rect rect, string label, SerializedProperty triggersProperty, SerializedProperty customProperty, GUIStyle style = null)
        {
            InternalSerializedCustomTriggerPopup(triggersProperty, customProperty, (idx, names) => style == null ? EditorGUI.Popup(rect, label, idx, names) : EditorGUI.Popup(rect, label, idx, names, style), () => EditorGUI.HelpBox(rect, "Receivers do not have Custom Triggers which share names.", MessageType.Warning));
        }

        public static void DrawTriggerActionCallback(string actionLabel, VRC.SDKBase.VRC_Trigger trigger, VRC.SDKBase.VRC_EventHandler.VrcEvent e)
        {
            VRC.SDKBase.VRC_Trigger.TriggerEvent triggerEvent = VRC_EditorTools.CustomTriggerPopup(actionLabel, trigger, e.ParameterString);
            e.ParameterString = triggerEvent == null ? null : triggerEvent.Name;
        }

        public static Dictionary<string, List<MethodInfo>> GetSharedAccessibleMethodsOnGameObjects(SerializedProperty objectsProperty)
        {
            Dictionary<string, List<MethodInfo>> methods = new Dictionary<string, List<MethodInfo>>();

            int idx = 0;
            for (; idx < objectsProperty.arraySize; ++idx)
            {
                SerializedProperty prop = objectsProperty.GetArrayElementAtIndex(idx);
                GameObject obj = prop.objectReferenceValue != null ? prop.objectReferenceValue as GameObject : null;
                if (obj != null)
                {
                    methods = VRC_EditorTools.GetAccessibleMethodsOnGameObject(obj);
                    break;
                }
            }
            List<string> toRemove = new List<string>();
            for (; idx < objectsProperty.arraySize; ++idx)
            {
                SerializedProperty prop = objectsProperty.GetArrayElementAtIndex(idx);
                GameObject obj = prop.objectReferenceValue != null ? prop.objectReferenceValue as GameObject : null;
                if (obj != null)
                {
                    Dictionary<string, List<MethodInfo>> thisObjMethods = VRC_EditorTools.GetAccessibleMethodsOnGameObject(obj);
                    foreach (string className in methods.Keys.Where(s => thisObjMethods.Keys.Contains(s) == false))
                        toRemove.Add(className);
                }
            }

            foreach (string className in toRemove)
                methods.Remove(className);

            return methods;
        }

        public static Dictionary<string, List<MethodInfo>> GetAccessibleMethodsOnGameObject(GameObject go)
        {
            Dictionary<string, List<MethodInfo>> methods = new Dictionary<string, List<MethodInfo>>();
            if (go == null)
                return methods;

            Component[] cs = go.GetComponents<Component>();
            if (cs == null)
                return methods;

            foreach (Component c in cs.Where(co => co != null))
            {
                Type t = c.GetType();

                if (methods.ContainsKey(t.Name))
                    continue;

                // if component is the eventhandler
                if (t == typeof(VRC.SDKBase.VRC_EventHandler))
                    continue;

                List<MethodInfo> l = GetAccessibleMethodsForClass(t);
                methods.Add(t.Name, l);
            }

            return methods;
        }

        public static List<MethodInfo> GetAccessibleMethodsForClass(Type t)
        {
            // Get the public methods.
            MethodInfo[] myArrayMethodInfo = t.GetMethods(BindingFlags.Public | BindingFlags.Instance);
            List<MethodInfo> methods = new List<MethodInfo>();

            // if component is in UnityEngine namespace, skip it
            if (!string.IsNullOrEmpty(t.Namespace) && (t.Namespace.Contains("UnityEngine") || t.Namespace.Contains("UnityEditor")))
                return methods;

            bool isVRCSDK2 = !string.IsNullOrEmpty(t.Namespace) && t.Namespace.Contains("VRCSDK2");

            // Display information for all methods.
            for (int i = 0; i < myArrayMethodInfo.Length; i++)
            {
                MethodInfo myMethodInfo = (MethodInfo)myArrayMethodInfo[i];

                // if it's VRCSDK2, require RPC
                if (isVRCSDK2 && myMethodInfo.GetCustomAttributes(typeof(VRC.SDKBase.RPC), true).Length == 0)
                    continue;

                methods.Add(myMethodInfo);
            }
            return methods;
        }

        public static byte[] ReadBytesFromProperty(SerializedProperty property)
        {
            byte[] bytes = new byte[property.arraySize];
            for (int idx = 0; idx < property.arraySize; ++idx)
                bytes[idx] = (byte)property.GetArrayElementAtIndex(idx).intValue;
            return bytes;
        }

        public static void WriteBytesToProperty(SerializedProperty property, byte[] bytes)
        {
            property.arraySize = bytes != null ? bytes.Length : 0;
            for (int idx = 0; idx < property.arraySize; ++idx)
                property.GetArrayElementAtIndex(idx).intValue = (int)bytes[idx];
        }
#if !VRC_CLIENT
        internal static string CropImage(string sourcePath, float width, float height, bool centerCrop = true, bool forceLinear = false, bool forceGamma = false)
        {
            var bytes = File.ReadAllBytes(sourcePath);
            var tex = new Texture2D(2, 2)
            {
                wrapMode = TextureWrapMode.Clamp
            };
            tex.LoadImage(bytes);
            var blitShader = Resources.Load<Shader>("CropShader");
            var blitMat = new Material(blitShader);
            blitMat.SetFloat("_TargetWidth", width);
            blitMat.SetFloat("_TargetHeight", height);
            blitMat.SetInt("_CenterCrop", centerCrop ? 1 : 0);
            if (forceLinear)
            {
                blitMat.SetInt("_ForceLinear", 1);
            }
            else if (forceGamma)
            {
                blitMat.SetInt("_ForceGamma", 1);
            }
            var rt = RenderTexture.GetTemporary((int)width, (int)height);
            Graphics.Blit(tex, rt, blitMat);
            RenderTexture.active = rt;
            var cropped = new Texture2D((int)width, (int)height);
            cropped.ReadPixels(new Rect(0, 0, (int)width, (int)height), 0, 0);
            cropped.Apply();
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);
            Object.DestroyImmediate(blitMat);
            return SaveTemporaryTexture("cropped", cropped);
        }

        internal static Camera CreateThumbnailCaptureCamera(RenderTexture target, bool useFlatColor, Color clearColor, bool usePostProcessing)
        {
            var camObject = new GameObject("TempCamera");
            var cam = camObject.AddComponent<Camera>();
            cam.enabled = false;
            if (SceneView.lastActiveSceneView == null)
            {
                Core.Logger.LogError("Failed to get scene view");
                return null;
            }
            var sceneCam = SceneView.lastActiveSceneView.camera;
            if (sceneCam == null)
            {
                Core.Logger.LogError("Failed to get scene camera");
                return null;
            }

            cam.nearClipPlane = sceneCam.nearClipPlane;
            cam.farClipPlane = sceneCam.farClipPlane;
            camObject.transform.SetPositionAndRotation(sceneCam.transform.position, sceneCam.transform.rotation);

            if (useFlatColor)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = clearColor;                
            }

#if POST_PROCESSING_INCLUDED
            if (usePostProcessing)
            {
                var ppLayer = camObject.AddComponent<PostProcessLayer>();
                ppLayer.volumeLayer = int.MaxValue;
                ppLayer.volumeTrigger = camObject.transform;
            }
#endif
            cam.targetTexture = target;
            return cam;
        }
        
        internal static string CaptureSceneImage(float width, float height, bool useFlatColor, Color clearColor, bool usePostProcessing, Camera customCamera = null)
        {
            var rt = Resources.Load<RenderTexture>("ThumbnailCapture");
            if (customCamera != null)
            {
                customCamera.targetTexture = rt;
                var backgroundColor = customCamera.backgroundColor;
                backgroundColor.a = 1;
                customCamera.backgroundColor = backgroundColor;
            }
            var camera = customCamera != null ? customCamera : CreateThumbnailCaptureCamera(rt, useFlatColor, clearColor, usePostProcessing);
            camera.Render();
            
            var blitShader = Resources.Load<Shader>("CropShader");
            var blitMat = new Material(blitShader);
            blitMat.SetFloat("_TargetWidth", width);
            blitMat.SetFloat("_TargetHeight", height);
            blitMat.SetInt("_CenterCrop", 1);
            var buffer = RenderTexture.GetTemporary((int)width, (int)height);
            
            Graphics.Blit(rt, buffer, blitMat);
            RenderTexture.active = buffer;
            var tex = new Texture2D((int)width, (int)height);
            tex.ReadPixels(new Rect(0, 0, (int)width, (int)height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(buffer);
            Object.DestroyImmediate(blitMat);
            
            var tempPath = SaveTemporaryTexture("captured", tex);
            if (camera != customCamera)
            {
                Object.DestroyImmediate(camera.gameObject);
            }
            return tempPath;
        }
        
        internal static string SaveTemporaryTexture(string postfix, Texture2D tex)
        {
            var tempPath = Path.GetTempPath();
            tempPath = Path.Combine(tempPath, GUID.Generate() + "_" + postfix + ".png");
            File.WriteAllBytes(tempPath, tex.EncodeToPNG());
            return tempPath;
        }
        
        internal static MethodInfo GetSetPanelIdleMethod()
        {
            return typeof(VRCSdkControlPanel).GetMethod("SetPanelIdle", BindingFlags.Instance | BindingFlags.NonPublic);
        } 
        
        internal static MethodInfo GetSetPanelBuildingMethod()
        {
            return typeof(VRCSdkControlPanel).GetMethod("SetPanelBuilding", BindingFlags.Instance | BindingFlags.NonPublic);
        } 
        
        internal static MethodInfo GetSetPanelUploadingMethod()
        {
            return typeof(VRCSdkControlPanel).GetMethod("SetPanelUploading", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        internal static MethodInfo GetCheckProjectSetupMethod()
        {
            return typeof(VRCSdkControlPanel).GetMethod("CheckProjectSetup", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        internal static void ToggleSdkTabsEnabled(VRCSdkControlPanel panel, bool value)
        {
            var tabsProp =
                typeof(VRCSdkControlPanel).GetProperty("TabsEnabled", BindingFlags.Instance | BindingFlags.NonPublic);
            tabsProp?.SetValue(panel, value);
        }
        
        internal static bool IsBuildTargetSupported(BuildTarget target)
        {
            try
            {
                var moduleManager = Type.GetType("UnityEditor.Modules.ModuleManager,UnityEditor.dll");
                var isPlatformSupportLoaded = moduleManager?.GetMethod("IsPlatformSupportLoaded",
                    BindingFlags.Static | BindingFlags.NonPublic);
                var getTargetStringFromBuildTarget = moduleManager?.GetMethod("GetTargetStringFromBuildTarget",
                    BindingFlags.Static | BindingFlags.NonPublic);

                var targetString = (string) getTargetStringFromBuildTarget?.Invoke(null, new object[] {target});
                if (string.IsNullOrWhiteSpace(targetString))
                {
                    throw new Exception("Failed to get string for build target");    
                }

                return (bool) (isPlatformSupportLoaded?.Invoke(null, new object[] {targetString}) ?? false);
            }
            catch (Exception e)
            {
                Core.Logger.LogError($"Failed to check build target support for {target}, assume not installed: {e.Message}");
                return false;
            }
        }
        
        internal static void OpenProgressWindow()
        {
            Progress.ShowDetails();
        }
        
        internal static void OpenConsoleWindow()
        {
            EditorApplication.ExecuteMenuItem("Window/General/Console");
        }
        
        internal static List<string> GetBuildTargetOptions()
        {
            var options = new List<string>
            {
                "Windows",
                "Android"
            };
            if (ApiUserPlatforms.CurrentUserPlatforms?.SupportsiOS == true)
            {
                options.Add("iOS");
            }

            return options;
        }
        
        internal static List<BuildTarget> GetBuildTargetOptionsAsEnum()
        {
            var options = new List<BuildTarget>
            {
                BuildTarget.StandaloneWindows64,
                BuildTarget.Android
            };
            if (ApiUserPlatforms.CurrentUserPlatforms?.SupportsiOS == true)
            {
                options.Add(BuildTarget.iOS);
            }

            return options;
        }

        internal static string GetCurrentBuildTarget()
        {
            return GetTargetName(GetCurrentBuildTargetEnum());
        }

        internal static BuildTarget GetCurrentBuildTargetEnum()
        {
            return EditorUserBuildSettings.activeBuildTarget;
        }

        internal static string GetTargetName(BuildTarget target)
        {
            string currentTarget;
            switch (target)
            {
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    currentTarget = "Windows";
                    break;
                case BuildTarget.Android:
                    currentTarget = "Android";
                    break;
                case BuildTarget.iOS:
                    currentTarget = "iOS";
                    break;
                default:
                    currentTarget = "Unsupported Target Platform";
                    break;
            }

            return currentTarget;
        }

        internal static BuildTargetGroup GetBuildTargetGroupForTarget(BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    return BuildTargetGroup.Standalone;
                case BuildTarget.Android:
                    return BuildTargetGroup.Android;
                case BuildTarget.iOS:
                    return BuildTargetGroup.iOS;
                default:
                    return BuildTargetGroup.Unknown;
            }
        }

        internal static bool DryRunState
        {
            get => SessionState.GetBool("VRC.SDKBase.DryRun", false);
            set => SessionState.SetBool("VRC.SDKBase.DryRun", value);
        }
#endif
    }
}
#endif