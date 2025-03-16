
using System;
using UnityEngine;
using UnityEngine.UIElements;

// Common interface used by all builder panels inside their respective packages

/// <summary>
/// This interface is reserved for SDK use, refer to Interfaces inside the Public SDK API folder for public APIs
/// </summary>
public interface IVRCSdkControlPanelBuilder
{
    void Initialize();
    void ShowSettingsOptions();
    bool IsValidBuilder(out string message);
    void CreateBuilderErrorGUI(VisualElement root);
    
    [Obsolete("Legacy method, use CreateValidationsGUI instead", false)]
    void ShowBuilder();
    
    void CreateValidationsGUI(VisualElement root);
    
    EventHandler OnContentChanged { get; set; }
    EventHandler OnShouldRevalidate { get; set; }
    
    void RegisterBuilder(VRCSdkControlPanel baseBuilder);
    void SelectAllComponents();
    void CreateContentInfoGUI(VisualElement root);
    void CreateBuildGUI(VisualElement root);

    /// <summary>
    /// Returns the image to show within the Builder tab. If no image is provided - the default image is used
    /// </summary>
    /// <returns></returns>
    Texture2D GetHeaderImage()
    {
        return null;
    }
}
