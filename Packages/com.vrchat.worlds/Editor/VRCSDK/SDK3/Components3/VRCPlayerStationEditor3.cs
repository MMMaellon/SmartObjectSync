#if VRC_SDK_VRCSDK3 && UNITY_EDITOR

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System;
using UnityEngine.UIElements;
using VRCStation = VRC.SDK3.Components.VRCStation;

namespace VRC.SDK3.Editor
{
	[CanEditMultipleObjects]
	[CustomEditor(typeof(VRCStation))]
	public class VRCPlayerStationEditor3 : VRCInspectorBase
	{
		
		private SerializedProperty propPlayerMobility;
		private SerializedProperty propCanUseStationFromStation;
		private SerializedProperty propAnimatorController;
		private SerializedProperty propDisableStationExit;
		private SerializedProperty propSeated;
		private SerializedProperty propStationEnterPlayerLocation;
		private SerializedProperty propStationExitPlayerLocation;
		
		
		private void OnEnable()
		{
			propPlayerMobility = serializedObject.FindProperty(nameof(VRCStation.PlayerMobility));
			propCanUseStationFromStation = serializedObject.FindProperty(nameof(VRCStation.canUseStationFromStation));
			propAnimatorController = serializedObject.FindProperty(nameof(VRCStation.animatorController));
			propDisableStationExit = serializedObject.FindProperty(nameof(VRCStation.disableStationExit));
			propSeated = serializedObject.FindProperty(nameof(VRCStation.seated));
			propStationEnterPlayerLocation = serializedObject.FindProperty(nameof(VRCStation.stationEnterPlayerLocation));
			propStationExitPlayerLocation = serializedObject.FindProperty(nameof(VRCStation.stationExitPlayerLocation));
		}

		public override void BuildInspectorGUI()
		{
			base.BuildInspectorGUI();
			
			AddField(propPlayerMobility);
			AddField(propSeated);
			AddField(propDisableStationExit);
			AddField(propCanUseStationFromStation);
			AddField(propStationEnterPlayerLocation);
			AddField(propStationExitPlayerLocation);
			AddField(propAnimatorController);
		}
	}
}
#endif