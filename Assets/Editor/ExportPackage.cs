using UnityEditor;

namespace Mobcast.Coffee.Toggles
{
	/// <summary>Project-only packaging tools; never exports implicitly on reload.</summary>
	public static class ExportPackage
	{
		[MenuItem("Export Package/CompositeToggle.unitypackage")]
		public static void ExportPlugin()
		{
			Export("CompositeToggle.unitypackage", new[] { "Assets/Package" });
		}

		[MenuItem("Export Package/CompositeToggle with Demo")]
		public static void ExportWithDemo()
		{
			Export("CompositeToggle-WithDemo.unitypackage", new[] {
				"Assets/Package", "Assets/Scenes", "Assets/Scripts", "Assets/Content",
				"Assets/Editor/TweenDemoSceneBuilder.cs"
			});
		}

		static void Export(string fileName, string[] paths)
		{
			if (EditorApplication.isPlayingOrWillChangePlaymode)
				return;
			AssetDatabase.ExportPackage(paths, fileName, ExportPackageOptions.Recurse);
			UnityEngine.Debug.Log("Export successfully : " + fileName);
		}
	}
}
