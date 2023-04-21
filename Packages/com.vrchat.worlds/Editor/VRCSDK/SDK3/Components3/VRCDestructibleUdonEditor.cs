/*
#if VRC_SDK_VRCSDK3

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System;

[CustomEditor(typeof(VRC.SDK3.Components.VRCDestructibleUdon))]
public class VRCDestructibleUdonEditor : Editor
{
	VRC.SDK3.Components.VRCDestructibleUdon myTarget;

	void OnEnable()
	{
		if (myTarget == null)
			myTarget = (VRC.SDK3.Components.VRCDestructibleUdon)target;
	}

	string[] UdonMethods = null;
	string[] UdonVariables = null;

	public override void OnInspectorGUI()
	{
		var udon = myTarget.GetComponent<VRC.Udon.UdonBehaviour>();
		if (udon != null)
		{
			#if VRC_CLIENT
				myTarget.UdonMethodApplyDamage = EditorGUILayout.TextField("On Apply Damage", myTarget.UdonMethodApplyDamage);
				myTarget.UdonMethodApplyHealing= EditorGUILayout.TextField("On Apply Healing", myTarget.UdonMethodApplyHealing);
				myTarget.UdonVariableCurrentHealth= EditorGUILayout.TextField("Current Health Variable", myTarget.UdonVariableCurrentHealth);
				myTarget.UdonVariableMaxHealth = EditorGUILayout.TextField("On Max Health Variable", myTarget.UdonVariableMaxHealth);
			#else
				List<string> methods = new List<string>(udon.GetPrograms());
				methods.Insert(0, "-none-");
				myTarget.UdonMethodApplyDamage = DrawUdonProgramPicker("On Apply Damage", myTarget.UdonMethodApplyDamage, methods);
				myTarget.UdonMethodApplyHealing = DrawUdonProgramPicker("On Apply Healing", myTarget.UdonMethodApplyHealing, methods);
				List<string> variables = new List<string>(udon.publicVariables.VariableSymbols);
				variables.Insert(0, "-none-");
				myTarget.UdonVariableCurrentHealth = DrawUdonProgramPicker("Current Health Variable", myTarget.UdonVariableCurrentHealth, variables);
				myTarget.UdonVariableMaxHealth = DrawUdonProgramPicker("Max Health Variable", myTarget.UdonVariableMaxHealth, variables);
			#endif
		}
	}

	string DrawUdonProgramPicker(string title, string current, List<string> choices)
	{
		int index = choices.IndexOf(current);
		if (index == -1)
			index = 0;
		int value = EditorGUILayout.Popup(title, index, choices.ToArray());
		if (value != 0)
			return choices[value];
		return current;
	}
}
#endif
*/