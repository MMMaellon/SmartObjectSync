#if VRC_SDK_VRCSDK3

using UnityEditor;
using UnityEngine;

[CustomEditor (typeof(VRC.SDK3.Components.VRCSceneDescriptor))]
public class VRCSceneDescriptorEditor3 : Editor
{
    VRC.SDK3.Components.VRCSceneDescriptor sceneDescriptor;
    VRC.Core.PipelineManager pipelineManager;
    [System.NonSerialized] string[] layerNames = null;
    static GUIContent maskContent = new GUIContent("Interact Passthrough", "Enabled layers will allow UI interaction and picking up through colliders.");
    const int USER_LAYER_START = 22;
    const int USER_LAYER_COUNT = 10;
    int mask = 0;

    public override void OnInspectorGUI()
    {
        if(sceneDescriptor == null)
            sceneDescriptor = (VRC.SDK3.Components.VRCSceneDescriptor)target;

        if(pipelineManager == null)
        {
            pipelineManager = sceneDescriptor.GetComponent<VRC.Core.PipelineManager>();
            if(pipelineManager == null)
                sceneDescriptor.gameObject.AddComponent<VRC.Core.PipelineManager>();
        }

        if (sceneDescriptor.spawns == null || sceneDescriptor.spawns.Length == 0)
        {
            sceneDescriptor.spawns = new[] { sceneDescriptor.transform };
            Debug.LogWarning($"Scene Descriptor spawns were empty, adding a default Spawn.");
        }

        DrawDefaultInspector();

        if (layerNames == null)
            PopulateUserLayerNames();

        if (layerNames != null)
        {
            mask = EditorGUILayout.MaskField(maskContent, sceneDescriptor.interactThruLayers >> USER_LAYER_START, layerNames);
            sceneDescriptor.interactThruLayers = mask << USER_LAYER_START;
        }
    }

    void PopulateUserLayerNames()
    {
        if (layerNames == null)
            layerNames = new string[USER_LAYER_COUNT];

        for (int i = 0; i < USER_LAYER_COUNT; ++i)
        {
            string name = LayerMask.LayerToName(USER_LAYER_START + i);
            if (string.IsNullOrWhiteSpace(name))
                layerNames[i] = $"<<layer {USER_LAYER_START + i}>>"; 
            else
                layerNames[i] = name; 
        }
    }
}

#endif

