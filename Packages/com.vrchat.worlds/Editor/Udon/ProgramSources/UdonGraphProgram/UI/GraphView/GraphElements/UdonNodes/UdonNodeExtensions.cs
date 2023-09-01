using System;
using UnityEditor.Experimental.GraphView;
using EditorGV = UnityEditor.Experimental.GraphView;
using EngineUI = UnityEngine.UIElements;
using EditorUI = UnityEditor.UIElements;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using VRC.Udon.Common;
using VRC.Udon.Compiler.Compilers;

namespace VRC.Udon.Editor.ProgramSources.UdonGraphProgram.UI.GraphView.UdonNodes
{
    public static class UdonNodeExtensions
    {
        
        #region Methods for creating popup selectors from Program variables / event

        private static readonly HashSet<string> InternalEventNames = new HashSet<string>()
        {
            "_start", "_update", "_lateUpdate", "_fixedUpdate", "onAnimatorIk", "_onAnimatorMove", "_onAvatarChanged", "_onAvatarEyeHeightChanged", "_onBecameInvisible", "_onBecameVisible",
            "_onPlayerCollisionEnter", "_onCollisionEnter", "_onCollisionEnter2D", "_onPlayerCollisionExit", "_onCollisionExit", "_onCollisionExit2D", "_onPlayerCollisionStay", "_onCollisionStay", "_onCollisionStay2D",
            "_onPlayerTriggerEnter", "_onTriggerEnter", "_onTriggerEnter2D", "_onPlayerTriggerExit", "_onTriggerExit", "_onTriggerExit2D", "_onPlayerTriggerStay", "_onTriggerStay", "_onTriggerStay2D", 
            "_onDestroy", "_onDisable", "_onDrawGizmos", "_onDrawGizmosSelected", "_onEnable", "_onJointBreak", "_onJointBreak2D", "_onMouseDown", "_onMouseDrag", "_onMouseEnter", "_onMouseExit", "_onMouseOver", "_onMouseUp", "_onMouseUpAsButton",
            "_onPlayerParticleCollision", "_onParticleCollision", "_onParticleTrigger", "_onPostRender", "_onPreCull", "_onPreRender", "_onRenderImage", "_onRenderObject", "_onTransformChildrenChanged", "_onTransformParentChanged", "_onValidate", "_onWillRenderObject",
            "_interact", "_onDrop", "_onPickup", "_onPickupUseDown", "_onPickupUseUp", "_onPreSerialization", "_onPostSerialization", "_onDeserialization", "_onVideoEnd", "_onVideoPause", "_onVideoPlay", "_onVideoStart", "_midiNoteOn", "_midiNoteOff", "_midiControlChange",
            "_onOwnershipRequest", "_onNetworkReady", "_onOwnershipTransferred", "_onPlayerJoined", "_onPlayerLeft", "_onSpawn", "_onStationEntered", "_onStationExited",
        };
        
        public enum ProgramPopupType
        {
            Variables, Events
        }
        
        private static List<string> GetCustomEventsFromAsset(AbstractSerializedUdonProgramAsset asset)
        {
            // don't return internal event names or VariableChange events
            return asset.RetrieveProgram().EntryPoints.GetExportedSymbols().Where(e =>
                !InternalEventNames.Contains(e) && !e.StartsWithCached(VariableChangedEvent.EVENT_PREFIX)).ToList();
        }

