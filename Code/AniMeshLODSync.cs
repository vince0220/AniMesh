using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AM{
	public class AniMeshLODSync : MonoBehaviour {
		#region Private variables
		private AniMeshAnimator _Animator;
		#endregion

		#region Base voids
		private void OnBecameVisible(){
			Animator.UpdateAnimator (); // update animator
		}
		#endregion

		#region Get / Set
		private AniMeshAnimator Animator{
			get{
				if (_Animator == null) {
					_Animator = this.GetComponentInParent<AniMeshAnimator> ();
				}
				return _Animator;
			}
		}
		#endregion
	}
}
