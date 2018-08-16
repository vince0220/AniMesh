using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.IO;

namespace AM{
	public class AniMeshData : ScriptableObject {
		#region Public variables
		[SerializeField]private AniClip[] _AniClips;
		#endregion

		#region Private variables
		[HideInInspector][SerializeField]private GameObject _FBX;
		[HideInInspector][SerializeField]private Mesh _BaseMesh;
		[HideInInspector][SerializeField]private AnimationClip[] _Clips;
		[HideInInspector][SerializeField]private Texture2D _AnimationTexture;
		[HideInInspector][SerializeField]private string _CreatedFolder;

		// private temp
		private Dictionary<string,int> _ClipIndexDictionary;
		#endregion

		#if UNITY_EDITOR
		#region Public inputs
		public bool Instantiate(string FBXPath){
			// Editor
			EditorUtility.DisplayProgressBar("Generating AniMesh","Loading FBX data",0.1f);

			// get base mesh
			_FBX = AssetDatabase.LoadAssetAtPath<GameObject> (FBXPath); // find fbx
			EditorUtility.DisplayProgressBar("Generating AniMesh","Loading Animation Clips",0.15f);
			_Clips = GetAnimationClips (FBXPath); // get clips
			EditorUtility.DisplayProgressBar("Generating AniMesh","Generating Base Mesh",0.30f);
			_BaseMesh = GenerateBase (); // generate base mesh
			EditorUtility.SetDirty (this); // set dirty
			EditorUtility.DisplayProgressBar("Generating AniMesh","Saving Prefab and Material",0.40f);
			GameObject Prefab = GeneratePrefab (GenerateMaterial());
			EditorUtility.DisplayProgressBar("Generating AniMesh","Generating Data texture",0.85f);

			_AnimationTexture = SaveTexture (GenerateAnimationTexture(),CreatedFolder,_FBX.name+"_Animation"); // save animation texture
				
			// set prefab settings
			Prefab.GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_AnimateTexture",_AnimationTexture);

			AssetDatabase.MoveAsset (AssetDatabase.GetAssetPath (this), CreatedFolder + FileName); // move self in folder
			AssetDatabase.SaveAssets (); // save assets

			EditorUtility.ClearProgressBar ();
			return true; // Initialized with succes
		}
		#endregion

