using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.Levels.EditorTool
{
	public class LevelToolSettings : ScriptableObject
	{
		public bool showWelcomeOnOpen = true;
		public bool autoSyncOnOpen = true;

		private const string AssetPath = "Assets/ProjectName/Levels/Editor/LevelToolSettings.asset";

		public bool autoNamingEnabled = true;

// Template supports {index} or {index:000}
		public string levelNameTemplate = "Level_{index:000}";
		
		public static LevelToolSettings GetOrCreate()
		{
#if UNITY_EDITOR
			var settings = AssetDatabase.LoadAssetAtPath<LevelToolSettings>(AssetPath);
			if (settings != null)
				return settings;

			EnsureParentFolderExists(AssetPath);

			settings = CreateInstance<LevelToolSettings>();
			AssetDatabase.CreateAsset(settings, AssetPath);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			return settings;
#else
            return null;
#endif
		}

#if UNITY_EDITOR
		private static void EnsureParentFolderExists(string assetPath)
		{
			var directory = Path.GetDirectoryName(assetPath);
			if (string.IsNullOrEmpty(directory))
				return;

			if (!AssetDatabase.IsValidFolder(directory))
			{
				var parent = "Assets";
				var parts = directory.Substring("Assets/".Length).Split('/');

				foreach (var part in parts)
				{
					var current = $"{parent}/{part}";
					if (!AssetDatabase.IsValidFolder(current))
						AssetDatabase.CreateFolder(parent, part);

					parent = current;
				}
			}
		}
#endif
	}
}