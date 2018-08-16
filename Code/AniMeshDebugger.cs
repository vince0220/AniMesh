using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AM;

namespace AM.Debug{
	[RequireComponent(typeof(AniMeshAnimator))]
	public class AniMeshDebugger : MonoBehaviour {
		#region Public variables
		public bool PlayOnAwake = false;
		public int PlayAnimation = 0;
		public float FadeDuration = 1f;
		public KeyCode[] KeyBindings;
		#endregion

		#region Private variables
		private AniMeshAnimator _Animator;
		#endregion

		#region Base Voids
		void Start(){
			if (PlayOnAwake) {
				Animator.PlayClip (PlayAnimation);
			}
		}
		void Update () {
			Animator.UpdateAnimator ();
			if (KeyBindings != null) {
				for (int i = 0; i < KeyBindings.Length; i++) {
					if (Input.GetKeyDown (KeyBindings [i])) {
						Animator.CrossFade (i,FadeDuration);
						break;
					}
				}
			}
		}
		#endregion

		#region Get / Set
		public AniMeshAnimator Animator{
			get{
				if (_Animator == null) {
					_Animator = GetComponent<AniMeshAnimator> ();
				}
				return _Animator;
			}
		}
		#endregion
	}
}