		#region Private voids
		private Texture2D SaveTexture(Texture2D Texture,string SaveFolder,string Name){
			string Path = SaveFolder + Name + ".png";
			Path = AssetDatabase.GenerateUniqueAssetPath (Path);
			var Bytes = Texture.EncodeToPNG ();
			Vector2 Size = new Vector2 (Texture.width,Texture.height);
			System.IO.File.WriteAllBytes (Path, Bytes);
			AssetDatabase.ImportAsset (Path,ImportAssetOptions.ForceUpdate);

			// set import settings
			var Importer = (TextureImporter)AssetImporter.GetAtPath(Path);
			Importer.sRGBTexture = false;
			Importer.mipmapEnabled = false;
			Importer.filterMode = FilterMode.Point;
			Importer.npotScale = TextureImporterNPOTScale.None;
			Importer.textureCompression = TextureImporterCompression.Uncompressed;
			Importer.maxTextureSize = (int)Mathf.Max (Size.x,Size.y);
			Importer.wrapMode = TextureWrapMode.Clamp;
			Importer.SaveAndReimport (); // reimport

			// return new texture
			return AssetDatabase.LoadAssetAtPath<Texture2D> (Path);
		}
		private AnimationClip[] GetAnimationClips(string FBXPath){
			Object[] objects = AssetDatabase.LoadAllAssetsAtPath (FBXPath);
			List<AnimationClip> Clips = new List<AnimationClip> ();
			foreach (Object AssetObj in objects) {
				AnimationClip clip = AssetObj as AnimationClip;
				if (clip != null && !clip.name.Contains("preview")) {
					Clips.Add (clip);
				}
			}
			return Clips.ToArray ();
		}
		private Texture2D GenerateAnimationTexture(){
			Texture2D AnimationTexture = new Texture2D (VertCount,FrameCount,TextureFormat.RGBA32,false); // init animation texture
			_AniClips = new AniClip[_Clips.Length]; // init array

			int CurrentFrame = 0; // the current frame
			for (int i = 0; i < _Clips.Length; i++) { // for each clip
				CurrentFrame = ClipFillTexture(ref AnimationTexture,i,CurrentFrame);
			}

			// set settings
			return AnimationTexture;
		}
		private int ClipFillTexture(ref Texture2D RefTexture,int Index,int BaseFrame){
			AnimationClip Clip = _Clips [Index];
			int TotalFrames = GetClipLength (Clip);

			Vector3[] BaseMeshVerts = _BaseMesh.vertices; // get base verts

			Vector3[,] Difference = new Vector3[BaseMeshVerts.Length, TotalFrames]; // create difference array

			// calculate difference vectors and macimal magnitude
			float MaxMagniture = 0f; // to find max magnitude
			
			for (int y = 0; y < TotalFrames; y++) { // for each frame
				Vector3[] FrameVerts = GetMesh(Clip, y).vertices; // get mesh at frame
				for (int x = 0; x < FrameVerts.Length; x++) { // for each vertex in frame
					Vector3 Vert = FrameVerts[x] - BaseMeshVerts[x]; // gradient of frame now and base mesh
					Difference [x, y] = Vert;

					float Magnitude = Vert.magnitude;
					if (Magnitude > MaxMagniture) {MaxMagniture = Magnitude;}
				}
			}

			// write in the texture
			for (int x = 0; x < Difference.GetLength (0); x++) {
				for (int y = 0; y < Difference.GetLength (1); y++) {
					Vector3 Direction = Difference [x, y];
					float Magnitude = Direction.magnitude;
					Direction = Direction.normalized;

					RefTexture.SetPixel (x, (BaseFrame+y), new Color (
						RemappedPos (Direction.x),
						RemappedPos (Direction.y),
						RemappedPos (Direction.z),
						Magnitude / MaxMagniture
					));
				}
			}

			_AniClips [Index] = new AniClip (_Clips[Index].name,BaseFrame, TotalFrames, MaxMagniture,_Clips[Index].frameRate); // set AniClip
			return BaseFrame + TotalFrames;
		}
		private float RemappedPos(float In){
			return (In + 1f) * 0.5f; // remap
		}
		private Mesh GetMesh(AnimationClip Clip, int Frame){
			GameObject TestObj = GameObject.Instantiate (_FBX);
			Clip.SampleAnimation (TestObj, (Frame / Clip.frameRate)); // sample animation
			Mesh M = new Mesh ();
			TestObj.GetComponentInChildren<SkinnedMeshRenderer>().BakeMesh(M);
			DestroyImmediate (TestObj);
			return M;
		}
		private Mesh GenerateBase(){
			string Path = CreatedFolder + _FBX.name + "_Mesh.asset";
			Path = AssetDatabase.GenerateUniqueAssetPath (Path);

			// create mesh
			Mesh M = GetMesh (_Clips [0], 0);
			Vector2[] uv2 = new Vector2[M.vertexCount];
			for (int i = 0; i < uv2.Length; i++) {uv2 [i] = new Vector2 ((float)(i)/(float)(uv2.Length-1),0);}
			M.uv2 = uv2;

			// save mesh
			AssetDatabase.CreateAsset (M,Path);
			return AssetDatabase.LoadAssetAtPath<Mesh>(Path);
		}
		private Material GenerateMaterial(){
			Material Mat = new Material (Shader.Find (AM.Core.AniMeshCore.Shader));
			string Path = AssetDatabase.GenerateUniqueAssetPath(_CreatedFolder+_FBX.name+"_Material.mat");
			AssetDatabase.CreateAsset (Mat, Path);
			return AssetDatabase.LoadAssetAtPath<Material>(Path);
		}
		private GameObject GeneratePrefab(Material mat){
			GameObject Prefab = new GameObject("",typeof(MeshFilter),typeof(MeshRenderer),typeof(AniMeshAnimator));
			Prefab.GetComponent<MeshFilter> ().mesh = _BaseMesh;
			Prefab.GetComponent<MeshRenderer> ().sharedMaterial = mat;
			string Path = AssetDatabase.GenerateUniqueAssetPath(CreatedFolder + _FBX.name+"_Prefab.prefab");

			// init prefab settings
			AniMeshAnimator Animator = Prefab.GetComponent<AniMeshAnimator>();
			Animator.Data = this;

			// finalize
			GameObject NewPrefab = PrefabUtility.CreatePrefab (Path, Prefab);
			GameObject.DestroyImmediate (Prefab);
			return NewPrefab;
		}
		#endregion

