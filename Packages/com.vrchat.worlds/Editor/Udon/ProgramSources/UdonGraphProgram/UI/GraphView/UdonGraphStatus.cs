#if UNITY_2019_3_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#else
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#endif
using UnityEditor;
using UnityEngine;


namespace VRC.Udon.Editor.ProgramSources.UdonGraphProgram.UI.GraphView
{
    public class UdonGraphStatus : ToolbarToggle
    {
        private VisualElement _detailsContainer;
        private UdonProgramSourceView _programSourceView;
        private UdonGraphProgramAsset _graphAsset;
        private TextElement _label;

        public UdonGraphStatus(VisualElement detailsContainer)
        {
            _detailsContainer = detailsContainer;

            _label = new TextElement() {text = "-", name = "Content"};
            _label.StretchToParentSize();
            Add(_label);
#if UNITY_2019_3_OR_NEWER
            this.RegisterValueChangedCallback(ShowGraphAssetDetails);
#else
            OnValueChanged(ShowGraphAssetDetails);
#endif

            _programSourceView = new UdonProgramSourceView();
        }

        private void ShowGraphAssetDetails(ChangeEvent<bool> changeEvent)
        {
            // Just remove it
            if (!changeEvent.newValue)
            {
                RemoveIfContaining(_programSourceView);
                return;
            }

            // else we need to make a new one and show it
            if (_graphAsset == null)
            {
                Debug.LogError("Can't show Asset Details for a null asset.");
                return;
            }

            RemoveIfContaining(_programSourceView);
            _detailsContainer.Add(_programSourceView);
            _programSourceView.BringToFront();
        }

        private void RemoveIfContaining(VisualElement element)
        {
            if (_detailsContainer.Contains(element))
            {
                _detailsContainer.Remove(element);
            }
        }

        public void OnAssemble(bool success, string assembly)
        {
            if (!enabledInHierarchy || _label == null) return;

            string newText;
            Color flashColor;
            Color targetColor;

            // change visuals based on success
            if (success)
            {
                newText = "OK";
                flashColor = new Color(0, 1, 0);
                targetColor = new Color(0.1f, 0.25f, 0.1f, alpha);
            }
            else
            {
                newText = "!";
                flashColor = new Color(1, 0, 0);
                targetColor = new Color(0.25f, 0.1f, 0.1f, alpha);
            }

            // Update visuals
            _label.text = newText;
            _label.style.backgroundColor = flashColor;
            LerpColor(targetColor);
            _programSourceView.SetText(assembly);
        }

        private Color targetColor;
        private Color startColor;
        private float lerpAmount = 0f;
        private float alpha = 0.75f;

        private void LerpColor(Color newColor)
        {
            startColor = _label.style.backgroundColor.value;
            targetColor = newColor;
            lerpAmount = 0f;
            EditorApplication.update += UpdateColor;
        }

        private void UpdateColor()
        {
            lerpAmount += 0.01f;

            if (lerpAmount >= 1)
            {
                EditorApplication.update -= UpdateColor;
            }

            if (_label != null)
            {
                _label.style.backgroundColor = Color.Lerp(startColor, targetColor, lerpAmount);
            }
        }

        public override void Blur()
        {
            EditorApplication.update -= UpdateColor;
        }

        ~UdonGraphStatus()
        {
            EditorApplication.update -= UpdateColor;
            if (_graphAsset != null)
            {
                _graphAsset.OnAssemble -= OnAssemble;
            }
        }

        internal void LoadAsset(UdonGraphProgramAsset asset)
        {
            _graphAsset = asset;
            _graphAsset.OnAssemble += OnAssemble;
            _programSourceView.LoadAsset(asset);
        }
    }
}