
using UnityEngine.UIElements;
using VRC.SDKBase.Editor;

// Common interface used by all builder panels inside their respective packages

/// <summary>
/// This interface is reserved for SDK use, refer to Interfaces inside the Public SDK API folder for public APIs
/// </summary>
public interface IVRCSdkControlPanelBuilder
{
    void ShowSettingsOptions();
    bool IsValidBuilder(out string message);
    void CreateBuilderErrorGUI(VisualElement root);
    void ShowBuilder();
    void RegisterBuilder(VRCSdkControlPanel baseBuilder);
    void SelectAllComponents();
    void CreateContentInfoGUI(VisualElement root);
    void CreateBuildGUI(VisualElement root);
}