		#region Get / Set
		// privates
		private string Folder{
			get{
				if (_FBX != null) {
					string path = AssetDatabase.GetAssetPath (_FBX);
					if (Path.GetExtension(path) != "")
					{
						path = path.Replace(Path.GetFileName (AssetDatabase.GetAssetPath (_FBX)), "");
					}
					return path;
				}
				return "Assets";
			}
		}
		private string CreatedFolder{
			get{
				if (_CreatedFolder == string.Empty || _CreatedFolder == "" || _CreatedFolder == null) {
					string Path = AssetDatabase.GenerateUniqueAssetPath(Folder + _FBX.name);
					Directory.CreateDirectory (Path);
					_CreatedFolder = Path+"/";
				}
				return _CreatedFolder;
			}
		}
		private string FileName{
			get{
				string path = AssetDatabase.GetAssetPath (this);
				string[] Split = path.Split ('/');
				return Split[Split.Length-1];
			}
		}
		#endregion
		#endif

		#region Private voids
		private int GetClipLength(AnimationClip Clip){
			return (int)(Clip.length * Clip.frameRate)+1;
		}
		#endregion

		#region public voids
		public int GetClipIndexByName(string Name){
			if (ClipIndexDictionary.ContainsKey (Name)) {
				return ClipIndexDictionary [Name];
			}
			return 0;
		}
		#endregion

		#region Get / Set
		// publics
		public Texture2D AnimationTexture{
			get{
				return _AnimationTexture;
			}
		}
		public int VertCount{
			get{
				return this._BaseMesh.vertexCount;
			}
		}
		public int FrameCount{
			get{
				int Frames = 0;
				for (int i = 0; i < _Clips.Length; i++) {
					Frames += GetClipLength (_Clips [i]);
				}
				return Frames;
			}
		}
		public AniClip[] Clips{
			get{
				return _AniClips;
			}
		}
		public Dictionary<string,int> ClipIndexDictionary{
			get{
				if (_ClipIndexDictionary == null) {
					_ClipIndexDictionary = new Dictionary<string, int> ();
					for (int i = 0; i < _AniClips.Length; i++) {
						_ClipIndexDictionary.Add (_AniClips[i].Title,i);
					}
				}
				return _ClipIndexDictionary;
			}
		}
		#endregion

		#region Inner Class
		[System.Serializable]
		public class AniClip{
			#region private variables
			// publics
			public string Title;
			[Range(0.01f,20)]public float _Speed = 1;
			public bool Loop = true;

			// privates
			[HideInInspector][SerializeField]private float _FrameRate = 60;
			[HideInInspector][SerializeField]private int _StartFrame;
			[HideInInspector][SerializeField]private int _Length;
			[HideInInspector][SerializeField]private float _MagnitudeMultiply;
			#endregion

			public AniClip(string Title,int Start, int Length,float Magnitude,float FrameRate){
				this.Title = Title;
				this._StartFrame = Start;
				this._Length = Length;
				this._MagnitudeMultiply = Magnitude;
				this._FrameRate = FrameRate;
			}

			#region Get / set
			public float FrameRate{
				get{
					return _FrameRate;
				}
			}
			public float Speed{
				get{
					return _Speed;
				}
				set{
					_Speed = value;
				}
			}
			public float MagnitudeMultiply{
				get{
					return _MagnitudeMultiply;
				}
			}
			public int Length{
				get{
					return _Length;
				}
			}
			public int StartFrame{
				get{
					return _StartFrame;
				}
			}
			#endregion
		}
		#endregion
	}
}
