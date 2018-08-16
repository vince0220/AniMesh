using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AM{
	public class AniMeshAnimator : MonoBehaviour {
		#region Inspector Variables
		[Header("Animator Settings")]
		[Tooltip("Insert a AniMesh data file here")]
		public AniMeshData Data;
		[Tooltip("This value determines the speed of the overal animator")]
		[SerializeField]private float _AnimatorSpeed = 1f;
		#endregion

		#region private variables
		// components
		private MaterialPropertyBlock[] _PropertyBlocks;
		private Renderer[] _Renderers;
		private AnimationTracker[] _Trackers = new AnimationTracker[2];
		#endregion

		#region Public voids
		public void UpdateAnimator(){
			if (CurrentClipIndex >= 0 && Data != null) { // if there is a current clip
				for (int i = 0; i < _Trackers.Length; i++) {
					if (_Trackers [i] != null) {
						_Trackers [i].UpdateTracker (Renderers,PropertyBlocks, Data,i,AnimatorSpeed);
					}
				}
			}
		}
		public void PlayClip(int Index){
			CrossFade (Index,0f);
		}
		public void CrossFade(string ClipTitle, float FadeLength,bool RandomStart = false,System.Action OnDone = null){
			CrossFade (Data.GetClipIndexByName(ClipTitle),0f,RandomStart,OnDone);
		}
		public void CrossFade(int Index, float FadeLength,bool RandomStart = false,System.Action OnDone = null){
			if (Data != null) {
				if ((CurrentClipIndex < 0 || _Trackers[CurrentClipIndex]._Clip != Data.Clips[Index]) && Index < Data.Clips.Length) { // if isnt already running
					int CurrentClip = CurrentClipIndex;
					for (int i = 0; i < _Trackers.Length; i++) {
						if (_Trackers [i] != null) {
							_Trackers [i].SetFade (FadeLength, 0,OnDone);
						}
					} // set to fade out

					// set new tracker
					for (int i = 0; i < _Trackers.Length; i++) {
						if (i != CurrentClip) {
							AnimationTracker NewTracker = FindTrackerByClip (Data.Clips [Index]);
							if (NewTracker == null) {NewTracker = new AnimationTracker (Data.Clips [Index], Data);}
							_Trackers [i] = NewTracker;
							_Trackers [i].ResetFrames ();
							_Trackers [i].SetFade (FadeLength, 1,OnDone);
							if (RandomStart) {_Trackers [i].ResetRandomRange ();}
							break;
						}
					}
				}
			}
		}
		#endregion

		#region Private voids
		private void InitializeLODSync(){
			for (int i = 0; i < _Renderers.Length; i++) {
				AniMeshLODSync LODSync = _Renderers [i].GetComponent<AniMeshLODSync> ();
				if (LODSync == null) {
					LODSync = _Renderers [i].gameObject.AddComponent<AniMeshLODSync> ();
				}
			}
		}
		#endregion

		#region Get / Set
		// publics
		public int ClipCount{
			get{
				return Data.Clips.Length;
			}
		}
		public float AnimatorSpeed{
			get{
				return _AnimatorSpeed;
			}
			set{
				_AnimatorSpeed = value;
			}
		}
		public string CurrentClipTitle{
			get{
				int CurrentIndex = CurrentClipIndex;
				if (CurrentIndex >= 0) {
					return Data.Clips[CurrentIndex].Title;
				}
				return "";
			}
		}
		public int CurrentClipIndex{
			get{
				if (CurrentTracker == null) {return -1;}

				for (int i = 0; i < _Trackers.Length; i++) {
					if (CurrentTracker == _Trackers[i]) {
						return i;
					}
				}
				return -1;
			}
		}

		// privates
		private MaterialPropertyBlock[] PropertyBlocks{
			get{
				if (_PropertyBlocks == null) {
					_PropertyBlocks = new MaterialPropertyBlock[Renderers.Length];
					for (int i = 0; i < _PropertyBlocks.Length; i++) {
						MaterialPropertyBlock Block = new MaterialPropertyBlock ();
						Renderers [i].GetPropertyBlock (Block);
						_PropertyBlocks [i] = Block;
					}
				}
				return _PropertyBlocks;
			}
		}
		private Renderer[] Renderers{
			get{
				if (_Renderers == null) {
					_Renderers = this.GetComponentsInChildren<Renderer> ();
					InitializeLODSync ();
				}
				return _Renderers;
			}
		}
		private AnimationTracker CurrentTracker{
			get{
				AnimationTracker Tracker = null;
				for (int i = 0; i < _Trackers.Length; i++) {
					if (Tracker == null || (Tracker != null && _Trackers[i] != null && Tracker._TargetStrength < _Trackers [i]._TargetStrength)) {
						Tracker = _Trackers [i];
					} 
				}
				return Tracker;
			}
		}

		private AnimationTracker FindTrackerByClip(AniMeshData.AniClip Clip){
			for (int i = 0; i < _Trackers.Length; i++) {
				if (_Trackers [i] != null && _Trackers[i]._Clip == Clip) {
					return _Trackers [i];
				}
			}
			return null;
		}
		#endregion

		#region Inner class
		private class AnimationTracker{
			// publics
			public float Strength = 0f;

			// privates
			private float _FadeStartTime;
			private float _CurrentFrame = 0;
			private float _FadeLength;
			public float _TargetStrength;
			private float _From = 0;
			private AniMeshData _Data;
			private float _ClipMin;
			private float _ClipMax;
			private System.Action OnDoneFade;
			private string[] _PropertyKeys = new string[]{
				"_AnimationData1",
				"_AnimationData2",
			};

			public AniMeshData.AniClip _Clip;

			public AnimationTracker(AniMeshData.AniClip Clip,AniMeshData Data){
				_Clip = Clip;
				_Data = Data;
				_ClipMin = ((float)(_Clip.StartFrame+1) / (float)_Data.FrameCount);
				_ClipMax = ((float)_Clip.StartFrame + (float)_Clip.Length) / (float)_Data.FrameCount;
			}


			#region Inputs
			public void UpdateTracker(Renderer[] Renderers,MaterialPropertyBlock[] Blocks, AniMeshData Data, int Index, float AnimatorSpeed){
				// update frame
				_CurrentFrame = Mathf.Clamp(_CurrentFrame + ((_Clip.FrameRate * Time.deltaTime) * _Clip.Speed) * AnimatorSpeed,0,_Clip.Length);
				if (_CurrentFrame >= _Clip.Length && _Clip.Loop) {
					_CurrentFrame = 0;
				}

				// update strength
				Strength = Lerp(_From,_TargetStrength,FadePercentage()); // lerp to target strength

				// on done fading callback
				if (OnDoneFade != null && FadePercentage() >= 1f) {
					OnDoneFade.Invoke ();
					OnDoneFade = null;
				}

				if (Strength <= 0 && !_Clip.Loop) {
					ResetFrames ();
				}

				// update renderers
				for (int i = 0; i < Blocks.Length; i++) {
					if (Renderers [i].isVisible) {
						MaterialPropertyBlock props = Blocks [i];
						Vector4 AnimationData = new Vector4 (
							Lerp (ClipMin, ClipMax, (float)_CurrentFrame / (float)_Clip.Length),
							Strength,
							_Clip.MagnitudeMultiply,
							0f
						);
						props.SetVector (_PropertyKeys [Index], AnimationData);
						Renderers [i].SetPropertyBlock (props);
					}
				}
			}
			public void SetFade(float FadeLength, float Target,System.Action OnDoneFade = null){
				float Diff = Mathf.Abs (Target - Strength);
				_FadeLength = FadeLength * Diff;
				_FadeStartTime = Time.time;
				_TargetStrength = Target;
				_From = Strength;
				this.OnDoneFade = OnDoneFade;
			}
			public void ResetFrames(){
				_CurrentFrame = 0;
			}
			public void ResetRandomRange(){
				_CurrentFrame = Random.Range (0,_Clip.Length);
			}
			public float Lerp(float a, float b, float f){
				return a + f * (b - a);
			}
			#endregion

			#region Get / Set
			public float FadePercentage(){
				float val = Mathf.Clamp((Time.time - _FadeStartTime) / _FadeLength,0,1);
				if (float.IsNaN(val)) {
					val = 0f;
				}
				return val;
			}
			public float ClipMin{
				get{
					return _ClipMin; // calculate clip relative min
				}
			}
			public float ClipMax{
				get{
					return _ClipMax; // calculate clip relative max
				}
			}
			#endregion
		}
		#endregion
	}
}
