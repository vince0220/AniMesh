#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using AM.Debug;

namespace AM.Inspector{
	[CustomEditor(typeof(AniMeshDebugger))]
	public class AniMeshDebugger_Inspector : Editor {
		#region Private variables
		private bool OpenKeyBindings = false;
		#endregion

		public override void OnInspectorGUI ()
		{
			AniMeshDebugger Debugger = (AniMeshDebugger)target;

			int BaseIndent = EditorGUI.indentLevel;

			EditorGUILayout.Space ();
			EditorGUILayout.LabelField ("Debugger Settings",EditorStyles.boldLabel);
			Debugger.FadeDuration = EditorGUILayout.Slider (new GUIContent("Fade Duration"),Debugger.FadeDuration,0,10);
			Debugger.PlayOnAwake = EditorGUILayout.Toggle (new GUIContent ("Play On Awake"), Debugger.PlayOnAwake);
			if (Debugger.PlayOnAwake) {
				EditorGUI.indentLevel = BaseIndent + 1;
				Debugger.PlayAnimation = EditorGUILayout.IntField(new GUIContent("Animation Index"),Debugger.PlayAnimation);
			}

			EditorGUI.indentLevel = BaseIndent;

			EditorGUILayout.Space ();
			EditorGUILayout.LabelField ("Key Bindings",EditorStyles.boldLabel);
			if (Debugger.KeyBindings == null || Debugger.KeyBindings.Length != Debugger.Animator.ClipCount) {
				Debugger.KeyBindings = new KeyCode[Debugger.Animator.ClipCount];
			}

			OpenKeyBindings = EditorGUILayout.Foldout (OpenKeyBindings, "Key Bindings");
			if (OpenKeyBindings) {
				EditorGUI.indentLevel = BaseIndent + 1;
				for (int i = 0; i < Debugger.KeyBindings.Length; i++) {
					Debugger.KeyBindings [i] = (KeyCode)EditorGUILayout.EnumPopup (new GUIContent(string.Format("({0} / {1}) - Key:",Debugger.Animator.Data.Clips[i].Title,i),string.Format("Select the key to bind to animation clip {0}. Pressing this key on runtime will cross fade to this animation clip.",i)),Debugger.KeyBindings [i]);
				}
			}
			EditorGUI.indentLevel = BaseIndent;
		}
	}
}
#endif
