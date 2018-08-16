#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace AM.Inspector{
	[CustomEditor(typeof(AniMeshAnimator))]
	public class AniMeshAnimator_Inspector : Editor {
		public override void OnInspectorGUI ()
		{
			AniMeshAnimator Animator = (AniMeshAnimator)target;

			EditorGUILayout.Space ();
			EditorGUILayout.LabelField ("Animator Settings", EditorStyles.boldLabel);
			Animator.Data = EditorGUILayout.ObjectField (new GUIContent("Animation Data","Insert an AniMeshData file here. This will provide the Animator with animation data."),Animator.Data, typeof(AniMeshData),true) as AniMeshData;
			Animator.AnimatorSpeed = EditorGUILayout.Slider (new GUIContent("Animator Speed","Animator Speed determines the overal speed of the animator. This speed will apply to all animations played by this animator."),Animator.AnimatorSpeed, 0, 20);
		}
	}
}
#endif