        public static EditorUI.PopupField<string> GetProgramPopup(this UdonNode node, ProgramPopupType popupType, EditorUI.PopupField<string> eventNamePopup)
        {
            const string placeholder = "----";
            const string missing = "MISSING! Was";
            
            List<string> options = new List<string>{placeholder};
            var data = node.data;

            bool unavailable = true;
            if(data.nodeUIDs.Length < 1 || string.IsNullOrEmpty(data.nodeUIDs[0]))
            {
                switch (popupType)
                {
                    case ProgramPopupType.Events:
                        options =  GetCustomEventsFromAsset(node.Graph.graphProgramAsset.SerializedProgramAsset);
                        break;
                    case ProgramPopupType.Variables:
                        node.Graph.RefreshVariables(false);
                        options = new List<string>(node.Graph.VariableNames).Where(x=>!x.StartsWithCached(VariableChangedEvent.OLD_VALUE_PREFIX)).ToList();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(popupType), popupType, null);
                }
                unavailable = options.Count == 0;
                options.Insert(0, placeholder);
            }
            else if(data.InputNodeAtIndex(0)?.fullName == "Get_Variable")
            {
                // So much work to get the underlying node referenced by a variable. Would be nice to have a method for this.
                var parts = data.nodeUIDs[0].Split('|');
                if (parts.Length < 1) return null;

                string targetId = parts[0];

                var variableGetterNode = node.Graph.graphData.FindNode(targetId);
                if (variableGetterNode == null || variableGetterNode.nodeValues.Length < 1) return null;

                string variableId = variableGetterNode.nodeValues[0].Deserialize() as string;
                if (string.IsNullOrWhiteSpace(variableId)) return null;

                string variableName = node.Graph.GetVariableName(variableId);
                if (string.IsNullOrWhiteSpace(variableName)) return null;

                if (node.Graph.udonBehaviour != null && node.Graph.udonBehaviour.publicVariables.TryGetVariableValue(variableName, out UdonBehaviour ub))
                {
                    if (ub != null)
                    {
                        switch (popupType)
                        {
                            case ProgramPopupType.Events:
                                options = GetCustomEventsFromAsset(ub.programSource.SerializedProgramAsset);
                                break;
                            case ProgramPopupType.Variables:
                            {
                                if(ub.programSource == null){break;}
                                if(ub.programSource.SerializedProgramAsset == null){break;}
                                options = ub.programSource.SerializedProgramAsset.RetrieveProgram()?.SymbolTable
                                    .GetSymbols().Where(s =>
                                        !s.StartsWithCached(UdonGraphCompiler.INTERNAL_VARIABLE_PREFIX) &&
                                        !s.StartsWithCached(VariableChangedEvent.OLD_VALUE_PREFIX)).ToList();
                                break;
                            }
                        }

                        options?.Insert(0, placeholder);
                        unavailable = false;
                    }
                }
            }
           
            
            int currentIndex = 0;
            int targetNodeValueIndex = node.data.fullName.Contains("SendCustomNetworkEvent") ? 2 : 1;
            string targetVarName = data.nodeValues[targetNodeValueIndex].Deserialize() as string;
            if (targetVarName != null && targetVarName.StartsWithCached(missing)) targetVarName = null;
            
            // If we have a target variable name:
            if (!string.IsNullOrWhiteSpace(targetVarName) && options != null)
            {
                if (options.Contains(targetVarName))
                {
                    currentIndex = options.IndexOf(targetVarName);
                }
                else
                {
                    options[0] = unavailable ? targetVarName : $"{missing} {targetVarName}";
                }
            }

            if (eventNamePopup == null)
            {
                eventNamePopup = new EditorUI.PopupField<string>(options, currentIndex)
                {
                    name = popupType == ProgramPopupType.Events ? "EventNamePopup" : "VariablePopup"
                };
                var eventNamePort = node.inputContainer.Q(null,  popupType == ProgramPopupType.Events ? "eventName" : "symbolName");
                eventNamePort?.Add(eventNamePopup);
                if (unavailable)
                {
                    eventNamePopup.SetEnabled(false);
                }
            }
            else
            {
                // Remaking it - remove the old one, at the new one at its previous location
                int index = node.inputContainer.IndexOf(eventNamePopup);
                eventNamePopup.RemoveFromHierarchy();
                eventNamePopup = new EditorUI.PopupField<string>(options, currentIndex);
                node.inputContainer.Insert(index, eventNamePopup);
            }
            eventNamePopup.RegisterValueChangedCallback(
                e =>
                {
                    node.SetNewValue(string.Compare(e.newValue, placeholder, StringComparison.Ordinal) == 0 ? "" : e.newValue.ToString(), targetNodeValueIndex);
                    // Todo: update text field directly and save instead of calling Reload
                    node.Reload();
                });

            return eventNamePopup;
        }
        #endregion
    }
}