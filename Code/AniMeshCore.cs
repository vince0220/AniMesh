#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using AM;

namespace AM.Core{
	public sealed class AniMeshCore {
		#region Constants
		public const string Shader = "Instanced/AniMesh_Standard";
		#endregion

		#region Menu voids
		[MenuItem("Assets/Convert To Animation Mesh")]
		public static void ConvertToAniMesh(){
			var Selected = (GameObject)SelectedObject;
			var Instance = CreateScriptableObject<AniMeshData>(CurrentFolder,Selected.name+"_AniMesh");
			Instance.Instantiate (FBXPath); // Init instance
		}
			
		[MenuItem("Assets/Convert To Animation Mesh",true)]
		public static bool ConvertToAniMeshValidation(){
			if (SelectedObject != null) {
				var Clips = GetAnimationClips (AssetDatabase.GetAssetPath(SelectedObject));
				return (Clips.Length > 0);
			}
			return false;
		}
		#endregion

		#region Public voids
		public static T CreateScriptableObject<T>(string Folder,string Name){
			string Path = AssetDatabase.GenerateUniqueAssetPath(Folder + Name + ".asset");
			var asset = ScriptableObject.CreateInstance(typeof(T));
			AssetDatabase.CreateAsset(asset,Path);
			AssetDatabase.SaveAssets();
			EditorUtility.FocusProjectWindow();
			return (T)(object)asset;
		}
		#endregion

		#region Private voids
		private static AnimationClip[] GetAnimationClips(string FBXPath){
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
		#endregion

		#region Get / set
		public static string CurrentFolder{
			get{
				if (SelectedObject != null) {
					string path = AssetDatabase.GetAssetPath (SelectedObject);
					if (Path.GetExtension(path) != "")
					{
						path = path.Replace(Path.GetFileName (AssetDatabase.GetAssetPath (SelectedObject)), "");
					}
					return path;
				}
				return "Assets";
			}
		}
		public static string FBXPath{
			get{
				if (SelectedObject != null) {
					return AssetDatabase.GetAssetPath (SelectedObject);
				}
				return "";
			}
		}
		public static Object SelectedObject{
			get{
				var obj = Selection.activeObject;
				if (obj != null && typeof(GameObject).IsAssignableFrom (obj.GetType ())) {
					string Path = AssetDatabase.GetAssetPath (obj).ToLower();
					if (Path.Contains (".fbx")) {
						return obj;
					}
				}
				return null;
			}
		}
		#endregion
	}
}
#endif